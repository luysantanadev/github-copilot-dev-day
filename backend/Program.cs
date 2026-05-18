var builder = WebApplication.CreateBuilder(args);

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowVue", policy =>
        policy.WithOrigins("http://localhost:5173")
              .AllowAnyHeader()
              .AllowAnyMethod());
});

var app = builder.Build();

app.UseCors("AllowVue");

// In-memory store
var todos = new List<Todo>();
int nextId = 1;

// GET all todos
app.MapGet("/api/todos", () => Results.Ok(todos));

// GET todo by id
app.MapGet("/api/todos/{id:int}", (int id) =>
{
    var todo = todos.FirstOrDefault(t => t.Id == id);
    return todo is null ? Results.NotFound() : Results.Ok(todo);
});

// POST create todo
app.MapPost("/api/todos", (CreateTodoRequest request) =>
{
    if (string.IsNullOrWhiteSpace(request.Title))
        return Results.BadRequest(new { error = "Title is required." });

    var todo = new Todo(nextId++, request.Title.Trim(), false);
    todos.Add(todo);
    return Results.Created($"/api/todos/{todo.Id}", todo);
});

// PUT update todo (toggle or rename)
app.MapPut("/api/todos/{id:int}", (int id, UpdateTodoRequest request) =>
{
    var index = todos.FindIndex(t => t.Id == id);
    if (index == -1) return Results.NotFound();

    var existing = todos[index];
    var updated = existing with
    {
        Title = string.IsNullOrWhiteSpace(request.Title) ? existing.Title : request.Title.Trim(),
        IsCompleted = request.IsCompleted
    };
    todos[index] = updated;
    return Results.Ok(updated);
});

// DELETE todo
app.MapDelete("/api/todos/{id:int}", (int id) =>
{
    var todo = todos.FirstOrDefault(t => t.Id == id);
    if (todo is null) return Results.NotFound();
    todos.Remove(todo);
    return Results.NoContent();
});

app.Run();

record Todo(int Id, string Title, bool IsCompleted);
record CreateTodoRequest(string Title);
record UpdateTodoRequest(string Title, bool IsCompleted);
