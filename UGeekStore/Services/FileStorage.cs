namespace UGeekStore.Services;

/// <summary>Stores uploaded photos/videos under wwwroot/uploads and returns the public path.</summary>
public class FileStorage
{
    private static readonly HashSet<string> Allowed = new(StringComparer.OrdinalIgnoreCase)
    { ".jpg", ".jpeg", ".png", ".webp", ".heic", ".gif", ".mp4", ".mov", ".webm", ".3gp", ".m4v" };

    private readonly IWebHostEnvironment _env;
    public FileStorage(IWebHostEnvironment env) => _env = env;

    public async Task<string?> SaveAsync(IFormFile? file, string folder)
    {
        if (file == null || file.Length == 0) return null;
        var ext = Path.GetExtension(file.FileName);
        if (!Allowed.Contains(ext)) throw new InvalidOperationException("Ֆայլի տեսակը թույլատրված չէ");
        var dir = Path.Combine(_env.WebRootPath, "uploads", folder);
        Directory.CreateDirectory(dir);
        var name = $"{DateTime.UtcNow:yyyyMMddHHmmss}_{Guid.NewGuid():N}{ext.ToLowerInvariant()}";
        await using var fs = File.Create(Path.Combine(dir, name));
        await file.CopyToAsync(fs);
        return $"/uploads/{folder}/{name}";
    }

    public static bool IsVideo(string? path) =>
        path != null && new[] { ".mp4", ".mov", ".webm", ".3gp", ".m4v" }.Contains(Path.GetExtension(path).ToLowerInvariant());
}
