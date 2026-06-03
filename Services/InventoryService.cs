using Microsoft.EntityFrameworkCore;
using BarcodeShippingSystem.Data;
using BarcodeShippingSystem.DTOs;
using BarcodeShippingSystem.Models;

namespace BarcodeShippingSystem.Services
{
    public class InventoryService : IInventoryService
    {
        private readonly ApplicationDbContext _context;

        public InventoryService(ApplicationDbContext context)
        {
            _context = context;
        }

        // ✅ CORREGIDO: Ahora coincide con lo que llama ProductService
        public async Task RecordExitAsync(int productId, int quantity, int referenceId, string referenceType, int userId, string? notes = null)
        {
            var transaction = new InventoryTransaction
            {
                ProductId = productId,
                Type = "OUT",
                Quantity = quantity,
                ReferenceId = referenceId,
                ReferenceType = referenceType,
                UserId = userId,
                Notes = notes ?? $"Salida por {referenceType} #{referenceId}",
                CreatedAt = DateTime.UtcNow
            };

            _context.InventoryTransactions.Add(transaction);
            await _context.SaveChangesAsync();
        }

        // ✅ CORREGIDO: Inicializar inventario
        public async Task InitializeInventoryAsync(int productId, int initialStock, int userId)
        {
            // Verificar si ya existe alguna transacción para este producto
            var exists = await _context.InventoryTransactions
                .AnyAsync(t => t.ProductId == productId);

            if (exists)
                return; // Ya tiene movimientos, no inicializar

            var transaction = new InventoryTransaction
            {
                ProductId = productId,
                Type = "IN",
                Quantity = initialStock,
                UserId = userId,
                Notes = "Inventario inicial",
                CreatedAt = DateTime.UtcNow
            };

            _context.InventoryTransactions.Add(transaction);
            await _context.SaveChangesAsync();
        }

        // ✅ Obtener stock actual
        public async Task<int> GetCurrentStockAsync(int productId)
        {
            var transactions = await _context.InventoryTransactions
                .Where(t => t.ProductId == productId)
                .ToListAsync();

            int stock = 0;
            foreach (var t in transactions)
            {
                if (t.Type == "IN")
                    stock += t.Quantity;
                else if (t.Type == "OUT")
                    stock -= t.Quantity;
            }

            return stock;
        }

        // ✅ Reporte diario
        public async Task<List<DailyStockDto>> GetDailyStockSummaryAsync(DateTime date)
        {
            var startDate = date.Date;
            var endDate = startDate.AddDays(1);

            // Obtener todos los productos que tienen movimientos
            var productIds = await _context.InventoryTransactions
                .Select(t => t.ProductId)
                .Distinct()
                .ToListAsync();

            var result = new List<DailyStockDto>();

            foreach (var productId in productIds)
            {
                // Transacciones de hoy
                var todayTransactions = await _context.InventoryTransactions
                    .Where(t => t.ProductId == productId && t.CreatedAt >= startDate && t.CreatedAt < endDate)
                    .ToListAsync();

                // Calcular stock antes de hoy
                var allBeforeToday = await _context.InventoryTransactions
                    .Where(t => t.ProductId == productId && t.CreatedAt < startDate)
                    .ToListAsync();

                int stockBefore = 0;
                foreach (var t in allBeforeToday)
                {
                    if (t.Type == "IN")
                        stockBefore += t.Quantity;
                    else if (t.Type == "OUT")
                        stockBefore -= t.Quantity;
                }

                // Calcular salidas de hoy
                int shippedToday = todayTransactions.Where(t => t.Type == "OUT").Sum(t => t.Quantity);

                // Obtener nombre del producto
                var product = await _context.Products.FindAsync(productId);

                result.Add(new DailyStockDto
                {
                    ProductId = productId,
                    ProductName = product?.Name ?? "Desconocido",
                    Date = startDate,
                    InitialStock = stockBefore,
                    ShippedToday = shippedToday,
                    CurrentStock = stockBefore - shippedToday
                });
            }

            return result.OrderBy(r => r.ProductName).ToList();
        }
    }
}