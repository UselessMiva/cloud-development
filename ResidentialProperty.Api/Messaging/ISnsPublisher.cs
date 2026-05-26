using ResidentialProperty.Domain.Entities;

namespace ResidentialProperty.Api.Messaging;

/// <summary>
/// Сервис для отправки сообщений в SNS-топик.
/// </summary>
public interface ISnsPublisher
{
    /// <summary>
    /// Публикует объект жилого строительства в SNS-топик.
    /// </summary>
    /// <param name="dto">Объект жилого строительства для отправки.</param>
    /// <returns>Задача, представляющая асинхронную операцию публикации.</returns>
    public Task Publish(ResidentialPropertyEntity dto);
}