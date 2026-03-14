using Microsoft.Extensions.Caching.Distributed;
using ResidentialProperty.Api.Services.ResidentialPropertyGeneratorService;
using ResidentialProperty.Domain.Entities;
using System.Text.Json;

namespace ResidentialProperty.Api.Services.ResidentialPropertyGeneratorService;

/// <summary>
/// Сервис получения объекта жилого строительства: сначала ищет в кэше, при промахе — генерирует новый и сохраняет
/// </summary>
public class ResidentialPropertyGeneratorService(
    IDistributedCache cache,
    ResidentialPropertyGenerator generator,
    IConfiguration configuration,
    ILogger<ResidentialPropertyGeneratorService> logger) : IResidentialPropertyGeneratorService
{
    private readonly int _expirationMinutes = configuration.GetValue("CacheSettings:ExpirationMinutes", 10);

    /// <summary>
    /// Возвращает объект жилого строительства по идентификатору.
    /// Если объект найден в кэше — возвращается из него; иначе генерируется, сохраняется в кэш и возвращается.
    /// </summary>
    /// <param name="id">Идентификатор объекта</param>
    /// <param name="cancellationToken">Токен отмены операции</param>
    /// <returns>Объект жилого строительства</returns>
    public async Task<ResidentialPropertyEntity> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        logger.LogInformation("Attempting to retrieve residential property {Id} from cache", id);

        var cacheKey = $"residential-property-{id}";

        // Получаем объект из кэша
        ResidentialPropertyEntity? property = null;
        try
        {
            var cachedData = await cache.GetStringAsync(cacheKey, cancellationToken);

            if (!string.IsNullOrEmpty(cachedData))
            {
                property = JsonSerializer.Deserialize<ResidentialPropertyEntity>(cachedData);

                if (property != null)
                {
                    logger.LogInformation("Residential property {Id} found in cache", id);
                    return property;
                }

                logger.LogWarning("Property {Id} was found in cache but could not be deserialized. Generating a new one", id);
            }
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Failed to retrieve property {Id} from cache (error ignored)", id);
        }

        // Если в кэше нет или ошибка — генерируем новый объект
        logger.LogInformation("Property {Id} not found in cache or cache unavailable, generating a new one", id);
        property = generator.Generate();
        property.Id = id;

        // Попытка сохранить в кэш
        try
        {
            logger.LogInformation("Saving property {Id} to cache", id);

            var cacheOptions = new DistributedCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(_expirationMinutes)
            };

            await cache.SetStringAsync(
                cacheKey,
                JsonSerializer.Serialize(property),
                cacheOptions,
                cancellationToken);

            logger.LogInformation(
                "Residential property generated and cached: Id={Id}, Address={Address}, Type={PropertyType}, TotalArea={TotalArea}, CadastralValue={CadastralValue}",
                property.Id,
                property.Address,
                property.PropertyType,
                property.TotalArea,
                property.CadastralValue);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Failed to save property {Id} to cache (error ignored)", id);
        }

        return property;
    }
}