// ═══════════════════════════════════════════════════════════════════════════════
// 📦 BOUNDED CONTEXT: SHIPPING & DELIVERY - APPLICATION LAYER (HANDLERS)
// ═══════════════════════════════════════════════════════════════════════════════
// Command Handlers pentru gestionarea shipping-ului
// Data: November 7, 2025
// ═══════════════════════════════════════════════════════════════════════════════

using Proiect.Application.Shipping.Commands;
using Proiect.Domain.Shipping;
using Proiect.Infrastructure.Persistence;

namespace Proiect.Application.Shipping.Handlers;

/// <summary>
/// Handler pentru comenzile de shipping & delivery
/// Orchestrează operațiile între comenzi, agregat și repository
/// </summary>
public class ShippingCommandHandlers
{
    private readonly IShipmentRepository _repository;
    
    public ShippingCommandHandlers(IShipmentRepository repository)
    {
        _repository = repository ?? throw new ArgumentNullException(nameof(repository));
    }
    
    // ═══════════════════════════════════════════════════════════════════════════
    // HANDLER: CreateShipment
    // ═══════════════════════════════════════════════════════════════════════════
    
    public async Task<Shipment> HandleCreateShipment(CreateShipment command)
    {
        // Verifică dacă există deja un shipment pentru această comandă
        var existing = await _repository.GetByOrderIdAsync(command.OrderId);
        if (existing != null)
        {
            throw new InvalidOperationException($"Shipment for order {command.OrderId} already exists");
        }
        
        // Creează agregatul
        var shipment = new Shipment(command.OrderId, command.DeliveryAddress);
        
        // Persistă
        await _repository.SaveAsync(shipment);
        
        return shipment;
    }
    
    // ═══════════════════════════════════════════════════════════════════════════
    // HANDLER: PrepareForShipment
    // Declanșează: ShipmentPrepared event
    // ═══════════════════════════════════════════════════════════════════════════
    
    public async Task HandlePrepareForShipment(PrepareForShipment command)
    {
        var shipment = await _repository.GetByIdAsync(command.ShipmentId);
        
        if (shipment == null)
        {
            throw new InvalidOperationException($"Shipment {command.ShipmentId} not found");
        }
        
        shipment.PrepareForShipment(command.Notes);
        
        await _repository.SaveAsync(shipment);
        
        // Publică evenimente
        // await _eventPublisher.PublishAsync(shipment.UncommittedEvents);
        
        shipment.ClearUncommittedEvents();
    }
    
    // ═══════════════════════════════════════════════════════════════════════════
    // HANDLER: ShipOrder
    // Declanșează: OrderShipped event
    // ═══════════════════════════════════════════════════════════════════════════
    
    public async Task HandleShipOrder(ShipOrder command)
    {
        var shipment = await _repository.GetByIdAsync(command.ShipmentId);
        
        if (shipment == null)
        {
            throw new InvalidOperationException($"Shipment {command.ShipmentId} not found");
        }
        
        shipment.Ship(
            command.Carrier,
            command.TrackingNumber,
            command.EstimatedDeliveryDate);
        
        await _repository.SaveAsync(shipment);
        
        // Publică evenimente (OrderShipped)
        // await _eventPublisher.PublishAsync(shipment.UncommittedEvents);
        
        shipment.ClearUncommittedEvents();
    }
    
    // ═══════════════════════════════════════════════════════════════════════════
    // HANDLER: UpdateTracking
    // Declanșează: ShipmentTrackingUpdated event
    // ═══════════════════════════════════════════════════════════════════════════
    
    public async Task HandleUpdateTracking(UpdateTracking command)
    {
        var shipment = await _repository.GetByIdAsync(command.ShipmentId);
        
        if (shipment == null)
        {
            throw new InvalidOperationException($"Shipment {command.ShipmentId} not found");
        }
        
        shipment.UpdateTrackingStatus(command.Location, command.Status, command.Notes);
        
        await _repository.SaveAsync(shipment);
        
        shipment.ClearUncommittedEvents();
    }
    
    // ═══════════════════════════════════════════════════════════════════════════
    // HANDLER: DeliverOrder
    // Declanșează: OrderDelivered event
    // ═══════════════════════════════════════════════════════════════════════════
    
    public async Task HandleDeliverOrder(DeliverOrder command)
    {
        var shipment = await _repository.GetByIdAsync(command.ShipmentId);
        
        if (shipment == null)
        {
            throw new InvalidOperationException($"Shipment {command.ShipmentId} not found");
        }
        
        shipment.Deliver(command.RecipientName, command.DeliveredBy, command.Notes);
        
        await _repository.SaveAsync(shipment);
        
        // Publică evenimente (OrderDelivered)
        // await _eventPublisher.PublishAsync(shipment.UncommittedEvents);
        
        shipment.ClearUncommittedEvents();
    }
    
    // ═══════════════════════════════════════════════════════════════════════════
    // HANDLER: MarkShipmentAsLost
    // Declanșează: ShipmentLost event
    // ═══════════════════════════════════════════════════════════════════════════
    
    public async Task HandleMarkShipmentAsLost(MarkShipmentAsLost command)
    {
        var shipment = await _repository.GetByIdAsync(command.ShipmentId);
        
        if (shipment == null)
        {
            throw new InvalidOperationException($"Shipment {command.ShipmentId} not found");
        }
        
        shipment.MarkAsLost(command.Reason);
        
        await _repository.SaveAsync(shipment);
        
        shipment.ClearUncommittedEvents();
    }
    
    // ═══════════════════════════════════════════════════════════════════════════
    // HANDLER: MarkShipmentAsReturned
    // Declanșează: ShipmentReturned event
    // ═══════════════════════════════════════════════════════════════════════════
    
    public async Task HandleMarkShipmentAsReturned(MarkShipmentAsReturned command)
    {
        var shipment = await _repository.GetByIdAsync(command.ShipmentId);
        
        if (shipment == null)
        {
            throw new InvalidOperationException($"Shipment {command.ShipmentId} not found");
        }
        
        shipment.MarkAsReturned(command.Reason);
        
        await _repository.SaveAsync(shipment);
        
        shipment.ClearUncommittedEvents();
    }
    
    // ═══════════════════════════════════════════════════════════════════════════
    // HANDLER: UpdateDeliveryAddress
    // Declanșează: DeliveryAddressUpdated event
    // ═══════════════════════════════════════════════════════════════════════════
    
    public async Task HandleUpdateDeliveryAddress(UpdateDeliveryAddress command)
    {
        var shipment = await _repository.GetByIdAsync(command.ShipmentId);
        
        if (shipment == null)
        {
            throw new InvalidOperationException($"Shipment {command.ShipmentId} not found");
        }
        
        shipment.UpdateDeliveryAddress(command.NewAddress);
        
        await _repository.SaveAsync(shipment);
        
        shipment.ClearUncommittedEvents();
    }
    
    // ═══════════════════════════════════════════════════════════════════════════
    // QUERY HELPERS
    // ═══════════════════════════════════════════════════════════════════════════
    
    public async Task<Shipment?> GetShipmentByIdAsync(Guid shipmentId)
    {
        return await _repository.GetByIdAsync(shipmentId);
    }
    
    public async Task<Shipment?> GetShipmentByOrderIdAsync(Guid orderId)
    {
        return await _repository.GetByOrderIdAsync(orderId);
    }
    
    public async Task<Shipment?> GetShipmentByTrackingNumberAsync(string trackingNumber)
    {
        return await _repository.GetByTrackingNumberAsync(trackingNumber);
    }
}
