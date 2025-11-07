// ═══════════════════════════════════════════════════════════════════════════════
// 📦 BOUNDED CONTEXT: SHIPPING & DELIVERY - API LAYER
// ═══════════════════════════════════════════════════════════════════════════════
// Controller pentru expunerea operațiilor de shipping & delivery
// Data: November 7, 2025
// ═══════════════════════════════════════════════════════════════════════════════

using Microsoft.AspNetCore.Mvc;
using Proiect.Application.Shipping.Commands;
using Proiect.Application.Shipping.Handlers;
using Proiect.Infrastructure.Persistence;
using Proiect.Domain.Shipping;

namespace Proiect.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ShippingController : ControllerBase
{
    private readonly ShippingCommandHandlers _commandHandlers;
    private readonly IShipmentRepository _repository;
    
    public ShippingController(
        ShippingCommandHandlers commandHandlers,
        IShipmentRepository repository)
    {
        _commandHandlers = commandHandlers;
        _repository = repository;
    }
    
    // ═══════════════════════════════════════════════════════════════════════════
    // POST: api/shipping
    // Creează un nou shipment pentru o comandă
    // ═══════════════════════════════════════════════════════════════════════════
    
    [HttpPost]
    public async Task<IActionResult> CreateShipment([FromBody] CreateShipmentRequest request)
    {
        try
        {
            var address = new DeliveryAddress(
                request.RecipientName,
                request.Street,
                request.City,
                request.PostalCode,
                request.Country,
                request.Phone,
                request.AdditionalInfo);
            
            var command = new CreateShipment(request.OrderId, address);
            var shipment = await _commandHandlers.HandleCreateShipment(command);
            
            return CreatedAtAction(nameof(GetShipment), new { id = shipment.ShipmentId }, new
            {
                shipmentId = shipment.ShipmentId,
                orderId = shipment.OrderId,
                status = shipment.Status.ToString(),
                deliveryAddress = shipment.DeliveryAddress
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
    // GET: api/shipping/{id}
    // Obține detalii despre un shipment
    // ═══════════════════════════════════════════════════════════════════════════
    
    [HttpGet("{id}")]
    public async Task<IActionResult> GetShipment(Guid id)
    {
        var shipment = await _repository.GetByIdAsync(id);
        
        if (shipment == null)
            return NotFound(new { error = $"Shipment {id} not found" });
        
        return Ok(new
        {
            shipmentId = shipment.ShipmentId,
            orderId = shipment.OrderId,
            status = shipment.Status.ToString(),
            deliveryAddress = shipment.DeliveryAddress,
            carrier = shipment.Carrier,
            trackingNumber = shipment.TrackingNumber,
            preparedAt = shipment.PreparedAt,
            shippedAt = shipment.ShippedAt,
            estimatedDeliveryDate = shipment.EstimatedDeliveryDate,
            deliveredAt = shipment.DeliveredAt,
            recipientName = shipment.RecipientName,
            deliveredBy = shipment.DeliveredBy,
            isDelivered = shipment.IsDelivered,
            isDelayed = shipment.IsDelayed,
            trackingEvents = shipment.TrackingEvents.Select(e => new
            {
                timestamp = e.Timestamp,
                description = e.Description,
                location = e.Location
            })
        });
    }
    
    // ═══════════════════════════════════════════════════════════════════════════
    // GET: api/shipping/order/{orderId}
    // Obține shipment-ul pentru o comandă
    // ═══════════════════════════════════════════════════════════════════════════
    
    [HttpGet("order/{orderId}")]
    public async Task<IActionResult> GetShipmentByOrderId(Guid orderId)
    {
        var shipment = await _repository.GetByOrderIdAsync(orderId);
        
        if (shipment == null)
            return NotFound(new { error = $"No shipment found for order {orderId}" });
        
        return Ok(new
        {
            shipmentId = shipment.ShipmentId,
            orderId = shipment.OrderId,
            status = shipment.Status.ToString(),
            carrier = shipment.Carrier,
            trackingNumber = shipment.TrackingNumber,
            estimatedDeliveryDate = shipment.EstimatedDeliveryDate,
            isDelivered = shipment.IsDelivered
        });
    }
    
    // ═══════════════════════════════════════════════════════════════════════════
    // GET: api/shipping/track/{trackingNumber}
    // Tracking public prin numărul de urmărire
    // ═══════════════════════════════════════════════════════════════════════════
    
    [HttpGet("track/{trackingNumber}")]
    public async Task<IActionResult> TrackShipment(string trackingNumber)
    {
        var shipment = await _repository.GetByTrackingNumberAsync(trackingNumber);
        
        if (shipment == null)
            return NotFound(new { error = $"No shipment found with tracking number {trackingNumber}" });
        
        return Ok(new
        {
            trackingNumber = shipment.TrackingNumber,
            status = shipment.Status.ToString(),
            carrier = shipment.Carrier,
            shippedAt = shipment.ShippedAt,
            estimatedDeliveryDate = shipment.EstimatedDeliveryDate,
            deliveredAt = shipment.DeliveredAt,
            trackingEvents = shipment.TrackingEvents.Select(e => new
            {
                timestamp = e.Timestamp,
                description = e.Description,
                location = e.Location
            })
        });
    }
    
    // ═══════════════════════════════════════════════════════════════════════════
    // POST: api/shipping/{id}/prepare
    // COMANDĂ: PrepareForShipment → EVENIMENT: ShipmentPrepared
    // ═══════════════════════════════════════════════════════════════════════════
    
    [HttpPost("{id}/prepare")]
    public async Task<IActionResult> PrepareForShipment(Guid id, [FromBody] PrepareShipmentRequest? request = null)
    {
        try
        {
            var command = new PrepareForShipment(id, request?.Notes ?? "");
            await _commandHandlers.HandlePrepareForShipment(command);
            
            return Ok(new { message = "Shipment prepared successfully", shipmentId = id });
        }
        catch (InvalidOperationException ex)
        {
            return NotFound(new { error = ex.Message });
        }
        catch (InvalidShippingCommandException ex)
        {
            return BadRequest(new { error = ex.Message, type = "InvalidCommand" });
        }
        catch (Exception ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }
    
    // ═══════════════════════════════════════════════════════════════════════════
    // POST: api/shipping/{id}/ship
    // COMANDĂ: ShipOrder → EVENIMENT: OrderShipped
    // ═══════════════════════════════════════════════════════════════════════════
    
    [HttpPost("{id}/ship")]
    public async Task<IActionResult> ShipOrder(Guid id, [FromBody] ShipOrderRequest request)
    {
        try
        {
            var command = new ShipOrder(
                id,
                request.Carrier,
                request.TrackingNumber,
                request.EstimatedDeliveryDate);
            
            await _commandHandlers.HandleShipOrder(command);
            
            return Ok(new
            {
                message = "Order shipped successfully",
                shipmentId = id,
                carrier = request.Carrier,
                trackingNumber = request.TrackingNumber
            });
        }
        catch (InvalidOperationException ex)
        {
            return NotFound(new { error = ex.Message });
        }
        catch (InvalidShippingCommandException ex)
        {
            return BadRequest(new { error = ex.Message, type = "InvalidCommand" });
        }
        catch (Exception ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }
    
    // ═══════════════════════════════════════════════════════════════════════════
    // POST: api/shipping/{id}/tracking
    // COMANDĂ: UpdateTracking → EVENIMENT: ShipmentTrackingUpdated
    // ═══════════════════════════════════════════════════════════════════════════
    
    [HttpPost("{id}/tracking")]
    public async Task<IActionResult> UpdateTracking(Guid id, [FromBody] UpdateTrackingRequest request)
    {
        try
        {
            var command = new UpdateTracking(id, request.Location, request.Status, request.Notes ?? "");
            await _commandHandlers.HandleUpdateTracking(command);
            
            return Ok(new { message = "Tracking updated successfully", shipmentId = id });
        }
        catch (InvalidOperationException ex)
        {
            return NotFound(new { error = ex.Message });
        }
        catch (InvalidShippingCommandException ex)
        {
            return BadRequest(new { error = ex.Message, type = "InvalidCommand" });
        }
        catch (Exception ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }
    
    // ═══════════════════════════════════════════════════════════════════════════
    // POST: api/shipping/{id}/deliver
    // COMANDĂ: DeliverOrder → EVENIMENT: OrderDelivered
    // ═══════════════════════════════════════════════════════════════════════════
    
    [HttpPost("{id}/deliver")]
    public async Task<IActionResult> DeliverOrder(Guid id, [FromBody] DeliverOrderRequest request)
    {
        try
        {
            var command = new DeliverOrder(
                id,
                request.RecipientName,
                request.DeliveredBy,
                request.Notes ?? "");
            
            await _commandHandlers.HandleDeliverOrder(command);
            
            return Ok(new
            {
                message = "Order delivered successfully",
                shipmentId = id,
                recipientName = request.RecipientName
            });
        }
        catch (InvalidOperationException ex)
        {
            return NotFound(new { error = ex.Message });
        }
        catch (InvalidShippingCommandException ex)
        {
            return BadRequest(new { error = ex.Message, type = "InvalidCommand" });
        }
        catch (Exception ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }
    
    // ═══════════════════════════════════════════════════════════════════════════
    // POST: api/shipping/{id}/mark-lost
    // COMANDĂ: MarkShipmentAsLost → EVENIMENT: ShipmentLost
    // ═══════════════════════════════════════════════════════════════════════════
    
    [HttpPost("{id}/mark-lost")]
    public async Task<IActionResult> MarkAsLost(Guid id, [FromBody] MarkAsLostRequest request)
    {
        try
        {
            var command = new MarkShipmentAsLost(id, request.Reason);
            await _commandHandlers.HandleMarkShipmentAsLost(command);
            
            return Ok(new { message = "Shipment marked as lost", shipmentId = id });
        }
        catch (Exception ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }
    
    // ═══════════════════════════════════════════════════════════════════════════
    // POST: api/shipping/{id}/mark-returned
    // COMANDĂ: MarkShipmentAsReturned → EVENIMENT: ShipmentReturned
    // ═══════════════════════════════════════════════════════════════════════════
    
    [HttpPost("{id}/mark-returned")]
    public async Task<IActionResult> MarkAsReturned(Guid id, [FromBody] MarkAsReturnedRequest request)
    {
        try
        {
            var command = new MarkShipmentAsReturned(id, request.Reason);
            await _commandHandlers.HandleMarkShipmentAsReturned(command);
            
            return Ok(new { message = "Shipment marked as returned", shipmentId = id });
        }
        catch (Exception ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }
    
    // ═══════════════════════════════════════════════════════════════════════════
    // PUT: api/shipping/{id}/address
    // COMANDĂ: UpdateDeliveryAddress → EVENIMENT: DeliveryAddressUpdated
    // ═══════════════════════════════════════════════════════════════════════════
    
    [HttpPut("{id}/address")]
    public async Task<IActionResult> UpdateAddress(Guid id, [FromBody] UpdateAddressRequest request)
    {
        try
        {
            var newAddress = new DeliveryAddress(
                request.RecipientName,
                request.Street,
                request.City,
                request.PostalCode,
                request.Country,
                request.Phone,
                request.AdditionalInfo);
            
            var command = new UpdateDeliveryAddress(id, newAddress);
            await _commandHandlers.HandleUpdateDeliveryAddress(command);
            
            return Ok(new { message = "Delivery address updated successfully", shipmentId = id });
        }
        catch (InvalidShippingCommandException ex)
        {
            return BadRequest(new { error = ex.Message, type = "CannotUpdateAfterShipping" });
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

public record CreateShipmentRequest(
    Guid OrderId,
    string RecipientName,
    string Street,
    string City,
    string PostalCode,
    string Country,
    string? Phone = null,
    string? AdditionalInfo = null
);

public record PrepareShipmentRequest(string? Notes = null);

public record ShipOrderRequest(
    string Carrier,
    string TrackingNumber,
    DateTime? EstimatedDeliveryDate = null
);

public record UpdateTrackingRequest(
    string Location,
    string Status,
    string? Notes = null
);

public record DeliverOrderRequest(
    string RecipientName,
    string DeliveredBy,
    string? Notes = null
);

public record MarkAsLostRequest(string Reason);

public record MarkAsReturnedRequest(string Reason);

public record UpdateAddressRequest(
    string RecipientName,
    string Street,
    string City,
    string PostalCode,
    string Country,
    string? Phone = null,
    string? AdditionalInfo = null
);

