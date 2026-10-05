using Microsoft.EntityFrameworkCore;

internal static class TireUploadEndpoints
{
    public static async Task<IResult> SaveAsync(int? id, HttpRequest request, ApiDb db,
        R2Storage storage, ILoggerFactory loggerFactory, IConfiguration configuration,
        CancellationToken cancellationToken)
    {
        if (!configuration.GetValue("Uploads:Enabled", true))
            return Results.Problem("Uploads are disabled for this demo.", statusCode: 503);

        var isJson = request.HasJsonContentType();
        if (!isJson && !request.HasFormContentType)
            return Results.Problem("Send application/json with imageUrl, or multipart/form-data with an image file.", statusCode: 415);

        IFormCollection? form = null;
        Tire? jsonInput = null;
        try
        {
            if (isJson) jsonInput = await TireJson.ReadAsync(request, cancellationToken);
            else form = await request.ReadFormAsync(cancellationToken);
        }
        catch (System.Text.Json.JsonException) { return Results.Problem("Invalid JSON body or field types.", statusCode: 400); }
        catch (InvalidDataException) { return Results.Problem("Invalid or oversized upload.", statusCode: 400); }
        catch (BadHttpRequestException exception)
        {
            return Results.Problem("Invalid or oversized upload.", statusCode: exception.StatusCode);
        }

        var existing = id.HasValue ? await db.Tires.FindAsync([id.Value], cancellationToken) : null;
        if (id.HasValue && existing is null) return Results.NotFound();
        var errors = new Dictionary<string, string[]>();
        if (isJson && jsonInput is null) return Results.Problem("A JSON object is required.", statusCode: 400);
        var input = isJson ? jsonInput! : TireForm.Read(form!, errors);
        if (isJson)
        {
            foreach (var error in TireValidator.Validate(input)) errors.TryAdd(error.Key, error.Value);
            TireJson.ValidateImageUrl(input.ImageUrl, !string.IsNullOrWhiteSpace(existing?.ImageUrl), errors);
        }
        var image = form?.Files.GetFile("image");
        string? extension = null;
        if (form is not null && (form.Files.Count > 1 || (form.Files.Count == 1 && image is null)))
            errors["Image"] = ["Upload one file in the image field."];
        else if (image is not null)
        {
            extension = await ImageUpload.ValidateAsync(image, cancellationToken);
            if (extension is null) errors["Image"] = ["Choose a JPEG, PNG, or WebP image between 1 byte and 5 MB."];
        }
        else if (!isJson && (existing is null || string.IsNullOrWhiteSpace(existing.ImageUrl)))
            errors["Image"] = ["An image is required."];
        if (errors.Count > 0) return Results.ValidationProblem(errors);

        string? newKey = null;
        if (image is not null)
        {
            if (!storage.IsConfigured) return Results.Problem("Image storage is not configured. Set the R2 environment variables on the API.", statusCode: 503);
            try
            {
                var uploaded = await storage.UploadAsync(image, extension!, cancellationToken);
                newKey = uploaded.Key;
                input.ImageKey = newKey;
                input.ImageUrl = uploaded.Url;
            }
            catch (Exception exception) when (exception is not OperationCanceledException)
            {
                loggerFactory.CreateLogger("TireUpload").LogError(exception, "R2 image upload failed.");
                return Results.Problem("Image upload failed. Please try again.", statusCode: 503);
            }
        }

        var oldKey = existing?.ImageKey;
        var tire = existing ?? input;
        if (existing is null) db.Tires.Add(tire);
        else
        {
            tire.Brand = input.Brand;
            tire.Type = input.Type;
            tire.RimDiameter = input.RimDiameter;
            tire.Price = input.Price;
            if (newKey is not null || (isJson && input.ImageUrl is not null && input.ImageUrl != existing.ImageUrl))
            {
                tire.ImageUrl = input.ImageUrl;
                tire.ImageKey = newKey;
            }
        }
        try { await db.SaveChangesAsync(cancellationToken); }
        catch
        {
            await storage.DeleteBestEffortAsync(newKey);
            throw;
        }
        if (oldKey != tire.ImageKey) await storage.DeleteBestEffortAsync(oldKey);
        return existing is null ? Results.Created($"/tires/{tire.Id}", tire) : Results.NoContent();
    }
}

