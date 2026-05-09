namespace TodoApi.Models;

/// <summary>Entidade principal da lista de tarefas.</summary>
public class Todo
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public bool IsCompleted { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? CompletedAt { get; set; }
}

// ── DTOs de request ───────────────────────────────────────────────────────────

/// <summary>Payload enviado na requisição de login.</summary>
public record LoginRequest(string Username, string Password);

/// <summary>Payload para criar uma nova tarefa.</summary>
public record CreateTodoRequest(string Title, string? Description);

/// <summary>Payload para atualizar uma tarefa existente (todos os campos são opcionais).</summary>
public record UpdateTodoRequest(string? Title, string? Description, bool? IsCompleted);
