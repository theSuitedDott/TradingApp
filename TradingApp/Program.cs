using TradingApp.Data;
using TradingApp.Extensions;
using TradingApp.Hubs;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddOpenApi();

// Configure Kestrel endpoints: HTTP on 5181 and HTTPS on 7065 (uses the developer certificate).
// Ensure a developer certificate is trusted: `dotnet dev-certs https --trust` (one-time).
builder.WebHost.ConfigureKestrel(options =>
{
    options.ListenAnyIP(5181); // HTTP
    options.ListenAnyIP(7065, listenOptions => // HTTPS - uses default cert store (dev cert)
    {
        listenOptions.UseHttps();
    });
});

builder.Services.Configure<AdminSeedOptions>(
    builder.Configuration.GetSection(AdminSeedOptions.SectionName));

builder.Services.AddPersistence(builder.Configuration);
builder.Services.AddApplicationServices();
builder.Services.AddPaperTrading(builder.Configuration);
builder.Services.AddMarketDataFeed(builder.Configuration);
builder.Services.AddInstitutionalSetupScanner(builder.Configuration);
builder.Services.AddJwtAuthentication(builder.Configuration);
builder.Services.AddSignalR();
// CORS for local frontend development (Vite / Docker nginx)
builder.Services.AddCors(options =>
{
    options.AddPolicy(name: "AllowFrontendDev", policy =>
    {
        policy.WithOrigins(
                "http://localhost:5173", // Vite
                "http://localhost:5174", // Vite possible alternate port
                "http://127.0.0.1:5173",
                "http://127.0.0.1:5174",
                "https://localhost:3443", // Frontend nginx container
                "http://localhost:3000"   // fallback
            )
            .AllowAnyHeader()
            .AllowAnyMethod()
            .AllowCredentials();
    });
});
// Swagger / OpenAPI
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

await app.MigrateAndSeedAsync();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
    app.MapOpenApi();
}

app.UseHttpsRedirection();
app.UseCors("AllowFrontendDev");
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
app.MapHub<PaperTradingHub>("/hubs/paper-trading");

app.Run();
