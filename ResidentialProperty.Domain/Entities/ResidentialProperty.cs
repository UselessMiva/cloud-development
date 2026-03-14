namespace ResidentialProperty.Domain.Entities;

/// <summary>
/// Объект жилого строительства
/// </summary>
public class ResidentialPropertyEntity
{
    /// <summary>
    /// Идентификатор в системе
    /// </summary>
    public required int Id { get; set; }

    /// <summary>
    /// Адрес
    /// </summary>
    public required string Address { get; set; }

    /// <summary>
    /// Тип недвижимости (Квартира, ИЖС, Апартаменты, Офис и т.д.)
    /// </summary>
    public required string PropertyType { get; set; }

    /// <summary>
    /// Год постройки (не может быть позже текущего)
    /// </summary>
    public int YearBuilt { get; set; }

    /// <summary>
    /// Общая площадь (округляется до двух знаков после запятой)
    /// </summary>
    public double TotalArea { get; set; }

    /// <summary>
    /// Жилая площадь (не может быть больше общей, округляется до двух знаков)
    /// </summary>
    public double LivingArea { get; set; }

    /// <summary>
    /// Этаж (не указывается для ИЖС, не может быть больше этажности)
    /// </summary>
    public int? Floor { get; set; }

    /// <summary>
    /// Этажность здания (не может быть меньше 1)
    /// </summary>
    public int TotalFloors { get; set; }

    /// <summary>
    /// Кадастровый номер (формат: **.**.**.******.****)
    /// </summary>
    public required string CadastralNumber { get; set; }

    /// <summary>
    /// Кадастровая стоимость (округляется до двух знаков, пропорциональна площади)
    /// </summary>
    public decimal CadastralValue { get; set; }
}