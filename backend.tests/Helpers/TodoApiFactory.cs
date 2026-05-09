using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using TodoApi.Data;

namespace TodoApi.Tests;

/// <summary>
/// Factory para criar o servidor de teste com banco de dados em memória.
/// Cada instância usa um banco isolado (Guid no nome) para evitar colisão entre testes.
/// Variáveis de ambiente mínimas são configuradas antes do Program.cs ser executado.
/// </summary>
public class TodoApiFactory : WebApplicationFactory<Program>
{
    private readonly string _dbName = Guid.NewGuid().ToString();

    // Segredo JWT fixo para testes (>= 32 caracteres)
    public const string TestJwtSecret = "test-jwt-secret-at-least-32-characters-long";
    public const string TestAdminUser  = "admin";
    public const string TestAdminPass  = "admin";

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        // Define variáveis de ambiente antes do Program.cs ser executado
        Environment.SetEnvironmentVariable("JWT_SECRET",     TestJwtSecret);
        Environment.SetEnvironmentVariable("ADMIN_USERNAME", TestAdminUser);
        Environment.SetEnvironmentVariable("ADMIN_PASSWORD", TestAdminPass);
        Environment.SetEnvironmentVariable("LOG_LEVEL",      "Warning");

        builder.ConfigureServices(services =>
        {
            // Remove o DbContext SQLite registrado no Program.cs
            var descriptor = services.SingleOrDefault(
                d => d.ServiceType == typeof(DbContextOptions<TodoDbContext>));

            if (descriptor is not null)
                services.Remove(descriptor);

            // Substitui por banco em memória isolado por instância
            services.AddDbContext<TodoDbContext>(opt =>
                opt.UseInMemoryDatabase(_dbName));
        });
    }
}
