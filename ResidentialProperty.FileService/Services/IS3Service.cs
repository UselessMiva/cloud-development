using System.Text.Json.Nodes;

namespace ResidentialProperty.FileService.Services;

/// <summary>
/// Служба для манипуляции файлами в объектном хранилище
/// </summary>
public interface IS3Service
{
    /// <summary>
    /// Сохраняет JSON-представление сущности в бакет
    /// </summary>
    /// <param name="fileData">Сериализованная сущность в JSON</param>
    public Task<bool> UploadFile(string fileData);

    /// <summary>
    /// Возвращает список ключей файлов в бакете
    /// </summary>
    public Task<List<string>> GetFileList();

    /// <summary>
    /// Возвращает JSON-документ по ключу
    /// </summary>
    /// <param name="key">Ключ файла в бакете</param>
    public Task<JsonNode> DownloadFile(string key);

    /// <summary>
    /// Создаёт бакет при необходимости
    /// </summary>
    public Task EnsureBucketExists();
}
