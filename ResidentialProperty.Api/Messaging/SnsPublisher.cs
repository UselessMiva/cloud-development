using Amazon.SimpleNotificationService;
using Amazon.SimpleNotificationService.Model;
using ResidentialProperty.Domain.Entities;
using System.Text.Json;

namespace ResidentialProperty.Api.Messaging;

/// <summary>
/// Сервис для отправки сообщений в SNS-топик.
/// </summary>
/// <param name="snsClient">Клиент AWS SNS для взаимодействия с сервисом уведомлений.</param>
/// <param name="configuration">Конфигурация приложения, содержащая ARN SNS-топика.</param>
/// <param name="logger">Логгер для записи информационных сообщений о процессе публикации.</param>
public class SnsPublisher(
    IAmazonSimpleNotificationService snsClient,
    IConfiguration configuration,
    ILogger<SnsPublisher> logger) : ISnsPublisher
{
    private readonly string _topicArn = configuration["AWS:Resources:SNSTopicArn"]
        ?? throw new InvalidOperationException("SNS Topic ARN is not configured (AWS:Resources:SNSTopicArn)");

    /// <summary>
    /// Публикует объект жилого строительства в SNS-топик.
    /// </summary>
    /// <param name="dto">Объект жилого строительства для отправки.</param>
    /// <returns>Задача, представляющая асинхронную операцию публикации.</returns>
    public async Task Publish(ResidentialPropertyEntity dto)
    {
        var message = JsonSerializer.Serialize(dto);

        logger.LogInformation("Publishing property {Id} to SNS topic {TopicArn}", dto.Id, _topicArn);

        await snsClient.PublishAsync(new PublishRequest
        {
            TopicArn = _topicArn,
            Message = message
        });
    }
}