using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using BarcodeShippingSystem.Data;
using BarcodeShippingSystem.Services; // ← ¡IMPORTANTE!

var builder = WebApplication.CreateBuilder(args);

// ¡¡¡SOLUCIÓN PARA POSTGRESQL!!! 
AppContext.SetSwitch("Npgsql.EnableLegacyTimestampBehavior", true);

// Add services to the container.
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddScoped<IProductService, ProductService>();

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

// Configurar Entity Framework
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

// ✅✅✅ REGISTRAR TODOS LOS SERVICIOS ✅✅✅
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<ITransportCompanyService, TransportCompanyService>(); // ← ¡ESTA LÍNEA ES CRÍTICA!

// Configure JWT Authentication
var jwtKey = builder.Configuration["Jwt:Key"] ?? "BarcodeShippingSystemSuperSecretKeyForJWTToken1234567890!!";

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = builder.Configuration["Jwt:Issuer"] ?? "BarcodeShippingSystemAPI",
            ValidAudience = builder.Configuration["Jwt:Audience"] ?? "BarcodeShippingClient",
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey)),
            ClockSkew = TimeSpan.Zero
        };
    });

var app = builder.Build();

// Configure the HTTP request pipeline.
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
}

app.UseHttpsRedirection();
app.UseCors("AllowAll");
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

// Verificar servicios registrados (para debug)
Console.WriteLine("\n🔍 VERIFICANDO SERVICIOS REGISTRADOS:");
var serviceProvider = builder.Services.BuildServiceProvider();
try
{
    var transportService = serviceProvider.GetService<ITransportCompanyService>();
    Console.WriteLine($"✅ ITransportCompanyService: {(transportService != null ? "REGISTRADO" : "NO REGISTRADO")}");

    var authService = serviceProvider.GetService<IAuthService>();
    Console.WriteLine($"✅ IAuthService: {(authService != null ? "REGISTRADO" : "NO REGISTRADO")}");
}
catch (Exception ex)
{
    Console.WriteLine($"❌ Error verificando servicios: {ex.Message}");
}

app.Run();