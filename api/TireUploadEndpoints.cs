using Microsoft.EntityFrameworkCore;

internal static class TireUploadEndpoints
{
    public static async Task<IResult> SaveAsync(int? id, HttpRequest request, ApiDb db,
        R2Storage storage, ILoggerFactory loggerFactory, CancellationToken cancellationToken)
    {
        if (request.ContentType is null || !request.ContentType.StartsWith("multipart/form-data", StringComparison.OrdinalIgnoreCase))
            return Results.Problem("Send tire fields and an image as multipart/form-data.", statusCode: 415);

        IFormCollection form;
        try { form = await request.ReadFormAsync(cancellationToken); }
        catch (InvalidDataException) { return Results.Problem("Invalid or oversized upload.", statusCode: 400); }
        catch (BadHttpRequestException exception)
        {
            return Results.Problem("Invalid or oversized upload.", statusCode: exception.StatusCode);
        }

        var existing = id.HasValue ? await db.Tires.FindAsync([id.Value], cancellationToken) : null;
        if (id.HasValue && existing is null) return Results.NotFound();
        var errors = new Dictionary<string, string[]>();
        var input = TireForm.Read(form, errors);
        var image = form.Files.GetFile("image");
        string? extension = null;
        if (form.Files.Count > 1 || (form.Files.Count == 1 && image is null))
            errors["Image"] = ["Upload one file in the image field."];
        else if (image is not null)
        {
            extension = await ImageUpload.ValidateAsync(image, cancellationToken);
            if (extension is null) errors["Image"] = ["Choose a JPEG, PNG, or WebP image between 1 byte and 5 MB."];
        }
        else if (existing is null || string.IsNullOrWhiteSpace(existing.ImageUrl))
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
            if (newKey is not null)
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
        if (newKey is not null) await storage.DeleteBestEffortAsync(oldKey);
        return existing is null ? Results.Created($"/tires/{tire.Id}", tire) : Results.NoContent();
    }
}

