using System.Text;
using System.Text.Json.Nodes;
using Microsoft.AspNetCore.Mvc;
using ResidentialProperty.FileService.Services;

namespace ResidentialProperty.FileService.Controllers;

/// <summary>
/// Контроллер для чтения файлов из объектного хранилища (MinIO/S3)
/// </summary>
/// <param name="s3Service">Служба для работы с S3</param>
/// <param name="logger">Логгер</param>
[ApiController]
[Route("api/s3")]
public class S3Controller(
    IS3Service s3Service,
    ILogger<S3Controller> logger) : ControllerBase
{
    /// <summary>
    /// Возвращает список ключей файлов в бакете
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(List<string>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<List<string>>> ListFiles()
    {
        logger.LogInformation("Method {Method} was called", nameof(ListFiles));
        try
        {
            var list = await s3Service.GetFileList();
            logger.LogInformation("Got a list of {Count} files from bucket", list.Count);
            return Ok(list);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Exception occurred during {Method}", nameof(ListFiles));
            return StatusCode(500, ex.Message);
        }
    }

    /// <summary>
    /// Возвращает JSON-документ по ключу
    /// </summary>
    /// <param name="key">Ключ файла в бакете</param>
    [HttpGet("{key}")]
    [ProducesResponseType(typeof(JsonNode), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<JsonNode>> GetFile(string key)
    {
        logger.LogInformation("Method {Method} was called with key {Key}", nameof(GetFile), key);
        try
        {
            var node = await s3Service.DownloadFile(key);
            logger.LogInformation("Received json of {Size} bytes",
                Encoding.UTF8.GetByteCount(node.ToJsonString()));
            return Ok(node);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Exception occurred during {Method}", nameof(GetFile));
            return StatusCode(500, ex.Message);
        }
    }
}
