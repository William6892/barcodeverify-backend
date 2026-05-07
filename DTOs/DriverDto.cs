// DTOs/DriverDto.cs
using System.ComponentModel.DataAnnotations;

namespace BarcodeShippingSystem.DTOs
{
    // Respuesta de conductor
    public class DriverResponseDto
    {
        public int Id { get; set; }
        public string IdentificationNumber { get; set; } = string.Empty; // Cédula
        public string FullName { get; set; } = string.Empty;
        public int TransportCompanyId { get; set; }
        public string TransportCompanyName { get; set; } = string.Empty;
        public bool IsActive { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    // Para crear conductor
    public class CreateDriverDto
    {
        [Required(ErrorMessage = "El número de cédula es requerido")]
        [StringLength(20, MinimumLength = 5, ErrorMessage = "La cédula debe tener entre 5 y 20 caracteres")]
        public string IdentificationNumber { get; set; } = string.Empty;

        [Required(ErrorMessage = "El nombre completo es requerido")]
        [StringLength(100, MinimumLength = 3, ErrorMessage = "El nombre debe tener entre 3 y 100 caracteres")]
        public string FullName { get; set; } = string.Empty;

        [Required(ErrorMessage = "La transportadora es requerida")]
        public int TransportCompanyId { get; set; }
    }

    // Para actualizar conductor
    public class UpdateDriverDto
    {
        [StringLength(20)]
        public string? IdentificationNumber { get; set; }

        [StringLength(100)]
        public string? FullName { get; set; }

        public int? TransportCompanyId { get; set; }
        public bool? IsActive { get; set; }
    }
}