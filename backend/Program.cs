using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using DotNetEnv;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Serilog;
using Serilog.Events;
using TodoApi.Data;
using TodoApi.Models;

// ── 1. Carrega variáveis do arquivo .env (ignorado se não existir) ─────────────
Env.Load();

// ── 2. Configura Serilog com nível de log vindo da variável LOG_LEVEL ──────────
var logLevelStr = Environment.GetEnvironmentVariable("LOG_LEVEL") ?? "Information";
var serilogLevel = logLevelStr.ToLowerInvariant() switch
{
    "debug"       => LogEventLevel.Debug,
    "warning"     => LogEventLevel.Warning,
    "error"       => LogEventLevel.Error,
    "fatal"       => LogEventLevel.Fatal,
    _             => LogEventLevel.Information
};

Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Is(serilogLevel)
    // Formato com propriedades estruturadas em JSON após a mensagem
    .WriteTo.Console(
        outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] {Message:lj} {Properties:j}{NewLine}{Exception}")
    .Enrich.FromLogContext()
    .CreateLogger();

// ── 3. Bootstrap do builder ───────────────────────────────────────────────────
var builder = WebApplication.CreateBuilder(args);
builder.Host.UseSerilog();

// ── 4. Lê configurações do ambiente / .env ────────────────────────────────────
var port          = int.Parse(Environment.GetEnvironmentVariable("APP_PORT") ?? "5000");
var dbPath        = Environment.GetEnvironmentVariable("DB_PATH") ?? "./todos.db";
var jwtSecret     = Environment.GetEnvironmentVariable("JWT_SECRET")
                    ?? throw new InvalidOperationException("A variável JWT_SECRET deve estar definida.");
var adminUsername = Environment.GetEnvironmentVariable("ADMIN_USERNAME") ?? "admin";
var adminPassword = Environment.GetEnvironmentVariable("ADMIN_PASSWORD") ?? "admin";
var corsOrigins   = (Environment.GetEnvironmentVariable("CORS_ORIGINS") ?? "http://localhost:5173")
                    .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

// ── 5. Define a URL de escuta ─────────────────────────────────────────────────
builder.WebHost.UseUrls($"http://0.0.0.0:{port}");

// ── 6. Banco de dados SQLite ──────────────────────────────────────────────────
builder.Services.AddDbContext<TodoDbContext>(opt =>
    opt.UseSqlite($"Data Source={dbPath}"));

// ── 7. Autenticação JWT ───────────────────────────────────────────────────────
var keyBytes = Encoding.UTF8.GetBytes(jwtSecret);

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(opt =>
    {
        opt.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer           = false,
            ValidateAudience         = false,
            ValidateLifetime         = true,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey         = new SymmetricSecurityKey(keyBytes),
            ClockSkew                = TimeSpan.Zero
        };
    });

builder.Services.AddAuthorization();

// ── 8. CORS: permite as origens configuradas em CORS_ORIGINS ─────────────────
builder.Services.AddCors(opt =>
    opt.AddDefaultPolicy(policy =>
        policy.WithOrigins(corsOrigins)
              .AllowAnyHeader()
              .AllowAnyMethod()));

// ── 9. Build da aplicação ─────────────────────────────────────────────────────
var app = builder.Build();

// ── 10. Cria (ou valida) o banco de dados na inicialização ────────────────────
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<TodoDbContext>();
    db.Database.EnsureCreated();
    Log.Information("Banco de dados inicializado: {DbPath}", dbPath);
}

app.UseCors();
app.UseAuthentication();
app.UseAuthorization();
app.UseSerilogRequestLogging(); // Loga cada requisição HTTP automaticamente

// ─────────────────────────────────────────────────────────────────────────────
// ENDPOINT: Health Check
// ─────────────────────────────────────────────────────────────────────────────
app.MapGet("/health", () => Results.Ok(new { status = "healthy", timestamp = DateTime.UtcNow }))
   .AllowAnonymous()
   .WithName("HealthCheck");

// ─────────────────────────────────────────────────────────────────────────────
// ENDPOINT: Login  POST /auth/login
// ─────────────────────────────────────────────────────────────────────────────
app.MapPost("/auth/login", (LoginRequest req) =>
{
    Log.Information("Tentativa de login: {Username}", req.Username);

    if (req.Username != adminUsername || req.Password != adminPassword)
    {
        Log.Warning("Credenciais inválidas para: {Username}", req.Username);
        return Results.Unauthorized();
    }

    var claims = new[]
    {
        new Claim(ClaimTypes.Name, req.Username),
        new Claim(ClaimTypes.Role, "Admin")
    };

    var signingKey  = new SymmetricSecurityKey(keyBytes);
    var credentials = new SigningCredentials(signingKey, SecurityAlgorithms.HmacSha256);
    var token       = new JwtSecurityToken(
        claims:             claims,
        expires:            DateTime.UtcNow.AddHours(8),
        signingCredentials: credentials);

    var tokenString = new JwtSecurityTokenHandler().WriteToken(token);

    Log.Information("Login bem-sucedido: {Username}", req.Username);
    return Results.Ok(new { token = tokenString, username = req.Username });
})
.AllowAnonymous()
.WithName("Login");

// ─────────────────────────────────────────────────────────────────────────────
// ENDPOINTS: Tarefas (todos protegidos por JWT)
// ─────────────────────────────────────────────────────────────────────────────

// GET /todos – lista todas as tarefas (ordenadas da mais recente para a mais antiga)
app.MapGet("/todos", async (TodoDbContext db) =>
{
    Log.Debug("Buscando todas as tarefas");
    var todos = await db.Todos.OrderByDescending(t => t.CreatedAt).ToListAsync();
    Log.Information("Retornando {Count} tarefa(s)", todos.Count);
    return Results.Ok(todos);
})
.RequireAuthorization();

// GET /todos/{id} – retorna uma tarefa pelo id
app.MapGet("/todos/{id:int}", async (TodoDbContext db, int id) =>
{
    Log.Debug("Buscando tarefa {Id}", id);
    var todo = await db.Todos.FindAsync(id);
    return todo is null ? Results.NotFound() : Results.Ok(todo);
})
.RequireAuthorization();

// POST /todos – cria nova tarefa
app.MapPost("/todos", async (TodoDbContext db, CreateTodoRequest req) =>
{
    if (string.IsNullOrWhiteSpace(req.Title))
    {
        Log.Warning("Tentativa de criar tarefa sem título");
        return Results.BadRequest(new { error = "O título é obrigatório." });
    }

    var todo = new Todo
    {
        Title       = req.Title.Trim(),
        Description = req.Description?.Trim(),
        IsCompleted = false,
        CreatedAt   = DateTime.UtcNow
    };

    db.Todos.Add(todo);
    await db.SaveChangesAsync();

    Log.Information("Tarefa criada: Id={Id}, Título='{Title}'", todo.Id, todo.Title);
    return Results.Created($"/todos/{todo.Id}", todo);
})
.RequireAuthorization();

// PUT /todos/{id} – atualiza uma tarefa existente
app.MapPut("/todos/{id:int}", async (TodoDbContext db, int id, UpdateTodoRequest req) =>
{
    var todo = await db.Todos.FindAsync(id);
    if (todo is null)
    {
        Log.Warning("Tarefa {Id} não encontrada para atualização", id);
        return Results.NotFound();
    }

    if (req.Title is not null)       todo.Title       = req.Title.Trim();
    if (req.Description is not null) todo.Description = req.Description.Trim();

    if (req.IsCompleted.HasValue && req.IsCompleted.Value != todo.IsCompleted)
    {
        todo.IsCompleted = req.IsCompleted.Value;
        todo.CompletedAt = req.IsCompleted.Value ? DateTime.UtcNow : null;
    }

    await db.SaveChangesAsync();

    Log.Information("Tarefa {Id} atualizada: IsCompleted={IsCompleted}", id, todo.IsCompleted);
    return Results.Ok(todo);
})
.RequireAuthorization();

// DELETE /todos/{id} – exclui uma tarefa
app.MapDelete("/todos/{id:int}", async (TodoDbContext db, int id) =>
{
    var todo = await db.Todos.FindAsync(id);
    if (todo is null)
    {
        Log.Warning("Tarefa {Id} não encontrada para exclusão", id);
        return Results.NotFound();
    }

    db.Todos.Remove(todo);
    await db.SaveChangesAsync();

    Log.Information("Tarefa {Id} excluída", id);
    return Results.NoContent();
})
.RequireAuthorization();

// ─────────────────────────────────────────────────────────────────────────────
Log.Information("API iniciando na porta {Port} | LogLevel={LogLevel}", port, logLevelStr);
app.Run();

// Necessário para o WebApplicationFactory nos testes de integração
public partial class Program { }
