// ═══════════════════════════════════════════════════════════════════════════════
// 📦 BOUNDED CONTEXT: INVENTORY MANAGEMENT - APPLICATION LAYER (HANDLERS)
// ═══════════════════════════════════════════════════════════════════════════════
// Command Handlers pentru gestionarea inventarului
// Data: November 7, 2025
// ═══════════════════════════════════════════════════════════════════════════════

using Proiect.Application.Inventory.Commands;
using Proiect.Domain.Inventory;
using Proiect.Infrastructure.Persistence;

namespace Proiect.Application.Inventory.Handlers;

/// <summary>
/// Handler pentru comenzile de inventar
/// Orchestrează operațiile între comenzi, agregat și repository
/// </summary>
public class InventoryCommandHandlers
{
    private readonly IInventoryRepository _repository;
    
    public InventoryCommandHandlers(IInventoryRepository repository)
    {
        _repository = repository ?? throw new ArgumentNullException(nameof(repository));
    }
    
    // ═══════════════════════════════════════════════════════════════════════════
    // HANDLER: CreateInventoryItem
    // ═══════════════════════════════════════════════════════════════════════════
    
    public async Task<InventoryItem> HandleCreateInventoryItem(CreateInventoryItem command)
    {
        // Verifică dacă SKU-ul există deja
        var existing = await _repository.GetBySkuAsync(command.Sku);
        if (existing != null)
        {
            throw new InvalidOperationException($"Inventory item with SKU {command.Sku} already exists");
        }
        
        // Creează agregatul
        var inventoryItem = new InventoryItem(
            command.Sku,
            command.InitialStock,
            command.MinimumStockLevel,
            command.ReorderPoint
        );
        
        // Persistă
        await _repository.SaveAsync(inventoryItem);
        
        return inventoryItem;
    }
    
    // ═══════════════════════════════════════════════════════════════════════════
    // HANDLER: ReserveStock
    // Declanșează: StockReserved event
    // ═══════════════════════════════════════════════════════════════════════════
    
    public async Task HandleReserveStock(ReserveStock command)
    {
        // Încarcă agregatul
        var inventoryItem = await _repository.GetBySkuAsync(command.Sku);
        
        if (inventoryItem == null)
        {
            throw new InvalidOperationException($"Inventory item with SKU {command.Sku} not found");
        }
        
        // Execută comanda pe agregat (validări + business logic)
        inventoryItem.Reserve(
            command.ReservationId,
            command.Quantity,
            command.Reason,
            command.ExpiresAt
        );
        
        // Persistă schimbările
        await _repository.SaveAsync(inventoryItem);
        
        // Publică evenimentele (aici ar trebui un event bus)
        // await _eventPublisher.PublishAsync(inventoryItem.UncommittedEvents);
        
        inventoryItem.ClearUncommittedEvents();
    }
    
    // ═══════════════════════════════════════════════════════════════════════════
    // HANDLER: ReleaseStock
    // Declanșează: StockReleased event
    // ═══════════════════════════════════════════════════════════════════════════
    
    public async Task HandleReleaseStock(ReleaseStock command)
    {
        var inventoryItem = await _repository.GetBySkuAsync(command.Sku);
        
        if (inventoryItem == null)
        {
            throw new InvalidOperationException($"Inventory item with SKU {command.Sku} not found");
        }
        
        // Execută comanda
        inventoryItem.Release(
            command.ReservationId,
            command.Quantity,
            command.Reason
        );
        
        // Persistă
        await _repository.SaveAsync(inventoryItem);
        
        // Publică evenimente
        // await _eventPublisher.PublishAsync(inventoryItem.UncommittedEvents);
        
        inventoryItem.ClearUncommittedEvents();
    }
    
    // ═══════════════════════════════════════════════════════════════════════════
    // HANDLER: CommitReservation
    // Declanșează: StockCommitted event
    // ═══════════════════════════════════════════════════════════════════════════
    
    public async Task HandleCommitReservation(CommitReservation command)
    {
        var inventoryItem = await _repository.GetBySkuAsync(command.Sku);
        
        if (inventoryItem == null)
        {
            throw new InvalidOperationException($"Inventory item with SKU {command.Sku} not found");
        }
        
        inventoryItem.CommitReservation(command.ReservationId, command.Reason);
        
        await _repository.SaveAsync(inventoryItem);
        
        inventoryItem.ClearUncommittedEvents();
    }
    
    // ═══════════════════════════════════════════════════════════════════════════
    // HANDLER: IncreaseStock
    // Declanșează: StockIncreased event
    // ═══════════════════════════════════════════════════════════════════════════
    
    public async Task HandleIncreaseStock(IncreaseStock command)
    {
        var inventoryItem = await _repository.GetBySkuAsync(command.Sku);
        
        if (inventoryItem == null)
        {
            throw new InvalidOperationException($"Inventory item with SKU {command.Sku} not found");
        }
        
        inventoryItem.IncreaseStock(command.Quantity, command.Reason);
        
        await _repository.SaveAsync(inventoryItem);
        
        inventoryItem.ClearUncommittedEvents();
    }
    
    // ═══════════════════════════════════════════════════════════════════════════
    // HANDLER: DecreaseStock
    // Declanșează: StockDecreased event
    // ═══════════════════════════════════════════════════════════════════════════
    
    public async Task HandleDecreaseStock(DecreaseStock command)
    {
        var inventoryItem = await _repository.GetBySkuAsync(command.Sku);
        
        if (inventoryItem == null)
        {
            throw new InvalidOperationException($"Inventory item with SKU {command.Sku} not found");
        }
        
        inventoryItem.DecreaseStock(command.Quantity, command.Reason);
        
        await _repository.SaveAsync(inventoryItem);
        
        inventoryItem.ClearUncommittedEvents();
    }
    
    // ═══════════════════════════════════════════════════════════════════════════
    // HANDLER: ExpireReservations
    // Eliberează automat rezervările expirate
    // ═══════════════════════════════════════════════════════════════════════════
    
    public async Task HandleExpireReservations(ExpireReservations command)
    {
        var inventoryItem = await _repository.GetBySkuAsync(command.Sku);
        
        if (inventoryItem == null)
        {
            return; // Nu există item, nimic de făcut
        }
        
        inventoryItem.ExpireReservations();
        
        if (inventoryItem.UncommittedEvents.Any())
        {
            await _repository.SaveAsync(inventoryItem);
            inventoryItem.ClearUncommittedEvents();
        }
    }
    
    // ═══════════════════════════════════════════════════════════════════════════
    // HELPER: ExpireAllReservations
    // Job periodic care expiră toate rezervările din sistem
    // ═══════════════════════════════════════════════════════════════════════════
    
    public async Task ExpireAllReservationsAsync()
    {
        var allItems = await _repository.GetAllAsync();
        
        foreach (var item in allItems)
        {
            item.ExpireReservations();
            
            if (item.UncommittedEvents.Any())
            {
                await _repository.SaveAsync(item);
                item.ClearUncommittedEvents();
            }
        }
    }
}
