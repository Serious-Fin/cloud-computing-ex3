using Amazon.S3;
using Amazon.S3.Model;

public sealed class SupabaseStorage : IDisposable
{
    private readonly IAmazonS3? client;
    private readonly string bucket;
    private readonly string publicUrl;
    private readonly ILogger<SupabaseStorage> logger;

    public bool IsConfigured => client is not null;

    public SupabaseStorage(IConfiguration configuration, ILogger<SupabaseStorage> logger)
    {
        this.logger = logger;
        bucket = configuration["Supabase:BucketName"] ?? "";
        publicUrl = (configuration["Supabase:PublicBaseUrl"] ?? "").TrimEnd('/');
        var endpoint = configuration["Supabase:Endpoint"];
        var accessKey = configuration["Supabase:AccessKeyId"];
        var secret = configuration["Supabase:SecretAccessKey"];
        var region = configuration["Supabase:Region"];
        if (!string.IsNullOrWhiteSpace(bucket) && !string.IsNullOrWhiteSpace(accessKey) &&
            !string.IsNullOrWhiteSpace(secret) && !string.IsNullOrWhiteSpace(region) &&
            IsHttps(endpoint) && IsHttps(publicUrl))
            client = new AmazonS3Client(accessKey, secret, new AmazonS3Config
            {
                ServiceURL = endpoint, AuthenticationRegion = region, ForcePathStyle = true
            });
        else logger.LogWarning("Supabase is not configured. Image uploads will return HTTP 503.");
    }

    internal SupabaseStorage(IAmazonS3 client, string bucket, string publicUrl, ILogger<SupabaseStorage> logger)
    {
        this.client = client;
        this.bucket = bucket;
        this.publicUrl = publicUrl.TrimEnd('/');
        this.logger = logger;
    }

    private static bool IsHttps(string? value) =>
        Uri.TryCreate(value, UriKind.Absolute, out var uri) && uri.Scheme == "https";

    public async Task<(string Key, string Url)> UploadAsync(IFormFile file, string extension,
        CancellationToken cancellationToken)
    {
        if (client is null) throw new InvalidOperationException("Supabase storage is not configured.");
        var key = $"tires/{Guid.NewGuid():N}.{extension}";
        await using var stream = file.OpenReadStream();
        await client.PutObjectAsync(new PutObjectRequest
        {
            BucketName = bucket, Key = key, InputStream = stream,
            ContentType = extension == "jpg" ? "image/jpeg" : $"image/{extension}",
            DisablePayloadSigning = true, DisableDefaultChecksumValidation = true,
            UseChunkEncoding = false
        }, cancellationToken);
        return (key, $"{publicUrl}/{key}");
    }

    // Only keys created by this app are managed; existing external image URLs are untouched.
    public async Task DeleteBestEffortAsync(string? key)
    {
        if (client is null || key is null || !key.StartsWith("tires/", StringComparison.Ordinal)) return;
        try { await client.DeleteObjectAsync(bucket, key); }
        catch (Exception exception) { logger.LogError(exception, "Could not delete Supabase object {Key}. Manual cleanup may be needed.", key); }
    }

    public void Dispose() => client?.Dispose();
}
