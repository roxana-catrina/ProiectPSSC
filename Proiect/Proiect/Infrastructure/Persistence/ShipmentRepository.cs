// ═══════════════════════════════════════════════════════════════════════════════
// 📦 BOUNDED CONTEXT: SHIPPING & DELIVERY - INFRASTRUCTURE LAYER
// ═══════════════════════════════════════════════════════════════════════════════
// Repository pentru persistarea Shipment
// Data: November 7, 2025
// ═══════════════════════════════════════════════════════════════════════════════

using Proiect.Domain.Shipping;

namespace Proiect.Infrastructure.Persistence;

/// <summary>
/// Interface pentru Repository Pattern - Shipment
/// Abstractizează persistarea agregatului Shipment
/// </summary>
public interface IShipmentRepository
{
    Task<Shipment?> GetByIdAsync(Guid shipmentId);
    Task<Shipment?> GetByOrderIdAsync(Guid orderId);
    Task<Shipment?> GetByTrackingNumberAsync(string trackingNumber);
    Task<IEnumerable<Shipment>> GetAllAsync();
    Task<IEnumerable<Shipment>> GetDelayedShipmentsAsync();
    Task SaveAsync(Shipment shipment);
    Task DeleteAsync(Guid shipmentId);
}

/// <summary>
/// Implementare In-Memory pentru Repository
/// Pentru producție, ar trebui implementat cu EF Core sau alt ORM
/// </summary>
public class InMemoryShipmentRepository : IShipmentRepository
{
    private readonly Dictionary<Guid, Shipment> _store = new();
    private readonly object _lock = new();
    
    public Task<Shipment?> GetByIdAsync(Guid shipmentId)
    {
        lock (_lock)
        {
            _store.TryGetValue(shipmentId, out var shipment);
            return Task.FromResult(shipment);
        }
    }
    
    public Task<Shipment?> GetByOrderIdAsync(Guid orderId)
    {
        lock (_lock)
        {
            var shipment = _store.Values.FirstOrDefault(s => s.OrderId == orderId);
            return Task.FromResult(shipment);
        }
    }
    
    public Task<Shipment?> GetByTrackingNumberAsync(string trackingNumber)
    {
        lock (_lock)
        {
            var shipment = _store.Values.FirstOrDefault(s => 
                s.TrackingNumber != null && 
                s.TrackingNumber.Equals(trackingNumber, StringComparison.OrdinalIgnoreCase));
            return Task.FromResult(shipment);
        }
    }
    
    public Task<IEnumerable<Shipment>> GetAllAsync()
    {
        lock (_lock)
        {
            return Task.FromResult(_store.Values.AsEnumerable());
        }
    }
    
    public Task<IEnumerable<Shipment>> GetDelayedShipmentsAsync()
    {
        lock (_lock)
        {
            var delayed = _store.Values.Where(s => s.IsDelayed).AsEnumerable();
            return Task.FromResult(delayed);
        }
    }
    
    public Task SaveAsync(Shipment shipment)
    {
        if (shipment == null)
            throw new ArgumentNullException(nameof(shipment));
        
        lock (_lock)
        {
            _store[shipment.ShipmentId] = shipment;
        }
        
        return Task.CompletedTask;
    }
    
    public Task DeleteAsync(Guid shipmentId)
    {
        lock (_lock)
        {
            _store.Remove(shipmentId);
        }
        
        return Task.CompletedTask;
    }
}

