public static class TireJson
{
    public static async Task<Tire?> ReadAsync(HttpRequest request, CancellationToken cancellationToken)
    {
        var body = await request.ReadFromJsonAsync<Tire>(cancellationToken);
        if (body is null) return null;
        // Clients cannot set IDs, storage keys, or background statistics.
        return new Tire
        {
            Brand = body.Brand?.Trim(), Type = body.Type,
            RimDiameter = body.RimDiameter, Price = body.Price,
            ImageUrl = string.IsNullOrWhiteSpace(body.ImageUrl) ? null : body.ImageUrl.Trim()
        };
    }

    public static void ValidateImageUrl(string? url, bool hasExistingImage,
        Dictionary<string, string[]> errors)
    {
        if (url is null)
        {
            if (!hasExistingImage) errors["ImageUrl"] = ["An imageUrl or image upload is required."];
        }
        else if (!Uri.TryCreate(url, UriKind.Absolute, out var uri) ||
            (uri.Scheme != "https" && uri.Scheme != "http"))
            errors["ImageUrl"] = ["ImageUrl must be an absolute HTTP or HTTPS URL."];
    }
}
