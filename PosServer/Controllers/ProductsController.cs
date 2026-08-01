using System.Globalization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PosServer.Data;
using PosServer.Models;
using PosServer.Services;

namespace PosServer.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class ProductsController : ControllerBase
    {
        private readonly CentralDbContext _context;
        private readonly ITenantService _tenantService;

        public ProductsController(CentralDbContext context, ITenantService tenantService)
        {
            _context = context;
            _tenantService = tenantService;
        }

        [HttpGet]
        public async Task<IActionResult> GetProducts([FromQuery] int page = 1, [FromQuery] int pageSize = 100)
        {
            var tenantId = _tenantService.GetTenantId();
            var query = _context.Products.Where(p => p.TenantId == tenantId);
            var total = await query.CountAsync();
            var products = await query
                .AsNoTracking()
                .OrderBy(p => p.Id)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();
            return Ok(new { data = products, page, pageSize, total });
        }

        [HttpGet("changes")]
        public async Task<IActionResult> GetChanges([FromQuery] string? since)
        {
            var tenantId = _tenantService.GetTenantId();
            DateTime sinceDateTime = DateTime.MinValue;

            if (!string.IsNullOrWhiteSpace(since))
            {
                if (!DateTime.TryParse(since, CultureInfo.InvariantCulture, DateTimeStyles.AdjustToUniversal, out sinceDateTime))
                {
                    sinceDateTime = DateTime.MinValue;
                }
            }

            var products = await _context.Products
                .AsNoTracking()
                .Where(p => p.TenantId == tenantId && p.LastUpdated > sinceDateTime)
                .ToListAsync();

            return Ok(products);
        }

        [HttpPost]
        public async Task<IActionResult> CreateOrUpdateProduct([FromBody] Product product)
        {
            var tenantId = _tenantService.GetTenantId();
            product.TenantId = tenantId;
            product.LastUpdated = DateTime.UtcNow;

            var existing = await _context.Products
                .FirstOrDefaultAsync(p => p.TenantId == tenantId && p.Barcode == product.Barcode);

            if (existing == null)
            {
                product.Id = 0; // Garantizar ID autonumerado en PostgreSQL
                _context.Products.Add(product);
            }
            else
            {
                existing.Name = product.Name;
                existing.Price = product.Price;
                existing.StockQuantity = product.StockQuantity;
                existing.MinStockThreshold = product.MinStockThreshold;
                existing.Category = product.Category;
                existing.CustomAttributes = product.CustomAttributes;
                existing.LastUpdated = DateTime.UtcNow;
            }

            await _context.SaveChangesAsync();
            return Ok(product);
        }
    }
}
