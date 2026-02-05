using System.ComponentModel.DataAnnotations;

namespace BarcodeShippingSystem.DTOs
{
    public class CreateUserDto
    {
        [Required(ErrorMessage = "El nombre de usuario es obligatorio")]
        [StringLength(100, MinimumLength = 3, ErrorMessage = "El nombre debe tener entre 3 y 100 caracteres")]
        public string Username { get; set; } = string.Empty;

        [Required(ErrorMessage = "El email es obligatorio")]
        [EmailAddress(ErrorMessage = "El email no tiene un formato válido")]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "La contraseña es obligatoria")]
        [StringLength(100, MinimumLength = 6, ErrorMessage = "La contraseña debe tener al menos 6 caracteres")]
        public string Password { get; set; } = string.Empty;

        // CAMBIA ESTA LÍNEA - quita el valor por defecto
        [Required(ErrorMessage = "El rol es obligatorio")]
        [RegularExpression("^(User|Admin|Scanner)$", ErrorMessage = "Rol debe ser User, Admin o Scanner")]
        public string Role { get; set; } = string.Empty; // ← Sin valor por defecto
    }

    public class UpdateUserRoleDto
    {
        [Required(ErrorMessage = "El rol es obligatorio")]
        [RegularExpression("^(User|Admin|Scanner)$", ErrorMessage = "Rol debe ser User, Admin o Scanner")]
        public string Role { get; set; } = string.Empty;
    }

    public class UpdateUserStatusDto
    {
        [Required(ErrorMessage = "El estado es obligatorio")]
        public bool IsActive { get; set; }
    }
}