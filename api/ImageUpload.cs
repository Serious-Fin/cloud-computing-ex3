public static class ImageUpload
{
    public const long MaxBytes = 5 * 1024 * 1024;

    public static async Task<string?> ValidateAsync(IFormFile file, CancellationToken cancellationToken)
    {
        if (file.Length is <= 0 or > MaxBytes) return null;
        await using var stream = file.OpenReadStream();
        var header = new byte[12];
        if (await stream.ReadAtLeastAsync(header, 12, false, cancellationToken) < 12) return null;
        if (header[0] == 0xff && header[1] == 0xd8 && header[2] == 0xff) return "jpg";
        if (header.AsSpan(0, 8).SequenceEqual(new byte[] { 137, 80, 78, 71, 13, 10, 26, 10 })) return "png";
        if (header.AsSpan(0, 4).SequenceEqual("RIFF"u8) && header.AsSpan(8, 4).SequenceEqual("WEBP"u8)) return "webp";
        return null;
    }
}
