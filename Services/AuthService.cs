using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using BarcodeShippingSystem.Data;
using BarcodeShippingSystem.DTOs;
using BarcodeShippingSystem.Models;

namespace BarcodeShippingSystem.Services
{
    public class AuthService : IAuthService
    {
        private readonly ApplicationDbContext _context;
        private readonly IConfiguration _configuration;

        public AuthService(ApplicationDbContext context, IConfiguration configuration)
        {
            _context = context;
            _configuration = configuration;
        }

        public async Task<AuthResponseDto> LoginAsync(LoginDto loginDto)
        {
            Console.WriteLine($"🔍 Intento de login para: {loginDto.Username}");

            var user = await _context.Users
                .FirstOrDefaultAsync(u => u.Username == loginDto.Username && u.IsActive);

            if (user == null)
            {
                Console.WriteLine("❌ Usuario no encontrado o inactivo");
                throw new UnauthorizedAccessException("Credenciales inválidas");
            }

            Console.WriteLine($"✅ Usuario encontrado: {user.Username} (ID: {user.Id})");

            // Verificar contraseña
            bool isPasswordValid = BCrypt.Net.BCrypt.Verify(loginDto.Password, user.PasswordHash);

            if (!isPasswordValid)
            {
                Console.WriteLine("❌ Contraseña incorrecta");
                Console.WriteLine($"   Hash en BD: {user.PasswordHash}");
                Console.WriteLine($"   Contraseña ingresada: {loginDto.Password}");
                throw new UnauthorizedAccessException("Credenciales inválidas");
            }

            Console.WriteLine("✅ Contraseña válida");

            // Actualizar último login
            user.LastLogin = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            var token = GenerateJwtToken(user);

            Console.WriteLine($"✅ Token JWT generado para {user.Username}");

            return new AuthResponseDto
            {
                Token = token,
                Expiration = DateTime.UtcNow.AddDays(7),
                User = new UserDto
                {
                    Id = user.Id,
                    Username = user.Username,
                    Email = user.Email,
                    Role = user.Role,
                    CreatedAt = user.CreatedAt
                }
            };
        }

        public async Task<UserDto> RegisterAsync(RegisterDto registerDto)
        {
            // Verificar si el usuario ya existe
            if (await _context.Users.AnyAsync(u => u.Username == registerDto.Username))
            {
                throw new Exception("El nombre de usuario ya existe");
            }

            if (await _context.Users.AnyAsync(u => u.Email == registerDto.Email))
            {
                throw new Exception("El email ya está registrado");
            }

            var user = new User
            {
                Username = registerDto.Username,
                Email = registerDto.Email,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(registerDto.Password),
                Role = "User", // Solo usuarios normales pueden registrarse
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            };

            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            return new UserDto
            {
                Id = user.Id,
                Username = user.Username,
                Email = user.Email,
                Role = user.Role,
                CreatedAt = user.CreatedAt
            };
        }

        public async Task<UserDto> CreateUserAsync(RegisterDto registerDto, int currentUserId)
        {
            // Verificar que el usuario actual es admin
            var currentUser = await _context.Users.FindAsync(currentUserId);
            if (currentUser == null || currentUser.Role != "Admin")
            {
                throw new UnauthorizedAccessException("No tiene permisos para crear usuarios");
            }

            // Verificar si el usuario ya existe
            if (await _context.Users.AnyAsync(u => u.Username == registerDto.Username))
            {
                throw new Exception("El nombre de usuario ya existe");
            }

            if (await _context.Users.AnyAsync(u => u.Email == registerDto.Email))
            {
                throw new Exception("El email ya está registrado");
            }

            var user = new User
            {
                Username = registerDto.Username,
                Email = registerDto.Email,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(registerDto.Password),
                Role = registerDto.Role,
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            };

            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            return new UserDto
            {
                Id = user.Id,
                Username = user.Username,
                Email = user.Email,
                Role = user.Role,
                CreatedAt = user.CreatedAt
            };
        }

        public async Task<UserDto> GetCurrentUserAsync(int userId)
        {
            var user = await _context.Users.FindAsync(userId);
            if (user == null)
            {
                throw new Exception("Usuario no encontrado");
            }

            return new UserDto
            {
                Id = user.Id,
                Username = user.Username,
                Email = user.Email,
                Role = user.Role,
                CreatedAt = user.CreatedAt
            };
        }

        private string GenerateJwtToken(User user)
        {
            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(
                _configuration["Jwt:Key"] ?? "BarcodeShippingSystemSuperSecretKeyForJWTToken1234567890!!"));

            var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var claims = new[]
            {
                new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new Claim(ClaimTypes.Name, user.Username),
                new Claim(ClaimTypes.Email, user.Email),
                new Claim(ClaimTypes.Role, user.Role)
            };

            var token = new JwtSecurityToken(
                issuer: _configuration["Jwt:Issuer"] ?? "BarcodeShippingSystemAPI",
                audience: _configuration["Jwt:Audience"] ?? "BarcodeShippingClient",
                claims: claims,
                expires: DateTime.UtcNow.AddDays(7),
                signingCredentials: credentials
            );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }
    }
}