using System.Text.Json.Serialization;
using Microsoft.EntityFrameworkCore;
using Scalar.AspNetCore;
using System.Threading.RateLimiting;

var builder = WebApplication.CreateBuilder(args);
var connectionString = DatabaseConnection.Normalize(
    builder.Configuration.GetConnectionString("Default"));
builder.Services.AddDbContext<ApiDb>(opt =>
    opt.UseNpgsql(connectionString));
builder.Services.ConfigureHttpJsonOptions(options =>
    options.SerializerOptions.Converters.Add(new JsonStringEnumConverter()));
builder.Services.AddOpenApi();
builder.Services.AddSingleton<R2Storage>();
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    // One shared allowance for all callers, including image replacements.
    options.AddPolicy("tire-writes", context => RateLimitPartition.GetFixedWindowLimiter(
        "all-callers", _ => new FixedWindowRateLimiterOptions
        {
            PermitLimit = 100,
            Window = TimeSpan.FromDays(1),
            QueueLimit = 0
        }));
});
builder.WebHost.ConfigureKestrel(options => options.Limits.MaxRequestBodySize = 6 * 1024 * 1024);
builder.Services.Configure<Microsoft.AspNetCore.Http.Features.FormOptions>(options => options.MultipartBodyLengthLimit = 6 * 1024 * 1024);
builder.Services.AddOptions<TireViewsOptions>()
    .BindConfiguration("TireViews")
    .Validate(options => double.IsFinite(options.IntervalMinutes) &&
        options.IntervalMinutes >= 0.01 && options.IntervalMinutes <= 1440,
        "TireViews:IntervalMinutes must be between 0.01 and 1440.")
    .ValidateOnStart();
builder.Services.AddHostedService<TireViewsWorker>();

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
app.UseRateLimiter();

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

app.MapPost("/tires", (HttpRequest request, ApiDb db, R2Storage storage,
    ILoggerFactory loggerFactory, IConfiguration configuration, CancellationToken cancellationToken) =>
    TireUploadEndpoints.SaveAsync(null, request, db, storage, loggerFactory, configuration, cancellationToken))
    .RequireRateLimiting("tire-writes")
    .Accepts<IFormFile>("multipart/form-data")
    .ProducesValidationProblem();

app.MapPut("/tires/{id}", (int id, HttpRequest request, ApiDb db, R2Storage storage,
    ILoggerFactory loggerFactory, IConfiguration configuration, CancellationToken cancellationToken) =>
    TireUploadEndpoints.SaveAsync(id, request, db, storage, loggerFactory, configuration, cancellationToken))
    .RequireRateLimiting("tire-writes")
    .Accepts<IFormFile>("multipart/form-data")
    .ProducesValidationProblem();
app.MapDelete("/tires/{id}", async (int id, ApiDb db, R2Storage storage) =>
{
    if (await db.Tires.FindAsync(id) is Tire tire)
    {
        db.Tires.Remove(tire);
        await db.SaveChangesAsync();
        await storage.DeleteBestEffortAsync(tire.ImageKey);
        return Results.NoContent();
    }

    return Results.NotFound();
});

app.Run();
