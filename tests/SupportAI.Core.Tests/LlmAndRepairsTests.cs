using SupportAI.Core.Models;
using SupportAI.Ia;
using SupportAI.Repairs;

namespace SupportAI.Core.Tests;

public class LlmAndRepairsTests
{
    [Fact]
    public void RepairCatalog_Get_ReturnsCorrectRepairOrNull()
    {
        // Act
        var dnsRepair = RepairCatalog.Get("rep.dns.flush");
        var invalidRepair = RepairCatalog.Get("rep.nonexistent");

        // Assert
        Assert.NotNull(dnsRepair);
        Assert.Equal("rep.dns.flush", dnsRepair.Id);
        Assert.Null(invalidRepair);
    }

    [Fact]
    public void RepairCatalog_All_ContainsAllRepairs()
    {
        // Act
        var all = RepairCatalog.All.ToList();

        // Assert
        Assert.NotEmpty(all);
        Assert.Contains(all, r => r.Id == "rep.dns.flush");
        Assert.Contains(all, r => r.Id == "rep.temp.clean");
        Assert.Contains(all, r => r.Id == "rep.explorer.restart");
    }

    [Fact]
    public void ParseResponseStatic_ParsesValidJsonCorrectly()
    {
        // Arrange
        var json = """
        {
          "explicacion": "Caché DNS saturada",
          "recomendaciones": [
            { "accion": "Flush DNS", "comando": "ipconfig /flushdns", "detalle": "Limpia caché" }
          ]
        }
        """;

        // Act
        var response = OpenRouterProvider.ParseResponseStatic(json, "TestProvider");

        // Assert
        Assert.Equal("Caché DNS saturada", response.Explicacion);
        Assert.Single(response.Recomendaciones);
        Assert.Equal("Flush DNS", response.Recomendaciones[0].Accion);
        Assert.Equal("ipconfig /flushdns", response.Recomendaciones[0].Comando);
        Assert.Equal("TestProvider", response.ProveedorUsado);
    }

    [Fact]
    public void ParseResponseStatic_ParsesEmptyRecomendacionesCorrectly()
    {
        // Arrange
        var json = """
        {
          "explicacion": "El sistema está óptimo",
          "recomendaciones": []
        }
        """;

        // Act
        var response = OpenRouterProvider.ParseResponseStatic(json, "TestProvider");

        // Assert
        Assert.Equal("El sistema está óptimo", response.Explicacion);
        Assert.Empty(response.Recomendaciones);
    }

    [Fact]
    public void ParseResponseStatic_ThrowsOnMalformedJson()
    {
        // Arrange
        var malformedJson = "invalid json";

        // Act & Assert
        Assert.ThrowsAny<System.Text.Json.JsonException>(() => 
            OpenRouterProvider.ParseResponseStatic(malformedJson, "TestProvider"));
    }

    [Fact]
    public async Task LlmService_CascadeSuccess_ReturnsFirstAvailableProviderResult()
    {
        // Arrange
        var mock1 = new MockLlmProvider("Provider1", true, new LlmResponse("Explicación 1", [], "Provider1"));
        var mock2 = new MockLlmProvider("Provider2", true, new LlmResponse("Explicación 2", [], "Provider2"));
        var rules = new RulesProvider();

        var service = new LlmService(new List<ILlmProvider> { mock1, mock2 }, rules);
        var diag = new Diagnostico();

        // Act
        var result = await service.AnalyzeAsync(diag);

        // Assert
        Assert.Equal("Provider1", result.ProveedorUsado);
        Assert.Equal("Explicación 1", result.Explicacion);
        Assert.Equal(1, mock1.CallCount);
        Assert.Equal(0, mock2.CallCount);
    }

    [Fact]
    public async Task LlmService_SkipsUnavailableProvider_AndTriesNext()
    {
        // Arrange
        var mock1 = new MockLlmProvider("Provider1", false, new LlmResponse("Explicación 1", [], "Provider1"));
        var mock2 = new MockLlmProvider("Provider2", true, new LlmResponse("Explicación 2", [], "Provider2"));
        var rules = new RulesProvider();

        var service = new LlmService(new List<ILlmProvider> { mock1, mock2 }, rules);
        var diag = new Diagnostico();

        // Act
        var result = await service.AnalyzeAsync(diag);

        // Assert
        Assert.Equal("Provider2", result.ProveedorUsado);
        Assert.Equal("Explicación 2", result.Explicacion);
        Assert.Equal(0, mock1.CallCount);
        Assert.Equal(1, mock2.CallCount);
    }

    [Fact]
    public async Task LlmService_FallsBackToRulesOnException()
    {
        // Arrange
        var mock1 = new MockLlmProvider("Provider1", true, null!); // Lanzará excepción al intentar analizar (null ref)
        var rules = new RulesProvider();

        var service = new LlmService(new List<ILlmProvider> { mock1 }, rules);
        var diag = new Diagnostico();

        // Act
        var result = await service.AnalyzeAsync(diag);

        // Assert
        Assert.Equal("Reglas locales", result.ProveedorUsado);
        Assert.False(string.IsNullOrWhiteSpace(result.Explicacion));
        Assert.Equal(1, mock1.CallCount);
    }

    private class MockLlmProvider : ILlmProvider
    {
        private readonly LlmResponse _response;

        public MockLlmProvider(string name, bool disponible, LlmResponse response)
        {
            Name = name;
            Disponible = disponible;
            _response = response;
        }

        public string Name { get; }
        public bool Disponible { get; }
        public int CallCount { get; private set; }

        public Task<LlmResponse> AnalyzeAsync(Diagnostico diag, CancellationToken ct = default)
        {
            CallCount++;
            if (_response == null)
                throw new InvalidOperationException("Mock provider error");
            return Task.FromResult(_response);
        }
    }
}
