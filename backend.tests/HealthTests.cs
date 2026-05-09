using System.Net;
using FluentAssertions;

namespace TodoApi.Tests;

/// <summary>
/// Testes para o endpoint de health check.
/// </summary>
public class HealthTests : IClassFixture<TodoApiFactory>
{
    private readonly HttpClient _client;

    public HealthTests(TodoApiFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task HealthCheck_Retorna200ComStatusHealthy()
    {
        var response = await _client.GetAsync("/health");

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var body = await response.Content.ReadAsStringAsync();
        body.Should().Contain("healthy");
    }
}
