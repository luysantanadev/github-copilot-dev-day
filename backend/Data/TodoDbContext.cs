using Microsoft.EntityFrameworkCore;
using TodoApi.Models;

namespace TodoApi.Data;

/// <summary>Contexto do Entity Framework Core para o banco de dados SQLite.</summary>
public class TodoDbContext : DbContext
{
    public TodoDbContext(DbContextOptions<TodoDbContext> options) : base(options) { }

    public DbSet<Todo> Todos => Set<Todo>();
}
