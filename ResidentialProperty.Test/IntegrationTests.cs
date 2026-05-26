using Aspire.Hosting;
using Microsoft.Extensions.Logging;
using ResidentialProperty.Domain.Entities;
using System.Net.Http.Json;
using System.Text.Json;

namespace ResidentialProperty.Test;

/// <summary>
/// Интеграционные тесты
/// </summary>
public class IntegrationTest : IAsyncLifetime
{
    private static readonly JsonSerializerOptions _jsonOptions = new(JsonSerializerDefaults.Web);

    private DistributedApplication? _app;
    private HttpClient? _gatewayClient;
    private HttpClient? _fileServiceClient;

    /// <inheritdoc/>
    public async Task InitializeAsync()
    {
        var cancellationToken = CancellationToken.None;
        IDistributedApplicationTestingBuilder builder = await DistributedApplicationTestingBuilder
            .CreateAsync<Projects.ResidentialProperty_AppHost>(cancellationToken);

        builder.Configuration["DcpPublisher:RandomizePorts"] = "false";

        builder.Services.AddLogging(logging =>
        {
            logging.SetMinimumLevel(LogLevel.Debug);
            logging.AddFilter("Aspire.Hosting.Dcp", LogLevel.Debug);
            logging.AddFilter("Aspire.Hosting", LogLevel.Debug);
        });

        _app = await builder.BuildAsync(cancellationToken);
        await _app.StartAsync(cancellationToken);

        _gatewayClient = _app.CreateHttpClient("residentialproperty-apigateway", "http");
        _fileServiceClient = _app.CreateHttpClient("fileservice", "http");
    }

    /// <summary>
    /// Тест на корректное создание объекта недвижимости
    /// </summary>
    [Fact]
    public async Task GetProperty_GenerateTest()
    {
        var response = await _gatewayClient!.GetAsync("/api/ResidentialProperty?id=1");

        Assert.NotNull(response);
        Assert.Equal(System.Net.HttpStatusCode.OK, response.StatusCode);

        var property = await response.Content.ReadFromJsonAsync<ResidentialPropertyEntity>(_jsonOptions);

        Assert.NotNull(property);
        Assert.Equal(1, property.Id);
        Assert.False(string.IsNullOrWhiteSpace(property.Address));
        Assert.False(string.IsNullOrWhiteSpace(property.PropertyType));
        Assert.False(string.IsNullOrWhiteSpace(property.CadastralNumber));
        Assert.True(property.YearBuilt > 1900);
        Assert.True(property.TotalArea > 0);
        Assert.True(property.LivingArea > 0);
        Assert.True(property.LivingArea <= property.TotalArea);
        Assert.True(property.TotalFloors >= 1);
        Assert.True(property.CadastralValue > 0);

        if (property.PropertyType != "ИЖС")
        {
            Assert.NotNull(property.Floor);
            Assert.True(property.Floor <= property.TotalFloors);
        }
    }

    /// <summary>
    /// Тест, что при запросах с одинаковыми id приходят одинаковые объекты (кэш)
    /// </summary>
    [Fact]
    public async Task GetProperty_CacheTest()
    {
        var response1 = await _gatewayClient!.GetAsync("/api/ResidentialProperty?id=2");
        var property1 = await response1.Content.ReadFromJsonAsync<ResidentialPropertyEntity>(_jsonOptions);

        var response2 = await _gatewayClient!.GetAsync("/api/ResidentialProperty?id=2");
        var property2 = await response2.Content.ReadFromJsonAsync<ResidentialPropertyEntity>(_jsonOptions);

        Assert.Equal(System.Net.HttpStatusCode.OK, response1.StatusCode);
        Assert.Equal(System.Net.HttpStatusCode.OK, response2.StatusCode);
        Assert.NotNull(property1);
        Assert.NotNull(property2);
        Assert.Equal(property1.Id, property2.Id);
        Assert.Equal(property1.Address, property2.Address);
        Assert.Equal(property1.PropertyType, property2.PropertyType);
        Assert.Equal(property1.CadastralNumber, property2.CadastralNumber);
        Assert.Equal(property1.YearBuilt, property2.YearBuilt);
        Assert.Equal(property1.TotalArea, property2.TotalArea);
        Assert.Equal(property1.LivingArea, property2.LivingArea);
        Assert.Equal(property1.CadastralValue, property2.CadastralValue);
    }

    /// <summary>
    /// Тест, что при запросах с разными id приходят разные объекты
    /// </summary>
    [Fact]
    public async Task GetProperty_DifferentPropertyTest()
    {
        var response1 = await _gatewayClient!.GetAsync("/api/ResidentialProperty?id=3");
        var property1 = await response1.Content.ReadFromJsonAsync<ResidentialPropertyEntity>(_jsonOptions);

        var response2 = await _gatewayClient!.GetAsync("/api/ResidentialProperty?id=4");
        var property2 = await response2.Content.ReadFromJsonAsync<ResidentialPropertyEntity>(_jsonOptions);

        Assert.Equal(System.Net.HttpStatusCode.OK, response1.StatusCode);
        Assert.Equal(System.Net.HttpStatusCode.OK, response2.StatusCode);
        Assert.NotNull(property1);
        Assert.NotNull(property2);
        Assert.NotEqual(property1.Id, property2.Id);
        Assert.NotEqual(property1.Address, property2.Address);
        Assert.NotEqual(property1.CadastralNumber, property2.CadastralNumber);
    }

    /// <summary>
    /// Тест на создание и запись в MinIO
    /// </summary>
    [Fact]
    public async Task GetProperty_GenerateInMinio()
    {
        var id = 5;
        var expectedKey = $"residentialproperty-{id}.json";

        using var response = await _gatewayClient!.GetAsync($"/api/ResidentialProperty?id={id}");
        response.EnsureSuccessStatusCode();

        var property = await response.Content.ReadFromJsonAsync<ResidentialPropertyEntity>(_jsonOptions);
        Assert.NotNull(property);

        ResidentialPropertyEntity? s3Item = null;
        var deadline = DateTime.UtcNow.AddSeconds(30);
        while (DateTime.UtcNow < deadline)
        {
            using var s3Response = await _fileServiceClient!.GetAsync($"/api/s3/{expectedKey}");
            if (s3Response.IsSuccessStatusCode)
            {
                s3Item = JsonSerializer.Deserialize<ResidentialPropertyEntity>(
                    await s3Response.Content.ReadAsStringAsync(),
                    _jsonOptions);
                if (s3Item is not null)
                    break;
            }
            await Task.Delay(1000);
        }

        Assert.NotNull(s3Item);
        Assert.Equal(property!.Id, s3Item!.Id);
        Assert.Equal(property.Address, s3Item.Address);
        Assert.Equal(property.PropertyType, s3Item.PropertyType);
        Assert.Equal(property.CadastralNumber, s3Item.CadastralNumber);
    }

    /// <summary>
    /// Тест на отправку в SNS (FileService получает сообщение)
    /// </summary>
    [Fact]
    public async Task GetProperty_SnsPublishTest()
    {
        var id = 7;

        using var response = await _gatewayClient!.GetAsync($"/api/ResidentialProperty?id={id}");
        response.EnsureSuccessStatusCode();

        await Task.Delay(TimeSpan.FromSeconds(5));

        var expectedKey = $"residentialproperty-{id}.json";

        ResidentialPropertyEntity? s3Item = null;
        var deadline = DateTime.UtcNow.AddSeconds(30);
        while (DateTime.UtcNow < deadline)
        {
            using var s3Response = await _fileServiceClient!.GetAsync($"/api/s3/{expectedKey}");
            if (s3Response.IsSuccessStatusCode)
            {
                s3Item = JsonSerializer.Deserialize<ResidentialPropertyEntity>(
                    await s3Response.Content.ReadAsStringAsync(),
                    _jsonOptions);
                if (s3Item is not null)
                    break;
            }
            await Task.Delay(1000);
        }

        Assert.NotNull(s3Item);
        Assert.Equal(id, s3Item!.Id);
    }

    /// <inheritdoc/>
    public async Task DisposeAsync()
    {
        _gatewayClient?.Dispose();
        _fileServiceClient?.Dispose();
        if (_app is not null)
        {
            await _app.StopAsync();
            await _app.DisposeAsync();
        }
    }
}