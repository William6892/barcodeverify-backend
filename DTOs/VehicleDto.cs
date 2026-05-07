// DTOs/VehicleDto.cs
using System.ComponentModel.DataAnnotations;

namespace BarcodeShippingSystem.DTOs
{
    // Respuesta de vehículo
    public class VehicleResponseDto
    {
        public int Id { get; set; }
        public string PlateNumber { get; set; } = string.Empty;
        public string? TrailerPlate { get; set; }
        public string? VehicleType { get; set; } // "Furgón", "Mula", "Camión"
        public int TransportCompanyId { get; set; }
        public string TransportCompanyName { get; set; } = string.Empty;
        public bool IsActive { get; set; }
        public DateTime CreatedAt { get; set; }

        // Propiedad calculada para mostrar
        public string DisplayName => TrailerPlate != null && VehicleType == "Mula"
            ? $"{PlateNumber} + Trailer: {TrailerPlate}"
            : PlateNumber;
    }

    // Para crear vehículo
    public class CreateVehicleDto
    {
        [Required(ErrorMessage = "La placa es requerida")]
        [StringLength(20)]
        [RegularExpression(@"^[A-Z0-9\-]+$", ErrorMessage = "Formato de placa inválido. Use solo letras mayúsculas, números y guiones")]
        public string PlateNumber { get; set; } = string.Empty;

        [StringLength(20)]
        [RegularExpression(@"^[A-Z0-9\-]+$", ErrorMessage = "Formato de placa de trailer inválido")]
        public string? TrailerPlate { get; set; }

        [StringLength(30)]
        [RegularExpression("^(Furgón|Mula|Camión|Trailer)$", ErrorMessage = "Tipo inválido. Use: Furgón, Mula, Camión o Trailer")]
        public string? VehicleType { get; set; }

        [Required(ErrorMessage = "La transportadora es requerida")]
        public int TransportCompanyId { get; set; }
    }

    // Para actualizar vehículo
    public class UpdateVehicleDto
    {
        [StringLength(20)]
        public string? PlateNumber { get; set; }

        [StringLength(20)]
        public string? TrailerPlate { get; set; }

        [StringLength(30)]
        public string? VehicleType { get; set; }

        public int? TransportCompanyId { get; set; }
        public bool? IsActive { get; set; }
    }
}