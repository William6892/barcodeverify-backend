using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using BarcodeShippingSystem.Data;
using BarcodeShippingSystem.Services;

var builder = WebApplication.CreateBuilder(args);

// ¡¡¡SOLUCIÓN PARA POSTGRESQL!!! 
AppContext.SetSwitch("Npgsql.EnableLegacyTimestampBehavior", true);

// Add services to the container.
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Configure CORS (mejor especificar orígenes para producción)
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

// ========== CONEXIÓN A BD CON VARIABLES DE ENTORNO ==========
var connectionString = Environment.GetEnvironmentVariable("DATABASE_URL");

// Si no hay variable de entorno, usa appsettings.json
if (string.IsNullOrEmpty(connectionString))
{
    connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
}

// Convertir URL de Render/PostgreSQL si es necesario
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

// ========== CONFIGURAR JWT CON VARIABLES DE ENTORNO ==========
var jwtKey = Environment.GetEnvironmentVariable("JWT_KEY") 
    ?? builder.Configuration["Jwt:Key"] 
    ?? "FallbackKeyForDevelopmentOnly1234567890!!";

var jwtIssuer = Environment.GetEnvironmentVariable("JWT_ISSUER") 
    ?? builder.Configuration["Jwt:Issuer"] 
    ?? "BarcodeShippingSystemAPI";

var jwtAudience = Environment.GetEnvironmentVariable("JWT_AUDIENCE") 
    ?? builder.Configuration["Jwt:Audience"] 
    ?? "BarcodeShippingClient";

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
        
        // Para desarrollo/testing
        options.Events = new JwtBearerEvents
        {
            OnAuthenticationFailed = context =>
            {
                Console.WriteLine($"🔐 Authentication failed: {context.Exception.Message}");
                return Task.CompletedTask;
            },
            OnTokenValidated = context =>
            {
                Console.WriteLine($"🔐 Token validated for: {context.Principal?.Identity?.Name}");
                return Task.CompletedTask;
            }
        };
    });

// Health check para monitoreo
builder.Services.AddHealthChecks();

var app = builder.Build();

// ========== CONFIGURAR PIPELINE ==========
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "Barcode Shipping System v1");
        c.RoutePrefix = "swagger";
        c.DocumentTitle = "Barcode Shipping API";
        c.EnableDeepLinking();
        c.EnableFilter();
        c.ShowExtensions();
    });
    
    // Debug: mostrar variables de entorno en desarrollo
    Console.WriteLine("\n🔧 ENVIRONMENT VARIABLES:");
    Console.WriteLine($"ASPNETCORE_ENVIRONMENT: {Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT")}");
    Console.WriteLine($"DATABASE_URL exists: {!string.IsNullOrEmpty(Environment.GetEnvironmentVariable("DATABASE_URL"))}");
    Console.WriteLine($"JWT_KEY exists: {!string.IsNullOrEmpty(Environment.GetEnvironmentVariable("JWT_KEY"))}");
}
else
{
    app.UseHttpsRedirection();
}

app.UseCors("AllowAll");
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

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
        time = DateTime.UtcNow
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
        return url; // Si falla, devuelve la original
    }
}

static string MaskPassword(string connectionString)
{
    if (string.IsNullOrEmpty(connectionString)) 
        return "No connection string";
        
    try
    {
        // Buscar Password= y enmascarar
        var start = connectionString.IndexOf("Password=");
        if (start == -1) return connectionString;
        
        start += 9; // "Password=".Length
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

// ========== VERIFICAR SERVICIOS REGISTRADOS ==========
Console.WriteLine("\n✅ SERVICIOS REGISTRADOS:");
Console.WriteLine($"🔐 Authentication: {builder.Services.Any(s => s.ServiceType.Name.Contains("Authentication"))}");
Console.WriteLine($"🗄️ DbContext: {builder.Services.Any(s => s.ServiceType == typeof(ApplicationDbContext))}");
Console.WriteLine($"📦 IProductService: {builder.Services.Any(s => s.ServiceType == typeof(IProductService))}");
Console.WriteLine($"🚚 ITransportCompanyService: {builder.Services.Any(s => s.ServiceType == typeof(ITransportCompanyService))}");

app.Run();