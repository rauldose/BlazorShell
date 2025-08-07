namespace BlazorShell.Application.Services;

public interface IFileStorageService
{
    Task<string> SaveFileAsync(Stream fileStream, string fileName, string? folder = null);
    Task<Stream?> GetFileAsync(string filePath);
    Task<bool> DeleteFileAsync(string filePath);
    Task<bool> FileExistsAsync(string filePath);
    Task<IEnumerable<string>> GetFilesAsync(string folder);
}
