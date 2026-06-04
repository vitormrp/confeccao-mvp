using Confeccao.Api.Common.CurrentUser;
using Confeccao.Api.Common.Events;
using Confeccao.Api.Common.Pipeline;
using Confeccao.Api.Common.Pricing;
using Confeccao.Api.Features.Cadastros;
using Confeccao.Api.Features.Cutter;
using Confeccao.Api.Features.Dispatch;
using Confeccao.Api.Features.Financial;
using Confeccao.Api.Features.Laundry;
using Confeccao.Api.Features.Orders;
using Confeccao.Api.Features.Production;
using Confeccao.Api.Features.Reports;
using Confeccao.Api.Features.Sewing;
using Confeccao.Api.Features.Stages;
using Confeccao.Api.Infrastructure;
using Confeccao.Api.Infrastructure.Seed;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

var connectionString = ConnectionStringHelper.Normalize(
    Environment.GetEnvironmentVariable("DATABASE_URL")
    ?? builder.Configuration.GetConnectionString("Postgres")
    ?? throw new InvalidOperationException("Postgres connection string not configured. Set DATABASE_URL or ConnectionStrings:Postgres."));

builder.Services.AddDbContext<ConfeccaoDbContext>(options =>
    options.UseNpgsql(connectionString)
           .UseSnakeCaseNamingConvention());

builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<ICurrentUserContext, HeaderCurrentUserContext>();
builder.Services.AddScoped<PipelineFlowService>();
builder.Services.AddScoped<PipelineEventLog>();
builder.Services.AddScoped<StageCompletionService>();
builder.Services.AddScoped<SewingDispatchService>();
builder.Services.AddScoped<LaundryPackageService>();
builder.Services.AddScoped<PricingEngine>();
builder.Services.AddScoped<CreditGenerator>();

// JSON: serialize enums as strings (lowercase camelCase shouldn't apply since enum names
// are PascalCase domain values; we want round-trippable string names like "Cutting").
builder.Services.ConfigureHttpJsonOptions(opts =>
{
    opts.SerializerOptions.Converters.Add(new System.Text.Json.Serialization.JsonStringEnumConverter(
        namingPolicy: null, allowIntegerValues: true));
});

const string CorsPolicy = "frontend";
var allowedOrigins = (Environment.GetEnvironmentVariable("CORS_ALLOWED_ORIGINS")
                     ?? "http://localhost:3000")
    .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

builder.Services.AddCors(options =>
{
    options.AddPolicy(CorsPolicy, policy =>
        policy.WithOrigins(allowedOrigins)
              .AllowAnyHeader()
              .AllowAnyMethod()
              .WithExposedHeaders("X-User-Id"));
});

var app = builder.Build();

app.UseCors(CorsPolicy);

// Apply pending migrations + seed reference data on startup. Both are idempotent.
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<ConfeccaoDbContext>();
    await db.Database.MigrateAsync();
    await ReferenceDataSeeder.SeedAsync(db);

    if (string.Equals(Environment.GetEnvironmentVariable("SEED_SAMPLE_DATA"), "true",
            StringComparison.OrdinalIgnoreCase))
    {
        var flow = scope.ServiceProvider.GetRequiredService<PipelineFlowService>();
        await SampleDataSeeder.SeedAsync(db, flow);
    }
}

app.MapGet("/api/v1/health", async (ConfeccaoDbContext db) =>
{
    var dbReachable = await db.Database.CanConnectAsync();
    return Results.Ok(new
    {
        status = "ok",
        database = dbReachable ? "reachable" : "unreachable",
        timestamp = DateTimeOffset.UtcNow
    });
});

app.MapCadastrosEndpoints();
app.MapOrdersEndpoints();
app.MapCutterEndpoints();
app.MapProductionEndpoints();
app.MapDispatchEndpoints();
app.MapStagesEndpoints();
app.MapSewingEndpoints();
app.MapLaundryEndpoints();
app.MapFinancialEndpoints();
app.MapReportsEndpoints();

app.Run();

public partial class Program { }
