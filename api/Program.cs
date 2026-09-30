using System.Text.Json.Serialization;
using Microsoft.EntityFrameworkCore;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddDbContext<ApiDb>(opt => opt.UseInMemoryDatabase("TireList"));
builder.Services.ConfigureHttpJsonOptions(options =>
    options.SerializerOptions.Converters.Add(new JsonStringEnumConverter()));
builder.Services.AddOpenApi();
var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference();
}

static Dictionary<string, string[]> ValidateTire(Tire tire)
{
    var errors = new Dictionary<string, string[]>();

    // 1) string: Brand
    if (string.IsNullOrWhiteSpace(tire.Brand))
        errors["Brand"] = ["Brand is required."];
    else if (tire.Brand.Trim().Length is < 2 or > 50)
        errors["Brand"] = ["Brand must be between 2 and 50 characters."];

    // 2) enum: Type
    if (tire.Type is not TireType type)
        errors["Type"] = ["Type is required. Allowed values: Summer, Winter, AllSeason."];
    else if (!Enum.IsDefined(type))
        errors["Type"] = ["Type must be one of: Summer, Winter, AllSeason."];

    // 3) int: RimDiameter
    if (tire.RimDiameter is < 13 or > 24)
        errors["RimDiameter"] = ["RimDiameter must be between 13 and 24 inches."];

    // 4) decimal: Price
    if (tire.Price <= 0 || tire.Price > 10000)
        errors["Price"] = ["Price must be greater than 0 and at most 10000."];
    else if (decimal.Round(tire.Price, 2) != tire.Price)
        errors["Price"] = ["Price can have at most 2 decimal places."];

    // 5) URL (string): ImageUrl
    if (string.IsNullOrWhiteSpace(tire.ImageUrl))
        errors["ImageUrl"] = ["ImageUrl is required."];
    else if (!Uri.TryCreate(tire.ImageUrl, UriKind.Absolute, out var uri) ||
             (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps))
        errors["ImageUrl"] = ["ImageUrl must be a valid absolute http(s) URL."];

    return errors;
}

app.MapGet("/tires", async (ApiDb db) =>
    await db.Tires.ToListAsync());

app.MapGet("/tires/{id}", async (int id, ApiDb db) =>
    await db.Tires.FindAsync(id)
        is Tire tire
            ? Results.Ok(tire)
            : Results.NotFound());

app.MapPost("/tires", async (Tire tire, ApiDb db) =>
{
    var errors = ValidateTire(tire);
    if (errors.Count > 0) return Results.ValidationProblem(errors);

    db.Tires.Add(tire);
    await db.SaveChangesAsync();

    return Results.Created($"/tires/{tire.Id}", tire);
})
.ProducesValidationProblem();

app.MapPut("/tires/{id}", async (int id, Tire inputTire, ApiDb db) =>
{
    var errors = ValidateTire(inputTire);
    if (errors.Count > 0) return Results.ValidationProblem(errors);

    var tire = await db.Tires.FindAsync(id);

    if (tire is null) return Results.NotFound();

    tire.Brand = inputTire.Brand;
    tire.Type = inputTire.Type;
    tire.RimDiameter = inputTire.RimDiameter;
    tire.Price = inputTire.Price;
    tire.ImageUrl = inputTire.ImageUrl;

    await db.SaveChangesAsync();

    return Results.NoContent();
})
.ProducesValidationProblem();

app.MapDelete("/tires/{id}", async (int id, ApiDb db) =>
{
    if (await db.Tires.FindAsync(id) is Tire tire)
    {
        db.Tires.Remove(tire);
        await db.SaveChangesAsync();
        return Results.NoContent();
    }

    return Results.NotFound();
});

app.Run();
