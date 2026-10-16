using Amazon.S3;
using Amazon.S3.Model;
using Microsoft.Extensions.Logging.Abstractions;

static void Check(bool condition, string message)
{
    if (!condition) throw new Exception(message);
}

var settings = new Dictionary<string, string?>
{
    ["Supabase:Endpoint"] = "https://example.storage.supabase.co/storage/v1/s3",
    ["Supabase:Region"] = "eu-central-1",
    ["Supabase:BucketName"] = "tire-images",
    ["Supabase:PublicBaseUrl"] = "https://example.supabase.co/storage/v1/object/public/tire-images",
    ["Supabase:AccessKeyId"] = "test-access",
    ["Supabase:SecretAccessKey"] = "test-secret"
};
var logger = NullLogger<SupabaseStorage>.Instance;
foreach (var missing in settings.Keys)
{
    var incomplete = new Dictionary<string, string?>(settings) { [missing] = "" };
    using var invalid = new SupabaseStorage(new ConfigurationBuilder().AddInMemoryCollection(incomplete).Build(), logger);
    Check(!invalid.IsConfigured, $"Reject missing {missing}");
}
using (var configured = new SupabaseStorage(new ConfigurationBuilder().AddInMemoryCollection(settings).Build(), logger))
    Check(configured.IsConfigured, "Accept complete configuration");
foreach (var field in new[] { "Supabase:Endpoint", "Supabase:PublicBaseUrl" })
{
    var insecure = new Dictionary<string, string?>(settings) { [field] = "http://example.com" };
    using var invalid = new SupabaseStorage(new ConfigurationBuilder().AddInMemoryCollection(insecure).Build(), logger);
    Check(!invalid.IsConfigured, "Require HTTPS");
}

var client = new RecordingS3();
using var storage = new SupabaseStorage(client, "tire-images", settings["Supabase:PublicBaseUrl"] + "/", logger);
foreach (var extension in new[] { "jpg", "png", "webp" })
{
    var bytes = new byte[] { 1, 2, 3 };
    var file = new FormFile(new MemoryStream(bytes), 0, bytes.Length, "image", "ignored.txt");
    var uploaded = await storage.UploadAsync(file, extension, default);
    Check(uploaded.Key.StartsWith("tires/") && uploaded.Key.EndsWith("." + extension), "Preserve managed key format");
    Check(uploaded.Url == settings["Supabase:PublicBaseUrl"] + "/" + uploaded.Key, "Public URL uses bucket path");
    Check(client.Put!.BucketName == "tire-images" && client.Put.Key == uploaded.Key, "Upload destination");
    Check(client.Put.ContentType == (extension == "jpg" ? "image/jpeg" : "image/" + extension), "Image content type");
    Check(!client.Put.UseChunkEncoding && client.Put.DisablePayloadSigning == true && client.Put.DisableDefaultChecksumValidation == true, "Compatible S3 upload flags");
    Check(client.Bytes!.SequenceEqual(bytes), "Upload bytes unchanged");
    await storage.DeleteBestEffortAsync(uploaded.Key);
    Check(client.Deleted == uploaded.Key, "Delete managed object");
}
client.Deleted = null;
await storage.DeleteBestEffortAsync(null);
await storage.DeleteBestEffortAsync("external/photo.jpg");
Check(client.Deleted is null, "Ignore unmanaged images");
client.Fail = true;
await storage.DeleteBestEffortAsync("tires/old.jpg");
try
{
    await storage.UploadAsync(new FormFile(new MemoryStream([1]), 0, 1, "image", "a.png"), "png", default);
    throw new Exception("Upload failure was swallowed");
}
catch (AmazonS3Exception) { }
Console.WriteLine("All storage configuration, upload, and cleanup checks passed.");

sealed class RecordingS3 : AmazonS3Client
{
    public RecordingS3() : base("test", "test", new AmazonS3Config { ServiceURL = "https://example.com" }) { }
    public PutObjectRequest? Put;
    public byte[]? Bytes;
    public string? Deleted;
    public bool Fail;
    public override async Task<PutObjectResponse> PutObjectAsync(PutObjectRequest request, CancellationToken cancellationToken = default)
    {
        if (Fail) throw new AmazonS3Exception("Simulated failure");
        Put = request;
        using var copy = new MemoryStream();
        await request.InputStream.CopyToAsync(copy, cancellationToken);
        Bytes = copy.ToArray();
        return new PutObjectResponse();
    }
    public override Task<DeleteObjectResponse> DeleteObjectAsync(string bucketName, string key, CancellationToken cancellationToken = default)
    {
        if (Fail) throw new AmazonS3Exception("Simulated failure");
        if (bucketName != "tire-images") throw new Exception("Incorrect delete bucket");
        Deleted = key;
        return Task.FromResult(new DeleteObjectResponse());
    }
}
