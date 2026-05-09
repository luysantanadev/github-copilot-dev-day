using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using FluentAssertions;
using TodoApi.Models;

namespace TodoApi.Tests;

/// <summary>
/// Testes de integração para o endpoint de autenticação.
/// </summary>
public class AuthTests : IClassFixture<TodoApiFactory>
{
    private readonly HttpClient _client;

    public AuthTests(TodoApiFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task Login_ComCredenciaisValidas_Retorna200ComToken()
    {
        // Arrange
        var payload = new LoginRequest(TodoApiFactory.TestAdminUser, TodoApiFactory.TestAdminPass);

        // Act
        var response = await _client.PostAsJsonAsync("/auth/login", payload);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var body = await response.Content.ReadFromJsonAsync<LoginResponse>();
        body.Should().NotBeNull();
        body!.Token.Should().NotBeNullOrEmpty();
        body.Username.Should().Be(TodoApiFactory.TestAdminUser);
    }

    [Fact]
    public async Task Login_ComSenhaErrada_Retorna401()
    {
        var payload = new LoginRequest(TodoApiFactory.TestAdminUser, "senha-errada");
        var response = await _client.PostAsJsonAsync("/auth/login", payload);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Login_ComUsuarioErrado_Retorna401()
    {
        var payload = new LoginRequest("usuario-inexistente", TodoApiFactory.TestAdminPass);
        var response = await _client.PostAsJsonAsync("/auth/login", payload);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task AcessarTodos_SemToken_Retorna401()
    {
        var response = await _client.GetAsync("/todos");
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }
}

// DTO interno para desserializar a resposta do login
file record LoginResponse(string Token, string Username);
