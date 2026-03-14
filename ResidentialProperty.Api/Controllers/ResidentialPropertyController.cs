using ResidentialProperty.Api.Services.ResidentialPropertyGeneratorService;
using ResidentialProperty.Domain.Entities;
using Microsoft.AspNetCore.Mvc;

namespace ResidentialProperty.Api.Controllers;

[Route("api/[controller]")]
[ApiController]
public class ResidentialPropertyController(
    IResidentialPropertyGeneratorService generatorService,
    ILogger<ResidentialPropertyController> logger) : ControllerBase
{
    /// <summary>
    /// Получить объект жилого строительства по ID, если не найден в кэше — сгенерировать новый
    /// </summary>
    /// <param name="id">ID объекта</param>
    /// <param name="cancellationToken">Токен отмены операции</param>
    /// <returns>Объект жилого строительства</returns>
    [HttpGet]
    public async Task<ActionResult<ResidentialPropertyEntity>> GetById([FromQuery] int id, CancellationToken cancellationToken)
    {
        logger.LogInformation("Received request to retrieve/generate property {Id}", id);

        var property = await generatorService.GetByIdAsync(id, cancellationToken);

        return Ok(property);
    }
}