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
    public class OrdersController : ControllerBase
    {
        private readonly CentralDbContext _context;
        private readonly ITenantService _tenantService;

        public OrdersController(CentralDbContext context, ITenantService tenantService)
        {
            _context = context;
            _tenantService = tenantService;
        }

        [HttpPost]
        public async Task<IActionResult> CreateOrder([FromBody, System.ComponentModel.DataAnnotations.Required] Order order)
        {
            if (order == null)
                return BadRequest("Payload de la orden es nulo.");

            var tenantId = _tenantService.GetTenantId();
            order.TenantId = tenantId;

            // Validar idempotencia si viene ClientSideId
            if (!string.IsNullOrEmpty(order.ClientSideId) && await _context.Orders.AnyAsync(o => o.ClientSideId == order.ClientSideId && o.TenantId == tenantId))
            {
                var existingOrder = await _context.Orders.FirstOrDefaultAsync(o => o.ClientSideId == order.ClientSideId && o.TenantId == tenantId);
                return Ok(new { Message = "La orden ya había sido registrada anteriormente (Idempotencia).", ServerOrderId = existingOrder?.Id });
            }

            // Resetear el ID de la Orden para evitar conflicto de Clave Primaria en PostgreSQL
            order.Id = 0;

            if (order.Items != null && order.Items.Any())
            {
                foreach (var item in order.Items)
                {
                    // Validar integridad referencial (Producto)
                    var productExists = await _context.Products.AnyAsync(p => p.Barcode == item.ProductBarcode && p.TenantId == tenantId);
                    if (!productExists)
                    {
                        return BadRequest(new { Message = $"El producto con código de barras {item.ProductBarcode} no existe en el catálogo central." });
                    }

                    item.TenantId = tenantId;
                    item.Id = 0;      // Resetear ID del ítem
                    item.OrderId = 0; // Desvincular clave foránea asignada en el SQLite local
                    item.Product = null!; // Avoid detached entity conflicts
                }
            }
            else
            {
                order.Items = new List<OrderItem>();
            }

            var strategy = _context.Database.CreateExecutionStrategy();
            return await strategy.ExecuteAsync(async () =>
            {
                using var transaction = await _context.Database.BeginTransactionAsync();
                try
                {
                    _context.Orders.Add(order);
                    await _context.SaveChangesAsync();
                    await transaction.CommitAsync();
                    return Ok(new { Message = "Orden sincronizada exitosamente", ServerOrderId = order.Id });
                }
                catch (Exception ex)
                {
                    await transaction.RollbackAsync();
                    Console.WriteLine("ERROR CreateOrder: " + ex.ToString());
                    return StatusCode(500, new { 
                        Error = "Error interno al guardar la orden en PostgreSQL", 
                        Details = ex.Message, 
                        InnerError = ex.InnerException?.Message 
                    });
                }
            });
        }

        [HttpGet]
        public async Task<IActionResult> GetOrders([FromQuery] int page = 1, [FromQuery] int pageSize = 50)
        {
            var tenantId = _tenantService.GetTenantId();
            
            var query = _context.Orders
                .AsNoTracking()
                .Where(o => o.TenantId == tenantId)
                .OrderByDescending(o => o.OrderDate).ThenByDescending(o => o.Id);
                
            var total = await query.CountAsync();
            var orders = await query
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Include(o => o.Items)
                .ToListAsync();
                
            return Ok(new { data = orders, page, pageSize, total });
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetOrder(int id)
        {
            var tenantId = _tenantService.GetTenantId();
            var order = await _context.Orders
                .AsNoTracking()
                .Include(o => o.Items)
                .FirstOrDefaultAsync(o => o.Id == id && o.TenantId == tenantId);

            if (order == null)
                return NotFound();

            return Ok(order);
        }
    }
}
