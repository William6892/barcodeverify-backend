using System.ComponentModel.DataAnnotations;

namespace BarcodeShippingSystem.DTOs
{
    // Para crear un nuevo envío
    public class CreateShipmentDto
    {
        [Required(ErrorMessage = "La transportadora es requerida")]
        public int TransportCompanyId { get; set; }

        public string? ShipmentNumber { get; set; }  // Opcional: si ya tienen número
        public DateTime? EstimatedDeparture { get; set; }
        public string? Notes { get; set; }  // Observaciones
    }

    // Para iniciar escaneo
    public class StartShipmentDto
    {
        [Required(ErrorMessage = "El número de envío es requerido")]
        public string ShipmentNumber { get; set; } = string.Empty;
    }

    // Para actualizar estado del envío
    public class UpdateShipmentStatusDto
    {
        [Required(ErrorMessage = "El estado es requerido")]
        [RegularExpression("^(Pending|InProgress|Completed|Cancelled)$",
            ErrorMessage = "Estado inválido. Use: Pending, InProgress, Completed o Cancelled")]
        public string Status { get; set; } = string.Empty;

        public string? Notes { get; set; }  // Razón del cambio de estado
    }

    // Para que Admin pueda editar más campos
    public class AdminUpdateShipmentDto
    {
        public int? TransportCompanyId { get; set; }
        public DateTime? EstimatedDeparture { get; set; }
        public DateTime? ActualDeparture { get; set; }
        public string? Status { get; set; }
        public string? Notes { get; set; }
    }

    // Respuesta de envío
    public class ShipmentResponseDto
    {
        public int Id { get; set; }
        public string ShipmentNumber { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public DateTime? EstimatedDeparture { get; set; }
        public DateTime? ActualDeparture { get; set; }
        public TransportCompanyDto? TransportCompany { get; set; }
        public UserDto? CreatedBy { get; set; }

        public DateTime? StartedAt { get; set; }
        public int ProductCount { get; set; }
        public int TotalQuantity { get; set; }
        public string? Notes { get; set; }
    }
}