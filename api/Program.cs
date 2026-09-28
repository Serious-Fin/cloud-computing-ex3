using Microsoft.EntityFrameworkCore;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddDbContext<ApiDb>(opt => opt.UseInMemoryDatabase("TireList"));
builder.Services.AddOpenApi();
var app = builder.Build();

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
    db.Tires.Add(tire);
    await db.SaveChangesAsync();

    return Results.Created($"/tires/{tire.Id}", tire);
});

app.MapPut("/tires/{id}", async (int id, Tire inputTire, ApiDb db) =>
{
    var tire = await db.Tires.FindAsync(id);

    if (tire is null) return Results.NotFound();

    tire.Brand = inputTire.Brand;
    tire.Type = inputTire.Type;
    tire.RimDiameter = inputTire.RimDiameter;
    tire.Price = inputTire.Price;
    tire.ImageUrl = inputTire.ImageUrl;

    await db.SaveChangesAsync();

    return Results.NoContent();
});

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
