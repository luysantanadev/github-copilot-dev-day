using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using FluentAssertions;
using TodoApi.Models;

namespace TodoApi.Tests;

/// <summary>
/// Testes de integração para os endpoints CRUD de tarefas.
/// Cada teste obtém um token JWT válido antes de executar as operações.
/// </summary>
public class TodosTests : IClassFixture<TodoApiFactory>
{
    private readonly HttpClient _client;

    public TodosTests(TodoApiFactory factory)
    {
        _client = factory.CreateClient();
    }

    // ── Helpers ──────────────────────────────────────────────────────────────

    private async Task AuthenticateAsync()
    {
        var response = await _client.PostAsJsonAsync("/auth/login",
            new LoginRequest(TodoApiFactory.TestAdminUser, TodoApiFactory.TestAdminPass));

        var body = await response.Content.ReadFromJsonAsync<LoginResponse>();
        _client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", body!.Token);
    }

    // ── Testes ────────────────────────────────────────────────────────────────

    [Fact]
    public async Task GetTodos_ComListaVazia_Retorna200EListaVazia()
    {
        await AuthenticateAsync();

        var response = await _client.GetAsync("/todos");
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var todos = await response.Content.ReadFromJsonAsync<List<Todo>>();
        todos.Should().NotBeNull().And.BeEmpty();
    }

    [Fact]
    public async Task PostTodo_ComDadosValidos_Retorna201ComTarefa()
    {
        await AuthenticateAsync();

        var payload = new CreateTodoRequest("Minha primeira tarefa", "Descrição opcional");
        var response = await _client.PostAsJsonAsync("/todos", payload);

        response.StatusCode.Should().Be(HttpStatusCode.Created);

        var created = await response.Content.ReadFromJsonAsync<Todo>();
        created.Should().NotBeNull();
        created!.Id.Should().BeGreaterThan(0);
        created.Title.Should().Be("Minha primeira tarefa");
        created.IsCompleted.Should().BeFalse();
    }

    [Fact]
    public async Task PostTodo_SemTitulo_Retorna400()
    {
        await AuthenticateAsync();

        var payload = new CreateTodoRequest("", null);
        var response = await _client.PostAsJsonAsync("/todos", payload);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task GetTodo_IdExistente_Retorna200()
    {
        await AuthenticateAsync();

        // Cria primeiro
        var created = await (await _client.PostAsJsonAsync("/todos",
            new CreateTodoRequest("Tarefa para buscar", null)))
            .Content.ReadFromJsonAsync<Todo>();

        // Busca pelo id
        var response = await _client.GetAsync($"/todos/{created!.Id}");
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var todo = await response.Content.ReadFromJsonAsync<Todo>();
        todo!.Title.Should().Be("Tarefa para buscar");
    }

    [Fact]
    public async Task GetTodo_IdInexistente_Retorna404()
    {
        await AuthenticateAsync();

        var response = await _client.GetAsync("/todos/99999");
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task PutTodo_MarcaComoCompleta_Retorna200ComCompletedAt()
    {
        await AuthenticateAsync();

        var created = await (await _client.PostAsJsonAsync("/todos",
            new CreateTodoRequest("Tarefa para concluir", null)))
            .Content.ReadFromJsonAsync<Todo>();

        var updatePayload = new UpdateTodoRequest(null, null, true);
        var response = await _client.PutAsJsonAsync($"/todos/{created!.Id}", updatePayload);

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var updated = await response.Content.ReadFromJsonAsync<Todo>();
        updated!.IsCompleted.Should().BeTrue();
        updated.CompletedAt.Should().NotBeNull();
    }

    [Fact]
    public async Task PutTodo_AtualizaTitulo_Retorna200ComNovoTitulo()
    {
        await AuthenticateAsync();

        var created = await (await _client.PostAsJsonAsync("/todos",
            new CreateTodoRequest("Título original", null)))
            .Content.ReadFromJsonAsync<Todo>();

        var updatePayload = new UpdateTodoRequest("Título atualizado", null, null);
        var response = await _client.PutAsJsonAsync($"/todos/{created!.Id}", updatePayload);

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var updated = await response.Content.ReadFromJsonAsync<Todo>();
        updated!.Title.Should().Be("Título atualizado");
    }

    [Fact]
    public async Task DeleteTodo_IdExistente_Retorna204ERemoveDaLista()
    {
        await AuthenticateAsync();

        var created = await (await _client.PostAsJsonAsync("/todos",
            new CreateTodoRequest("Tarefa para excluir", null)))
            .Content.ReadFromJsonAsync<Todo>();

        var deleteResponse = await _client.DeleteAsync($"/todos/{created!.Id}");
        deleteResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);

        var getResponse = await _client.GetAsync($"/todos/{created.Id}");
        getResponse.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task DeleteTodo_IdInexistente_Retorna404()
    {
        await AuthenticateAsync();

        var response = await _client.DeleteAsync("/todos/99999");
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }
}

file record LoginResponse(string Token, string Username);
