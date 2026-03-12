using Microsoft.EntityFrameworkCore;
using BarcodeShippingSystem.Data;
using BarcodeShippingSystem.Models;
using BarcodeShippingSystem.DTOs;

namespace BarcodeShippingSystem.Services
{
    public interface ITransportCompanyService
    {
        Task<List<TransportCompanyDto>> GetAllAsync(bool activeOnly = true);
        Task<TransportCompanyDto?> GetByIdAsync(int id);
        Task<TransportCompanyDto> CreateAsync(CreateTransportCompanyDto dto);
        Task<bool> UpdateAsync(int id, UpdateTransportCompanyDto dto);
        Task<bool> DeleteAsync(int id);
        Task<bool> ToggleStatusAsync(int id);
    }

    public class TransportCompanyService : ITransportCompanyService
    {
        private readonly ApplicationDbContext _context;

        public TransportCompanyService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<List<TransportCompanyDto>> GetAllAsync(bool activeOnly = true)
        {
            var query = _context.TransportCompanies.AsQueryable();

            if (activeOnly)
                query = query.Where(tc => tc.IsActive);

            return await query
                .OrderBy(tc => tc.Name)
                .Select(tc => new TransportCompanyDto
                {
                    Id = tc.Id,
                    Name = tc.Name,
                    IsActive = tc.IsActive,
                    CreatedAt = tc.CreatedAt
                })
                .ToListAsync();
        }

        public async Task<TransportCompanyDto?> GetByIdAsync(int id)
        {
            return await _context.TransportCompanies
                .Where(tc => tc.Id == id)
                .Select(tc => new TransportCompanyDto
                {
                    Id = tc.Id,
                    Name = tc.Name,
                    IsActive = tc.IsActive,
                    CreatedAt = tc.CreatedAt
                })
                .FirstOrDefaultAsync();
        }

        public async Task<TransportCompanyDto> CreateAsync(CreateTransportCompanyDto dto)
        {
            var company = new TransportCompany
            {
                Name = dto.Name,
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            };

            _context.TransportCompanies.Add(company);
            await _context.SaveChangesAsync();

            return new TransportCompanyDto
            {
                Id = company.Id,
                Name = company.Name,
                IsActive = company.IsActive,
                CreatedAt = company.CreatedAt
            };
        }

        public async Task<bool> UpdateAsync(int id, UpdateTransportCompanyDto dto)
        {
            var company = await _context.TransportCompanies.FindAsync(id);

            if (company == null) return false;

            if (!string.IsNullOrEmpty(dto.Name)) company.Name = dto.Name;
            if (dto.IsActive.HasValue) company.IsActive = dto.IsActive.Value;

            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> DeleteAsync(int id)
        {
            var company = await _context.TransportCompanies
                .Include(tc => tc.Shipments)
                .FirstOrDefaultAsync(tc => tc.Id == id);

            if (company == null || company.Shipments.Any()) return false;

            _context.TransportCompanies.Remove(company);
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> ToggleStatusAsync(int id)
        {
            var company = await _context.TransportCompanies.FindAsync(id);

            if (company == null) return false;

            company.IsActive = !company.IsActive;
            await _context.SaveChangesAsync();
            return true;
        }
    }
}