using Amazon.SimpleNotificationService;
using Amazon.SimpleNotificationService.Model;
using System.Net;

namespace ResidentialProperty.FileService.Messaging;

public class SnsService(IAmazonSimpleNotificationService snsClient, IConfiguration configuration, ILogger<SnsService> logger)
{
    private readonly string _topicArn = configuration["AWS:Resources:SnsTopicArn"]
        ?? throw new InvalidOperationException("SNS Topic ARN is not configured");

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