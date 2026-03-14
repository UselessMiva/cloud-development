using Bogus;
using ResidentialProperty.Domain.Entities;

namespace ResidentialProperty.Api.Services.ResidentialPropertyGeneratorService;

/// <summary>
/// Генератор случайных объектов жилого строительства с использованием Bogus
/// </summary>
public class ResidentialPropertyGenerator
{
    private readonly Faker<ResidentialPropertyEntity> _faker;
    private int _idCounter = 1;

    public ResidentialPropertyGenerator()
    {
        var propertyTypes = new[] { "Квартира", "ИЖС", "Апартаменты", "Офис", "Коммерческая" };
        var currentYear = DateTime.Now.Year;

        _faker = new Faker<ResidentialPropertyEntity>("ru")
            .RuleFor(p => p.Id, f => _idCounter++)
            .RuleFor(p => p.Address, f => $"{f.Address.City()}, ул. {f.Address.StreetName()}, д. {f.Random.Number(1, 100)}")
            .RuleFor(p => p.PropertyType, f => f.PickRandom(propertyTypes))
            .RuleFor(p => p.YearBuilt, f => f.Random.Number(1950, currentYear))
            .RuleFor(p => p.TotalArea, f => Math.Round(f.Random.Double(30, 200), 2))
            .RuleFor(p => p.LivingArea, (f, p) => Math.Round(p.TotalArea * f.Random.Double(0.5, 0.9), 2))
            .RuleFor(p => p.TotalFloors, f => f.Random.Number(1, 25))
            .RuleFor(p => p.Floor, (f, p) =>
                p.PropertyType == "ИЖС" ? null : f.Random.Number(1, p.TotalFloors))
            .RuleFor(p => p.CadastralNumber, f =>
                $"{f.Random.Number(1, 99):D2}.{f.Random.Number(1, 99):D2}.{f.Random.Number(1, 99):D2}.{f.Random.Number(1, 999999):D6}.{f.Random.Number(1, 9999):D4}")
            .RuleFor(p => p.CadastralValue, (f, p) =>
                Math.Round((decimal)(p.TotalArea * f.Random.Double(50000, 150000)), 2));
    }

    /// <summary>
    /// Генерирует один случайный объект жилого строительства
    /// </summary>
    public ResidentialPropertyEntity Generate() => _faker.Generate();
}
