using Amazon.SimpleNotificationService;
using Amazon.SimpleNotificationService.Model;
using System.Net;

namespace ResidentialProperty.FileService.Messaging;

/// <summary>
/// Сервис для подписки на SNS-топик и получения уведомлений.
/// </summary>
/// <param name="snsClient">Клиент AWS SNS для взаимодействия с сервисом уведомлений.</param>
/// <param name="configuration">Конфигурация приложения, содержащая ARN топика и URL эндпоинта.</param>
/// <param name="logger">Логгер для записи информационных сообщений и ошибок подписки.</param>
public class SnsService(IAmazonSimpleNotificationService snsClient, IConfiguration configuration, ILogger<SnsService> logger)
{
    private readonly string _topicArn = configuration["AWS:Resources:SnsTopicArn"]
        ?? throw new InvalidOperationException("SNS Topic ARN is not configured");

    /// <summary>
    /// Выполняет подписку HTTP-эндпоинта текущего сервиса на SNS-топик.
    /// </summary>
    /// <returns>Задача, представляющая асинхронную операцию подписки.</returns>
    public async Task SubscribeEndpoint()
    {
        var endpoint = configuration["AWS:Resources:SNSUrl"];

        if (string.IsNullOrWhiteSpace(endpoint))
        {
            logger.LogWarning("SNS endpoint URL is not configured. Skipping subscription.");
            return;
        }

        var request = new SubscribeRequest
        {
            TopicArn = _topicArn,
            Protocol = "http",
            Endpoint = endpoint,
            ReturnSubscriptionArn = true
        };

        var response = await snsClient.SubscribeAsync(request);

        if (response.HttpStatusCode != HttpStatusCode.OK)
        {
            logger.LogError("Failed to subscribe to SNS topic {TopicArn}", _topicArn);
        }
        else
        {
            logger.LogInformation("Successfully subscribed to SNS topic {TopicArn}. Subscription ARN: {SubscriptionArn}",
                _topicArn, response.SubscriptionArn);
        }
    }
}