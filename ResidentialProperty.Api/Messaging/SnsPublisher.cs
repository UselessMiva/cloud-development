using Amazon.SimpleNotificationService;
using Amazon.SimpleNotificationService.Model;
using ResidentialProperty.Domain.Entities;
using System.Text.Json;

namespace ResidentialProperty.Api.Messaging;

public class SnsPublisher : ISnsPublisher
{
    private readonly IAmazonSimpleNotificationService _snsClient;
    private readonly string _topicArn;
    private readonly ILogger<SnsPublisher> _logger;

    public SnsPublisher(
        IAmazonSimpleNotificationService snsClient,
        IConfiguration configuration,
        ILogger<SnsPublisher> logger)
    {
        _snsClient = snsClient;
        _logger = logger;
        _topicArn = configuration["AWS:Resources:SNSTopicArn"]
            ?? throw new InvalidOperationException("SNS Topic ARN is not configured (AWS:Resources:SNSTopicArn)");
    }

    public async Task Publish(ResidentialPropertyEntity dto)
    {
        var message = JsonSerializer.Serialize(dto);

        _logger.LogInformation("Publishing property {Id} to SNS topic {TopicArn}", dto.Id, _topicArn);

        await _snsClient.PublishAsync(new PublishRequest
        {
            TopicArn = _topicArn,
            Message = message
        });
    }
}
