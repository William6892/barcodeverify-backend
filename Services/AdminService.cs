// Services/AdminService.cs
using Microsoft.EntityFrameworkCore;
using BarcodeShippingSystem.Data;

namespace BarcodeShippingSystem.Services
{
    public class AdminService : IAdminService
    {
        private readonly ApplicationDbContext _context;

        public AdminService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<object> GetDashboardStatsAsync(DateTime? startDate, DateTime? endDate)
        {
            var start = startDate ?? DateTime.UtcNow.Date.AddDays(-30);
            var end = endDate ?? DateTime.UtcNow.Date.AddDays(1).AddTicks(-1);

            var totalShipments = await _context.Shipments
                .Where(s => s.CreatedAt >= start && s.CreatedAt <= end)
                .CountAsync();

            var productsQuery = _context.Products
                .Where(p => p.ScannedAt >= start && p.ScannedAt <= end);

            var totalProductsScanned = await productsQuery.AnyAsync()
                ? await productsQuery.SumAsync(p => p.Quantity)
                : 0;

            var totalUsersActive = await _context.Users
                .Where(u => u.IsActive && u.LastLogin >= start)
                .CountAsync();

            var topTransportCompanies = await _context.Shipments
                .Include(s => s.TransportCompany)
                .Where(s => s.CreatedAt >= start && s.CreatedAt <= end)
                .GroupBy(s => s.TransportCompanyId)
                .Select(g => new
                {
                    CompanyId = g.Key,
                    CompanyName = g.First().TransportCompany != null ? g.First().TransportCompany.Name : "Desconocida",
                    ShipmentCount = g.Count(),
                    ProductCount = g.SelectMany(s => s.Products).Sum(p => p.Quantity)
                })
                .OrderByDescending(x => x.ShipmentCount)
                .Take(5)
                .ToListAsync();

            var topUsers = await _context.ScanOperations
                .Include(so => so.User)
                .Where(so => so.StartTime >= start && so.StartTime <= end)
                .GroupBy(so => so.UserId)
                .Select(g => new
                {
                    UserId = g.Key,
                    Username = g.First().User != null ? g.First().User.Username : "Desconocido",
                    Email = g.First().User != null ? g.First().User.Email : "",
                    ScanCount = g.Count(),
                    TotalProductsScanned = g.Sum(so => so.ProductCount),
                    LastScan = g.Max(so => so.StartTime)
                })
                .OrderByDescending(x => x.TotalProductsScanned)
                .Take(5)
                .ToListAsync();

            var topProducts = await _context.Products
                .Where(p => p.ScannedAt >= start && p.ScannedAt <= end)
                .GroupBy(p => p.Barcode)
                .Select(g => new
                {
                    Barcode = g.Key,
                    Name = g.First().Name,
                    Category = g.First().Category,
                    Brand = g.First().Brand,
                    TotalQuantity = g.Sum(p => p.Quantity),
                    ShipmentCount = g.Select(p => p.ShipmentId).Distinct().Count()
                })
                .OrderByDescending(x => x.TotalQuantity)
                .Take(10)
                .ToListAsync();

            var shipmentsByStatus = await _context.Shipments
                .Where(s => s.CreatedAt >= start && s.CreatedAt <= end)
                .GroupBy(s => s.Status)
                .Select(g => new
                {
                    Status = g.Key,
                    Count = g.Count(),
                    Percentage = totalShipments > 0 ? Math.Round((double)g.Count() / totalShipments * 100, 1) : 0
                })
                .ToListAsync();

            var dailyActivity = await _context.ScanOperations
                .Where(so => so.StartTime >= DateTime.UtcNow.Date.AddDays(-7))
                .GroupBy(so => so.StartTime.Date)
                .Select(g => new
                {
                    Date = g.Key,
                    ScanOperations = g.Count(),
                    ProductsScanned = g.Sum(so => so.ProductCount)
                })
                .OrderBy(x => x.Date)
                .ToListAsync();

            return new
            {
                Period = new { Start = start, End = end },
                Summary = new
                {
                    TotalShipments = totalShipments,
                    TotalProductsScanned = totalProductsScanned,
                    TotalUsersActive = totalUsersActive,
                    AvgProductsPerShipment = totalShipments > 0 ? Math.Round((double)totalProductsScanned / totalShipments, 1) : 0
                },
                TopTransportCompanies = topTransportCompanies,
                TopUsers = topUsers,
                TopProducts = topProducts,
                ShipmentsByStatus = shipmentsByStatus,
                DailyActivity = dailyActivity
            };
        }

        public async Task<object> GetAllUsersAsync()
        {
            var users = await _context.Users
                .Select(u => new
                {
                    u.Id,
                    u.Username,
                    u.Email,
                    u.Role,
                    u.IsActive,
                    u.CreatedAt,
                    u.LastLogin,
                    TotalScans = _context.ScanOperations.Count(so => so.UserId == u.Id),
                    TotalProductsScanned = _context.ScanOperations
                        .Where(so => so.UserId == u.Id)
                        .Sum(so => so.ProductCount)
                })
                .OrderByDescending(u => u.CreatedAt)
                .ToListAsync();

            return users;
        }

        public async Task<object> GetAllTransportCompaniesAsync()
        {
            var companies = await _context.TransportCompanies
                .Select(tc => new
                {
                    tc.Id,
                    tc.Name,
                    tc.IsActive,
                    tc.CreatedAt,
                    TotalShipments = _context.Shipments.Count(s => s.TransportCompanyId == tc.Id),
                    TotalProducts = _context.Shipments
                        .Where(s => s.TransportCompanyId == tc.Id)
                        .SelectMany(s => s.Products)
                        .Sum(p => p.Quantity)
                })
                .OrderByDescending(tc => tc.CreatedAt)
                .ToListAsync();

            return companies;
        }

        public async Task<object> SearchProductsAsync(string? barcode, string? name, string? category, DateTime? startDate, DateTime? endDate, int page, int pageSize)
        {
            var query = _context.Products
                .Include(p => p.Shipment)
                    .ThenInclude(s => s.TransportCompany)
                .Include(p => p.ScannedByUser)
                .AsQueryable();

            if (!string.IsNullOrEmpty(barcode))
                query = query.Where(p => p.Barcode.Contains(barcode));

            if (!string.IsNullOrEmpty(name))
                query = query.Where(p => p.Name.Contains(name));

            if (!string.IsNullOrEmpty(category))
                query = query.Where(p => p.Category == category);

            if (startDate.HasValue)
                query = query.Where(p => p.ScannedAt >= startDate.Value);

            if (endDate.HasValue)
                query = query.Where(p => p.ScannedAt <= endDate.Value.AddDays(1).AddTicks(-1));

            var totalCount = await query.CountAsync();
            var products = await query
                .OrderByDescending(p => p.ScannedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(p => new
                {
                    p.Id,
                    p.Barcode,
                    p.Name,
                    p.Description,
                    p.SKU,
                    p.Quantity,
                    p.Category,
                    p.Brand,
                    p.Model,
                    p.SerialNumber,
                    ScannedAt = p.ScannedAt,
                    ScannedBy = p.ScannedByUser != null ? new
                    {
                        p.ScannedByUser.Username,
                        p.ScannedByUser.Email
                    } : null,
                    Shipment = p.Shipment != null ? new
                    {
                        p.Shipment.Id,
                        p.Shipment.ShipmentNumber,
                        p.Shipment.Status,
                        TransportCompany = p.Shipment.TransportCompany != null ? new
                        {
                            p.Shipment.TransportCompany.Name
                        } : null
                    } : null
                })
                .ToListAsync();

            return new
            {
                TotalCount = totalCount,
                Page = page,
                PageSize = pageSize,
                TotalPages = (int)Math.Ceiling(totalCount / (double)pageSize),
                Products = products
            };
        }

        public async Task<object> GenerateShipmentReportAsync(DateTime? startDate, DateTime? endDate)
        {
            var start = startDate ?? DateTime.UtcNow.Date.AddDays(-30);
            var end = endDate ?? DateTime.UtcNow.Date.AddDays(1).AddTicks(-1);

            var shipments = await _context.Shipments
                .Include(s => s.TransportCompany)
                .Include(s => s.Products)
                .Where(s => s.CreatedAt >= start && s.CreatedAt <= end)
                .Select(s => new
                {
                    s.Id,
                    s.ShipmentNumber,
                    s.Status,
                    s.CreatedAt,
                    s.ActualDeparture,
                    TransportCompany = s.TransportCompany != null ? new
                    {
                        s.TransportCompany.Name
                    } : null,
                    TotalProducts = s.Products.Sum(p => p.Quantity),
                    Products = s.Products.Select(p => new
                    {
                        p.Barcode,
                        p.Name,
                        p.Quantity,
                        p.Category,
                        p.Brand
                    }).ToList()
                })
                .ToListAsync();

            return new
            {
                ReportPeriod = new { Start = start, End = end },
                TotalShipments = shipments.Count,
                TotalProducts = shipments.Sum(s => s.TotalProducts),
                Shipments = shipments
            };
        }

        public async Task<object> GetQuickStatsAsync()
        {
            var today = DateTime.UtcNow.Date;
            var tomorrow = today.AddDays(1);

            var shipmentsToday = await _context.Shipments
                .Where(s => s.CreatedAt >= today && s.CreatedAt < tomorrow)
                .CountAsync();

            var productsQuery = _context.Products
                .Where(p => p.ScannedAt >= today && p.ScannedAt < tomorrow);

            var productsToday = await productsQuery.AnyAsync()
                ? await productsQuery.SumAsync(p => p.Quantity)
                : 0;

            var activeUsers = await _context.Users
                .Where(u => u.IsActive)
                .CountAsync();

            var pendingShipments = await _context.Shipments
                .Where(s => s.Status == "Pending")
                .CountAsync();

            var inProgressShipments = await _context.Shipments
                .Where(s => s.Status == "InProgress")
                .CountAsync();

            return new
            {
                Today = new
                {
                    Shipments = shipmentsToday,
                    ProductsScanned = productsToday
                },
                System = new
                {
                    ActiveUsers = activeUsers,
                    PendingShipments = pendingShipments,
                    InProgressShipments = inProgressShipments
                }
            };
        }
    }
}