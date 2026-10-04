using Amazon.S3;
using Amazon.S3.Model;

public sealed class R2Storage : IDisposable
{
    private readonly AmazonS3Client? client;
    private readonly string bucket;
    private readonly string publicUrl;
    private readonly ILogger<R2Storage> logger;

    public bool IsConfigured => client is not null;

    public R2Storage(IConfiguration configuration, ILogger<R2Storage> logger)
    {
        this.logger = logger;
        bucket = configuration["R2:BucketName"] ?? "";
        publicUrl = (configuration["R2:PublicBaseUrl"] ?? "").TrimEnd('/');
        var endpoint = configuration["R2:Endpoint"];
        var accessKey = configuration["R2:AccessKeyId"];
        var secret = configuration["R2:SecretAccessKey"];
        if (!string.IsNullOrWhiteSpace(bucket) && !string.IsNullOrWhiteSpace(accessKey) &&
            !string.IsNullOrWhiteSpace(secret) && IsHttps(endpoint) && IsHttps(publicUrl))
            client = new AmazonS3Client(accessKey, secret, new AmazonS3Config
            {
                ServiceURL = endpoint, AuthenticationRegion = "auto", ForcePathStyle = true
            });
        else logger.LogWarning("R2 is not configured. Image uploads will return HTTP 503.");
    }

    private static bool IsHttps(string? value) =>
        Uri.TryCreate(value, UriKind.Absolute, out var uri) && uri.Scheme == "https";

    public async Task<(string Key, string Url)> UploadAsync(IFormFile file, string extension,
        CancellationToken cancellationToken)
    {
        var key = $"tires/{Guid.NewGuid():N}.{extension}";
        await using var stream = file.OpenReadStream();
        await client!.PutObjectAsync(new PutObjectRequest
        {
            BucketName = bucket, Key = key, InputStream = stream,
            ContentType = extension == "jpg" ? "image/jpeg" : $"image/{extension}",
            DisablePayloadSigning = true, DisableDefaultChecksumValidation = true
        }, cancellationToken);
        return (key, $"{publicUrl}/{key}");
    }

    // Only keys created by this app are managed; existing external image URLs are untouched.
    public async Task DeleteBestEffortAsync(string? key)
    {
        if (client is null || key is null || !key.StartsWith("tires/", StringComparison.Ordinal)) return;
        try { await client.DeleteObjectAsync(bucket, key); }
        catch (Exception exception) { logger.LogError(exception, "Could not delete R2 object {Key}. Manual cleanup may be needed.", key); }
    }

    public void Dispose() => client?.Dispose();
}
