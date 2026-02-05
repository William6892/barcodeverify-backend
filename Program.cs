using BarcodeShippingSystem.Data;
using BarcodeShippingSystem.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

// ¡¡¡SOLUCIÓN PARA POSTGRESQL!!! 
AppContext.SetSwitch("Npgsql.EnableLegacyTimestampBehavior", true);

// Add services to the container.
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Configure CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll",
        policy =>
        {
            policy.AllowAnyOrigin()
                  .AllowAnyMethod()
                  .AllowAnyHeader();
        });
});

// ========== CONEXIÓN A BD ==========
var connectionString = Environment.GetEnvironmentVariable("DATABASE_URL");

if (string.IsNullOrEmpty(connectionString))
{
    connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
}

if (!string.IsNullOrEmpty(connectionString) && connectionString.StartsWith("postgres://"))
{
    connectionString = ConvertPostgresUrlToConnectionString(connectionString);
}

Console.WriteLine($"📡 Connection String: {MaskPassword(connectionString)}");

// Configurar Entity Framework con PostgreSQL
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseNpgsql(connectionString));

// ========== REGISTRAR SERVICIOS ==========
builder.Services.AddScoped<IProductService, ProductService>();
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<ITransportCompanyService, TransportCompanyService>();

// ========== CONFIGURAR JWT ==========
var jwtKey = Environment.GetEnvironmentVariable("JWT_KEY")
    ?? builder.Configuration["Jwt:Key"]
    ?? "FallbackKeyForDevelopmentOnly1234567890!!";

var jwtIssuer = Environment.GetEnvironmentVariable("JWT_ISSUER")
    ?? builder.Configuration["Jwt:Issuer"]
    ?? "BarcodeShippingSystemAPI";

var jwtAudience = Environment.GetEnvironmentVariable("JWT_AUDIENCE")
    ?? builder.Configuration["Jwt:Audience"]
    ?? "BarcodeShippingClient";

Console.WriteLine($"🔐 JWT Config - Key: {(string.IsNullOrEmpty(jwtKey) ? "❌ NOT SET" : "✅ SET")}");
Console.WriteLine($"🔐 JWT Config - Issuer: {jwtIssuer}");
Console.WriteLine($"🔐 JWT Config - Audience: {jwtAudience}");

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = jwtIssuer,
            ValidAudience = jwtAudience,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey)),
            ClockSkew = TimeSpan.Zero
        };
    });

// Health check
builder.Services.AddHealthChecks();

var app = builder.Build();

// ========== DIAGNÓSTICO: VER CONTROLLERS ==========
Console.WriteLine("\n🔍 BUSCANDO CONTROLLERS...");
try
{
    var controllerTypes = AppDomain.CurrentDomain.GetAssemblies()
        .SelectMany(a => a.GetTypes())
        .Where(t =>
            t != null &&
            !t.IsAbstract &&
            t.IsClass &&
            (typeof(ControllerBase).IsAssignableFrom(t) ||
             (t.Name.EndsWith("Controller") && t.IsSubclassOf(typeof(ControllerBase))))
        )
        .ToList();

    Console.WriteLine($"📋 Controllers encontrados ({controllerTypes.Count}):");
    foreach (var type in controllerTypes)
    {
        Console.WriteLine($"  ✅ {type.Name} ({type.FullName})");
    }

    if (controllerTypes.Count == 0)
    {
        Console.WriteLine("❌ NO SE ENCONTRARON CONTROLLERS!");
        Console.WriteLine("🔍 Assemblies cargadas:");
        foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
        {
            Console.WriteLine($"  - {assembly.FullName}");
        }
    }
}
catch (Exception ex)
{
    Console.WriteLine($"❌ Error buscando controllers: {ex.Message}");
}

// ========== CONFIGURAR PIPELINE ==========
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "Barcode Shipping System v1");
        c.RoutePrefix = "swagger";
        c.DocumentTitle = "Barcode Shipping API";
    });
}
else
{
    app.UseHttpsRedirection();
}

app.UseCors("AllowAll");
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

// ========== ENDPOINTS DE PRUEBA DIRECTOS ==========
// Endpoint de prueba SIN controller (para diagnóstico)
app.MapGet("/api/test-direct", () =>
{
    return Results.Ok(new
    {
        message = "✅ Endpoint directo funciona",
        time = DateTime.UtcNow,
        environment = app.Environment.EnvironmentName
    });
});

// Endpoint de prueba de shipments SIN controller
app.MapGet("/api/test-shipments", () =>
{
    return Results.Ok(new
    {
        message = "✅ Endpoint de shipments directo",
        shipments = new[]
        {
            new { id = 1, number = "TEST001", status = "Pending" },
            new { id = 2, number = "TEST002", status = "InProgress" }
        }
    });
}).RequireAuthorization();

// Health check endpoint
app.MapHealthChecks("/health");

// Root endpoint
app.MapGet("/", () =>
{
    var env = app.Environment.EnvironmentName;
    return Results.Ok(new
    {
        message = "Barcode Shipping System API",
        version = "1.0",
        environment = env,
        status = "running",
        time = DateTime.UtcNow,
        endpoints = new[]
        {
            "/api/auth/login",
            "/api/test-direct",
            "/api/test-shipments",
            "/health"
        }
    });
});

// ========== FUNCIONES AUXILIARES ==========
static string ConvertPostgresUrlToConnectionString(string url)
{
    try
    {
        var uri = new Uri(url);
        var host = uri.Host;
        var port = uri.Port > 0 ? uri.Port : 5432;
        var database = uri.LocalPath.TrimStart('/');
        var userInfo = uri.UserInfo.Split(':');
        var user = userInfo[0];
        var password = userInfo.Length > 1 ? userInfo[1] : "";

        return $"Host={host};Port={port};Database={database};Username={user};Password={password};SSL Mode=Require;Trust Server Certificate=true";
    }
    catch (Exception ex)
    {
        Console.WriteLine($"❌ Error converting connection string: {ex.Message}");
        return url;
    }
}

static string MaskPassword(string connectionString)
{
    if (string.IsNullOrEmpty(connectionString))
        return "No connection string";

    try
    {
        var start = connectionString.IndexOf("Password=");
        if (start == -1) return connectionString;

        start += 9;
        var end = connectionString.IndexOf(';', start);
        if (end == -1) end = connectionString.Length;

        var password = connectionString.Substring(start, end - start);
        return connectionString.Replace(password, "*****");
    }
    catch
    {
        return "[Connection string masked]";
    }
}

Console.WriteLine("\n🚀 APLICACIÓN INICIANDO...");
app.Run();