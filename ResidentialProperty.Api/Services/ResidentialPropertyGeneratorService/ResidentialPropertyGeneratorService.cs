using Microsoft.Extensions.Caching.Distributed;
using ResidentialProperty.Api.Messaging;
using ResidentialProperty.Domain.Entities;
using System.Text.Json;

namespace ResidentialProperty.Api.Services.ResidentialPropertyGeneratorService;

/// <summary>
/// Сервис получения объекта жилого строительства: сначала ищет в кэше, при промахе — генерирует новый и сохраняет
/// </summary>
public class ResidentialPropertyGeneratorService(
    IDistributedCache cache,
    ResidentialPropertyGenerator generator,
    ISnsPublisher snsPublisher,
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
        var property = await GetFromCacheAsync(cacheKey, id, cancellationToken);

        if (property != null)
        {
            return property;
        }

        // Если в кэше нет или ошибка — генерируем новый объект
        logger.LogInformation("Property {Id} not found in cache or cache unavailable, generating a new one", id);
        property = generator.Generate();
        property.Id = id;

        // Попытка сохранить в кэш
        await SaveToCacheAsync(cacheKey, property, cancellationToken);

        // Публикуем сообщение в SNS для обработки файловым сервисом
        try
        {
            await snsPublisher.Publish(property);
            logger.LogInformation("Property {Id} published to SNS", property.Id);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Failed to publish property {Id} to SNS (error ignored)", property.Id);
        }

        return property;
    }

    private async Task<ResidentialPropertyEntity?> GetFromCacheAsync(string cacheKey, int id, CancellationToken cancellationToken)
    {
        try
        {
            var cachedData = await cache.GetStringAsync(cacheKey, cancellationToken);

            if (!string.IsNullOrEmpty(cachedData))
            {
                var property = JsonSerializer.Deserialize<ResidentialPropertyEntity>(cachedData);

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

        return null;
    }

    private async Task SaveToCacheAsync(string cacheKey, ResidentialPropertyEntity property, CancellationToken cancellationToken)
    {
        try
        {
            logger.LogInformation("Saving property {Id} to cache", property.Id);

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
            logger.LogWarning(ex, "Failed to save property {Id} to cache (error ignored)", property.Id);
        }
    }
}
