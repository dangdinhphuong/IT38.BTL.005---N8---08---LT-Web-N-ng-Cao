using Couppa.Api.Middleware;
using Microsoft.AspNetCore.Http;

namespace Couppa.Api.Services;

public interface IProductImageStorage
{
    Task<IReadOnlyList<string>> SaveAsync(
        IReadOnlyCollection<IFormFile> files,
        CancellationToken cancellationToken = default);

    Task DeleteAsync(
        IReadOnlyCollection<string> imageUrls,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// Lưu ảnh sản phẩm vào wwwroot/uploads/products để StaticFiles phục vụ trực tiếp.
/// Tên file gốc không được dùng để tránh path traversal; file name mới là GUID.
/// </summary>
public sealed class ProductImageStorage : IProductImageStorage
{
    private const long MaxFileSizeBytes = 5 * 1024 * 1024;
    private static readonly HashSet<string> AllowedExtensions =
        new(StringComparer.OrdinalIgnoreCase) { ".jpg", ".jpeg", ".png", ".gif", ".webp" };
    private static readonly HashSet<string> AllowedContentTypes =
        new(StringComparer.OrdinalIgnoreCase) { "image/jpeg", "image/png", "image/gif", "image/webp" };

    private readonly IWebHostEnvironment _environment;

    public ProductImageStorage(IWebHostEnvironment environment)
    {
        _environment = environment;
    }

    public async Task<IReadOnlyList<string>> SaveAsync(
        IReadOnlyCollection<IFormFile> files,
        CancellationToken cancellationToken = default)
    {
        var validFiles = files.Where(file => file.Length > 0).ToList();
        if (validFiles.Count == 0)
        {
            return Array.Empty<string>();
        }

        var webRoot = GetWebRoot();
        var relativeDirectory = Path.Combine("uploads", "products");
        var absoluteDirectory = Path.Combine(webRoot, relativeDirectory);
        Directory.CreateDirectory(absoluteDirectory);

        var savedUrls = new List<string>(validFiles.Count);
        try
        {
            foreach (var file in validFiles)
            {
                Validate(file);

                var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
                var fileName = $"{Guid.NewGuid():N}{extension}";
                var absolutePath = Path.Combine(absoluteDirectory, fileName);

                await using var stream = new FileStream(
                    absolutePath,
                    FileMode.CreateNew,
                    FileAccess.Write,
                    FileShare.None);
                await file.CopyToAsync(stream, cancellationToken);

                savedUrls.Add($"/uploads/products/{fileName}");
            }

            return savedUrls;
        }
        catch
        {
            await DeleteAsync(savedUrls, cancellationToken);
            throw;
        }
    }

    public Task DeleteAsync(
        IReadOnlyCollection<string> imageUrls,
        CancellationToken cancellationToken = default)
    {
        var webRoot = GetWebRoot();
        foreach (var imageUrl in imageUrls)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (!imageUrl.StartsWith("/uploads/products/", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            var fileName = imageUrl["/uploads/products/".Length..];
            if (string.IsNullOrWhiteSpace(fileName)
                || fileName.Contains("..", StringComparison.Ordinal)
                || Path.GetFileName(fileName) != fileName)
            {
                continue;
            }

            var path = Path.Combine(webRoot, "uploads", "products", fileName);
            if (File.Exists(path))
            {
                File.Delete(path);
            }
        }

        return Task.CompletedTask;
    }

    private static void Validate(IFormFile file)
    {
        var extension = Path.GetExtension(file.FileName);
        if (!AllowedExtensions.Contains(extension)
            || !AllowedContentTypes.Contains(file.ContentType))
        {
            throw AppException.Unprocessable(
                "PRODUCT_IMAGE_INVALID",
                "Chỉ chấp nhận file ảnh JPG, JPEG, PNG, GIF hoặc WEBP.");
        }

        if (file.Length > MaxFileSizeBytes)
        {
            throw AppException.Unprocessable(
                "PRODUCT_IMAGE_TOO_LARGE",
                "Mỗi file ảnh không được vượt quá 5 MB.");
        }
    }

    private string GetWebRoot()
    {
        var webRoot = _environment.WebRootPath;
        if (string.IsNullOrWhiteSpace(webRoot))
        {
            webRoot = Path.Combine(_environment.ContentRootPath, "wwwroot");
        }

        return webRoot;
    }
}
