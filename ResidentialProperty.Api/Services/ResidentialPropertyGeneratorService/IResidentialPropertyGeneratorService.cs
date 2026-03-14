using ResidentialProperty.Domain.Entities;

namespace ResidentialProperty.Api.Services.ResidentialPropertyGeneratorService;

/// <summary>
/// Сервис получения объекта жилого строительства
/// </summary>
public interface IResidentialPropertyGeneratorService
{
    public Task<ResidentialPropertyEntity> GetByIdAsync(int id, CancellationToken cancellationToken = default);
}