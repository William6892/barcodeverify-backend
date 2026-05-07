// DTOs/ShipmentDto.cs - ACTUALIZADO
using System.ComponentModel.DataAnnotations;

namespace BarcodeShippingSystem.DTOs
{
    // Para crear un nuevo envío - ACTUALIZADO
    public class CreateShipmentDto
    {
        [Required(ErrorMessage = "La transportadora es requerida")]
        public int TransportCompanyId { get; set; }

        [Required(ErrorMessage = "El conductor es requerido")]
        public int DriverId { get; set; }

        [Required(ErrorMessage = "El vehículo es requerido")]
        public int VehicleId { get; set; }

        public string? ShipmentNumber { get; set; }  // Opcional: si ya tienen número
        public DateTime? EstimatedDeparture { get; set; }
        public string? Notes { get; set; }  // Observaciones
    }

    // Para iniciar escaneo - SIN CAMBIOS
    public class StartShipmentDto
    {
        [Required(ErrorMessage = "El número de envío es requerido")]
        public string ShipmentNumber { get; set; } = string.Empty;
    }

    // Para actualizar estado del envío - SIN CAMBIOS
    public class UpdateShipmentStatusDto
    {
        [Required(ErrorMessage = "El estado es requerido")]
        [RegularExpression("^(Pending|InProgress|Completed|Cancelled)$",
            ErrorMessage = "Estado inválido. Use: Pending, InProgress, Completed o Cancelled")]
        public string Status { get; set; } = string.Empty;

        public string? Notes { get; set; }
    }

    // Para que Admin pueda editar más campos - ACTUALIZADO
    public class AdminUpdateShipmentDto
    {
        public int? TransportCompanyId { get; set; }
        public int? DriverId { get; set; }
        public int? VehicleId { get; set; }
        public DateTime? EstimatedDeparture { get; set; }
        public DateTime? ActualDeparture { get; set; }
        public string? Status { get; set; }
        public string? Notes { get; set; }
    }

    // Respuesta de envío - ACTUALIZADO (nuevos campos incluidos)
    public class ShipmentResponseDto
    {
        public int Id { get; set; }
        public string ShipmentNumber { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public DateTime? EstimatedDeparture { get; set; }
        public DateTime? ActualDeparture { get; set; }

        // Transportadora
        public TransportCompanyDto? TransportCompany { get; set; }

        // NUEVO: Información del conductor
        public DriverInfoDto? Driver { get; set; }

        // NUEVO: Información del vehículo
        public VehicleInfoDto? Vehicle { get; set; }

        // Usuario que creó
        public UserDto? CreatedBy { get; set; }

        public DateTime? StartedAt { get; set; }
        public int ProductCount { get; set; }
        public int TotalQuantity { get; set; }
        public string? Notes { get; set; }
    }

    // NUEVO: Información resumida del conductor para incluir en ShipmentResponse
    public class DriverInfoDto
    {
        public int Id { get; set; }
        public string IdentificationNumber { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
    }

    // NUEVO: Información resumida del vehículo para incluir en ShipmentResponse
    public class VehicleInfoDto
    {
        public int Id { get; set; }
        public string PlateNumber { get; set; } = string.Empty;
        public string? TrailerPlate { get; set; }
        public string? VehicleType { get; set; }

        // Propiedad útil para mostrar en frontend
        public string DisplayText => TrailerPlate != null && VehicleType == "Mula"
            ? $"{PlateNumber} + {TrailerPlate}"
            : PlateNumber;
    }
}