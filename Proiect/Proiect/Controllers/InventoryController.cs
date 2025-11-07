// ═══════════════════════════════════════════════════════════════════════════════
// 📦 BOUNDED CONTEXT: INVENTORY MANAGEMENT - API LAYER
// ═══════════════════════════════════════════════════════════════════════════════
// Controller pentru expunerea operațiilor de inventar
// Data: November 7, 2025
// ═══════════════════════════════════════════════════════════════════════════════

using Microsoft.AspNetCore.Mvc;
using Proiect.Application.Inventory.Commands;
using Proiect.Application.Inventory.Handlers;
using Proiect.Infrastructure.Persistence;
using Proiect.Domain.Inventory;

namespace Proiect.Controllers;

[ApiController]
[Route("api/[controller]")]
public class InventoryController : ControllerBase
{
    private readonly InventoryCommandHandlers _commandHandlers;
    private readonly IInventoryRepository _repository;
    
    public InventoryController(
        InventoryCommandHandlers commandHandlers,
        IInventoryRepository repository)
    {
        _commandHandlers = commandHandlers;
        _repository = repository;
    }
    
    // ═══════════════════════════════════════════════════════════════════════════
    // POST: api/inventory
    // Creează un nou item de inventar
    // ═══════════════════════════════════════════════════════════════════════════
    
    [HttpPost]
    public async Task<IActionResult> CreateInventoryItem([FromBody] CreateInventoryItem command)
    {
        try
        {
            var item = await _commandHandlers.HandleCreateInventoryItem(command);
            return CreatedAtAction(nameof(GetInventoryItem), new { sku = item.Sku }, new
            {
                sku = item.Sku,
                totalOnHand = item.TotalOnHand,
                available = item.Available,
                minimumStockLevel = item.MinimumStockLevel,
                reorderPoint = item.ReorderPoint
            });
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }
    
    // ═══════════════════════════════════════════════════════════════════════════
    // GET: api/inventory/{sku}
    // Obține detalii despre un item de inventar
    // ═══════════════════════════════════════════════════════════════════════════
    
    [HttpGet("{sku}")]
    public async Task<IActionResult> GetInventoryItem(string sku)
    {
        var item = await _repository.GetBySkuAsync(sku);
        
        if (item == null)
            return NotFound(new { error = $"Inventory item {sku} not found" });
        
        return Ok(new
        {
            sku = item.Sku,
            totalOnHand = item.TotalOnHand,
            available = item.Available,
            minimumStockLevel = item.MinimumStockLevel,
            reorderPoint = item.ReorderPoint,
            isLowStock = item.IsLowStock,
            needsReorder = item.NeedsReorder,
            reservations = item.Reservations.Select(r => new
            {
                reservationId = r.ReservationId,
                quantity = r.Quantity,
                reason = r.Reason,
                reservedAt = r.ReservedAt,
                expiresAt = r.ExpiresAt,
                isExpired = r.IsExpired
            })
        });
    }
    
    // ═══════════════════════════════════════════════════════════════════════════
    // GET: api/inventory
    // Obține toate itemele de inventar
    // ═══════════════════════════════════════════════════════════════════════════
    
    [HttpGet]
    public async Task<IActionResult> GetAllInventoryItems()
    {
        var items = await _repository.GetAllAsync();
        
        return Ok(items.Select(item => new
        {
            sku = item.Sku,
            totalOnHand = item.TotalOnHand,
            available = item.Available,
            minimumStockLevel = item.MinimumStockLevel,
            reorderPoint = item.ReorderPoint,
            isLowStock = item.IsLowStock,
            needsReorder = item.NeedsReorder,
            reservationsCount = item.Reservations.Count
        }));
    }
    
    // ═══════════════════════════════════════════════════════════════════════════
    // POST: api/inventory/{sku}/reserve
    // COMANDĂ: ReserveStock → EVENIMENT: StockReserved
    // ═══════════════════════════════════════════════════════════════════════════
    
    [HttpPost("{sku}/reserve")]
    public async Task<IActionResult> ReserveStock(string sku, [FromBody] ReserveStockRequest request)
    {
        try
        {
            var command = new ReserveStock(
                sku,
                request.ReservationId ?? Guid.NewGuid(),
                request.Quantity,
                request.Reason ?? "Order placed",
                request.ExpiresAt
            );
            
            await _commandHandlers.HandleReserveStock(command);
            
            return Ok(new
            {
                message = "Stock reserved successfully",
                sku,
                reservationId = command.ReservationId,
                quantity = command.Quantity
            });
        }
        catch (InvalidOperationException ex)
        {
            return NotFound(new { error = ex.Message });
        }
        catch (InsufficientStockException ex)
        {
            return BadRequest(new { error = ex.Message, type = "InsufficientStock" });
        }
        catch (Exception ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }
    
    // ═══════════════════════════════════════════════════════════════════════════
    // POST: api/inventory/{sku}/release
    // COMANDĂ: ReleaseStock → EVENIMENT: StockReleased
    // ═══════════════════════════════════════════════════════════════════════════
    
    [HttpPost("{sku}/release")]
    public async Task<IActionResult> ReleaseStock(string sku, [FromBody] ReleaseStockRequest request)
    {
        try
        {
            var command = new ReleaseStock(
                sku,
                request.ReservationId,
                request.Quantity,
                request.Reason ?? "Order cancelled"
            );
            
            await _commandHandlers.HandleReleaseStock(command);
            
            return Ok(new
            {
                message = "Stock released successfully",
                sku,
                reservationId = command.ReservationId,
                quantity = command.Quantity
            });
        }
        catch (InvalidOperationException ex)
        {
            return NotFound(new { error = ex.Message });
        }
        catch (ReservationNotFoundException ex)
        {
            return NotFound(new { error = ex.Message, type = "ReservationNotFound" });
        }
        catch (Exception ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }
    
    // ═══════════════════════════════════════════════════════════════════════════
    // POST: api/inventory/{sku}/commit
    // COMANDĂ: CommitReservation → EVENIMENT: StockCommitted
    // ═══════════════════════════════════════════════════════════════════════════
    
    [HttpPost("{sku}/commit")]
    public async Task<IActionResult> CommitReservation(string sku, [FromBody] CommitReservationRequest request)
    {
        try
        {
            var command = new CommitReservation(
                sku,
                request.ReservationId,
                request.Reason ?? "Order shipped"
            );
            
            await _commandHandlers.HandleCommitReservation(command);
            
            return Ok(new
            {
                message = "Reservation committed successfully",
                sku,
                reservationId = command.ReservationId
            });
        }
        catch (Exception ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }
    
    // ═══════════════════════════════════════════════════════════════════════════
    // POST: api/inventory/{sku}/increase
    // COMANDĂ: IncreaseStock → EVENIMENT: StockIncreased
    // ═══════════════════════════════════════════════════════════════════════════
    
    [HttpPost("{sku}/increase")]
    public async Task<IActionResult> IncreaseStock(string sku, [FromBody] StockAdjustmentRequest request)
    {
        try
        {
            var command = new IncreaseStock(sku, request.Quantity, request.Reason ?? "Stock received");
            
            await _commandHandlers.HandleIncreaseStock(command);
            
            return Ok(new
            {
                message = "Stock increased successfully",
                sku,
                quantity = command.Quantity
            });
        }
        catch (Exception ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }
    
    // ═══════════════════════════════════════════════════════════════════════════
    // POST: api/inventory/{sku}/decrease
    // COMANDĂ: DecreaseStock → EVENIMENT: StockDecreased
    // ═══════════════════════════════════════════════════════════════════════════
    
    [HttpPost("{sku}/decrease")]
    public async Task<IActionResult> DecreaseStock(string sku, [FromBody] StockAdjustmentRequest request)
    {
        try
        {
            var command = new DecreaseStock(sku, request.Quantity, request.Reason ?? "Stock damaged");
            
            await _commandHandlers.HandleDecreaseStock(command);
            
            return Ok(new
            {
                message = "Stock decreased successfully",
                sku,
                quantity = command.Quantity
            });
        }
        catch (Exception ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }
    
    // ═══════════════════════════════════════════════════════════════════════════
    // POST: api/inventory/{sku}/expire-reservations
    // Expiră manual rezervările pentru un SKU
    // ═══════════════════════════════════════════════════════════════════════════
    
    [HttpPost("{sku}/expire-reservations")]
    public async Task<IActionResult> ExpireReservations(string sku)
    {
        try
        {
            var command = new ExpireReservations(sku);
            await _commandHandlers.HandleExpireReservations(command);
            
            return Ok(new { message = "Expired reservations released", sku });
        }
        catch (Exception ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }
}

// ═══════════════════════════════════════════════════════════════════════════════
// REQUEST DTOs
// ═══════════════════════════════════════════════════════════════════════════════

public record ReserveStockRequest(
    int Quantity,
    Guid? ReservationId = null,
    string? Reason = null,
    DateTime? ExpiresAt = null
);

public record ReleaseStockRequest(
    Guid ReservationId,
    int Quantity,
    string? Reason = null
);

public record CommitReservationRequest(
    Guid ReservationId,
    string? Reason = null
);

public record StockAdjustmentRequest(
    int Quantity,
    string? Reason = null
);
