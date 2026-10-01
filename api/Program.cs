using System.Text.Json.Serialization;
using Microsoft.EntityFrameworkCore;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);
var connectionString = DatabaseConnection.Normalize(
    builder.Configuration.GetConnectionString("Default"));
builder.Services.AddDbContext<ApiDb>(opt =>
    opt.UseNpgsql(connectionString));
builder.Services.ConfigureHttpJsonOptions(options =>
    options.SerializerOptions.Converters.Add(new JsonStringEnumConverter()));
builder.Services.AddOpenApi();

builder.Services.AddCors(options =>
    options.AddDefaultPolicy(policy =>
        policy.AllowAnyOrigin().AllowAnyHeader().AllowAnyMethod()));

var app = builder.Build();

// Initialize fresh databases and apply pending schema changes.
using (var scope = app.Services.CreateScope())
{
    await scope.ServiceProvider.GetRequiredService<ApiDb>().Database.MigrateAsync();
}

app.UseCors();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference();
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
    var errors = TireValidator.Validate(tire);
    if (errors.Count > 0) return Results.ValidationProblem(errors);

    db.Tires.Add(tire);
    await db.SaveChangesAsync();

    return Results.Created($"/tires/{tire.Id}", tire);
})
.ProducesValidationProblem();

app.MapPut("/tires/{id}", async (int id, Tire inputTire, ApiDb db) =>
{
    var errors = TireValidator.Validate(inputTire);
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
