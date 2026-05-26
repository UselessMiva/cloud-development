using Amazon.SimpleNotificationService.Util;
using Microsoft.AspNetCore.Mvc;
using ResidentialProperty.FileService.Services;
using System.Text;

namespace ResidentialProperty.FileService.Controllers;

/// <summary>
/// Контроллер для приёма сообщений AWS SNS и сохранения их в MinIO.
/// </summary>
[ApiController]
[Route("api/sns")]
public class SnsController(
    IS3Service s3Service,
    IConfiguration configuration,
    ILogger<SnsController> logger) : ControllerBase
{
    private readonly string _localstackHost = configuration["AWS:Resources:LocalStackHost"] ?? "localhost";
    private readonly int _localstackPort = configuration.GetValue<int?>("AWS:Resources:LocalStackPort") ?? 4566;

    /// <summary>
    /// Вебхук для приёма сообщений из SNS-топика. Также используется
    /// для подтверждения подписки при инициализации информационного обмена.
    /// </summary>
    /// <remarks>В любом случае возвращает 200</remarks>
    [HttpPost]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> Post()
    {
        logger.LogInformation("SNS webhook was called");
        try
        {
            using var reader = new StreamReader(Request.Body, Encoding.UTF8);
            var jsonContent = await reader.ReadToEndAsync();

            var snsMessage = Message.ParseMessage(jsonContent);

            if (snsMessage.Type == "SubscriptionConfirmation")
            {
                logger.LogInformation("SubscriptionConfirmation was received");
                using var httpClient = new HttpClient();
                var builder = new UriBuilder(new Uri(snsMessage.SubscribeURL))
                {
                    Scheme = "http",
                    Host = _localstackHost,
                    Port = _localstackPort
                };
                var response = await httpClient.GetAsync(builder.Uri);
                if (!response.IsSuccessStatusCode)
                {
                    var body = await response.Content.ReadAsStringAsync();
                    throw new InvalidOperationException(
                        $"SubscriptionConfirmation returned {response.StatusCode}: {body}");
                }
                logger.LogInformation("Subscription was successfully confirmed");
                return Ok();
            }

            if (snsMessage.Type == "Notification")
            {
                await s3Service.UploadFile(snsMessage.MessageText);
                logger.LogInformation("Notification was successfully processed");
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Exception occurred while processing SNS notification");
        }

        return Ok();
    }
}
