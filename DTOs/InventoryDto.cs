// DTOs/InventoryDto.cs
namespace BarcodeShippingSystem.DTOs
{
    public class DailyStockDto
    {
        public int ProductId { get; set; }
        public string? ProductName { get; set; }
        public DateTime Date { get; set; }
        public int InitialStock { get; set; }
        public int ShippedToday { get; set; }
        public int CurrentStock { get; set; }
    }
}