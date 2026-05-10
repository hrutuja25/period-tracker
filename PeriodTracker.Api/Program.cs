using PeriodTracker.Api.Repositories;
using PeriodTracker.Api.Services;
using StackExchange.Redis;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy
            .WithOrigins(
                builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>()
                    ?? ["http://localhost:5173", "http://127.0.0.1:5173"])
            .AllowAnyHeader()
            .AllowAnyMethod();
    });
});

var storageProvider = builder.Configuration.GetValue<string>("Storage:Provider") ?? "InMemory";
if (storageProvider.Equals("MySql", StringComparison.OrdinalIgnoreCase))
{
    var mySqlConnectionString = builder.Configuration.GetConnectionString("MySql")
        ?? throw new InvalidOperationException("Missing ConnectionStrings:MySql");

    builder.Services.AddSingleton<IPeriodRepository>(_ =>
        new MySqlPeriodRepository(mySqlConnectionString));
    builder.Services.AddSingleton<IPredictionRepository>(_ =>
        new MySqlPredictionRepository(mySqlConnectionString));
}
else
{
    builder.Services.AddSingleton<IPeriodRepository, InMemoryPeriodRepository>();
    builder.Services.AddSingleton<IPredictionRepository, InMemoryPredictionRepository>();
}

if (builder.Configuration.GetValue("Cache:Enabled", false))
{
    builder.Services.AddSingleton<IConnectionMultiplexer>(_ =>
        ConnectionMultiplexer.Connect(builder.Configuration.GetConnectionString("Redis")
            ?? "localhost:6379"));
    builder.Services.AddSingleton<IPredictionCacheRepository, RedisPredictionCacheRepository>();
}
else
{
    builder.Services.AddSingleton<IPredictionCacheRepository, DisabledPredictionCacheRepository>();
}

builder.Services.AddSingleton<PeriodService>();
builder.Services.AddSingleton<PredictionService>();
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

app.UseCors();
app.UseSwagger();
app.UseSwaggerUI();

app.MapGet("/", () => Results.Redirect("/swagger"));
app.MapControllers();

app.Run();
