// ═══════════════════════════════════════════════════════════════════════════════
// 📦 BOUNDED CONTEXT: INVENTORY MANAGEMENT - INFRASTRUCTURE LAYER
// ═══════════════════════════════════════════════════════════════════════════════
// Repository pentru persistarea InventoryItem
// Data: November 7, 2025
// ═══════════════════════════════════════════════════════════════════════════════

using Proiect.Domain.Inventory;

namespace Proiect.Infrastructure.Persistence;

/// <summary>
/// Interface pentru Repository Pattern
/// Abstractizează persistarea agregatului InventoryItem
/// </summary>
public interface IInventoryRepository
{
    Task<InventoryItem?> GetBySkuAsync(string sku);
    Task<IEnumerable<InventoryItem>> GetAllAsync();
    Task SaveAsync(InventoryItem inventoryItem);
    Task DeleteAsync(string sku);
}

/// <summary>
/// Implementare In-Memory pentru Repository
/// Pentru producție, ar trebui implementat cu EF Core sau alt ORM
/// </summary>
public class InMemoryInventoryRepository : IInventoryRepository
{
    private readonly Dictionary<string, InventoryItem> _store = new();
    private readonly object _lock = new();
    
    public Task<InventoryItem?> GetBySkuAsync(string sku)
    {
        lock (_lock)
        {
            _store.TryGetValue(sku, out var item);
            return Task.FromResult(item);
        }
    }
    
    public Task<IEnumerable<InventoryItem>> GetAllAsync()
    {
        lock (_lock)
        {
            return Task.FromResult(_store.Values.AsEnumerable());
        }
    }
    
    public Task SaveAsync(InventoryItem inventoryItem)
    {
        if (inventoryItem == null)
            throw new ArgumentNullException(nameof(inventoryItem));
        
        lock (_lock)
        {
            _store[inventoryItem.Sku] = inventoryItem;
        }
        
        return Task.CompletedTask;
    }
    
    public Task DeleteAsync(string sku)
    {
        lock (_lock)
        {
            _store.Remove(sku);
        }
        
        return Task.CompletedTask;
    }
}
