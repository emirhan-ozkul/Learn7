using System.Transactions;
using lrnWebApi;
using lrnWebApi.Models;
using lrnWebApi.Services;
using lrnWebApi.SettingCS;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.DataProtection;

var builder = WebApplication.CreateBuilder(args);
string dbPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "sentencedbV8.db");
string connectionString = $"Data Source={dbPath}";

DatabaseSettings.ConnectionString = connectionString;


var allowedOrigins = (Environment.GetEnvironmentVariable("CORS_ALLOWED_ORIGINS")
    ?? "http://localhost:5173,http://localhost:3000,http://192.168.1.50:5173")
    .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.WithOrigins(allowedOrigins)
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

builder.Configuration.AddEnvironmentVariables();
builder.WebHost.UseUrls("http://0.0.0.0:5000");
builder.Services.AddControllers();
builder.Services.AddOpenApi();
builder.Services
    .AddAuthentication("Bearer")
    .AddScheme<AuthenticationSchemeOptions, SessionAuthenticationHandler>("Bearer", _ => { });
builder.Services.AddAuthorization();

var dataProtectionKeysPath = Environment.GetEnvironmentVariable("DATA_PROTECTION_KEYS_PATH")
    ?? Path.Combine(AppContext.BaseDirectory, "data-protection-keys");
Directory.CreateDirectory(dataProtectionKeysPath);
builder.Services
    .AddDataProtection()
    .PersistKeysToFileSystem(new DirectoryInfo(dataProtectionKeysPath))
    .SetApplicationName("lrnWebApi");

builder.Services.AddScoped<UserApiKeyStore>();
builder.Services.AddScoped<UserAccountService>();

var app = builder.Build();
try
{
    InitializeDatabase.CreateInitializeDatabase();
}
catch(Exception ex)
{
    Console.WriteLine("Bir ata meydana geldi : DB Initialize in program.cs : " + ex);
}
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

// ⚠️ EN KRİTİK SIRALAMA: UseRouting ile MapControllers arasında olmalı
app.UseRouting(); 

// Parametre vermeden direkt çağırıyoruz, global olarak tüm controller'ları korur
app.UseCors();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();
