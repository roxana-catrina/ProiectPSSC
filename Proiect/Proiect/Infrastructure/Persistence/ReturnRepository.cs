// ═══════════════════════════════════════════════════════════════════════════════
// 📦 INFRASTRUCTURE LAYER - RETURNS REPOSITORY
// ═══════════════════════════════════════════════════════════════════════════════
// Implementare repository pentru persistența agregatului Return
// Data: November 7, 2025
// ═══════════════════════════════════════════════════════════════════════════════

using ReturnsManagement.Application.Returns.Commands.Handlers;
using ReturnsManagement.Domain.Returns;

namespace ReturnsManagement.Infrastructure.Persistence;

/// <summary>
/// In-Memory Repository Implementation pentru Return Aggregate
/// Într-o aplicație reală, aceasta ar fi înlocuită cu EF Core sau alt ORM
/// </summary>
public class ReturnRepository : IReturnRepository
{
    private readonly Dictionary<Guid, Return> _returns = new();
    private readonly object _lock = new();

    public Task<Return?> GetByIdAsync(Guid returnId)
    {
        lock (_lock)
        {
            _returns.TryGetValue(returnId, out var @return);
            return Task.FromResult(@return);
        }
    }

    public Task<Return?> GetByRmaCodeAsync(string rmaCode)
    {
        lock (_lock)
        {
            var @return = _returns.Values.FirstOrDefault(r => r.RmaCode.Value == rmaCode);
            return Task.FromResult(@return);
        }
    }

    public Task<List<Return>> GetByOrderIdAsync(Guid orderId)
    {
        lock (_lock)
        {
            var returns = _returns.Values.Where(r => r.OrderId == orderId).ToList();
            return Task.FromResult(returns);
        }
    }

    public Task<List<Return>> GetByCustomerIdAsync(Guid customerId)
    {
        lock (_lock)
        {
            var returns = _returns.Values.Where(r => r.CustomerId == customerId).ToList();
            return Task.FromResult(returns);
        }
    }

    public Task AddAsync(Return @return)
    {
        lock (_lock)
        {
            if (_returns.ContainsKey(@return.ReturnId))
            {
                throw new InvalidOperationException($"Return with ID {@return.ReturnId} already exists");
            }

            _returns.Add(@return.ReturnId, @return);
            return Task.CompletedTask;
        }
    }

    public Task UpdateAsync(Return @return)
    {
        lock (_lock)
        {
            if (!_returns.ContainsKey(@return.ReturnId))
            {
                throw new InvalidOperationException($"Return with ID {@return.ReturnId} not found");
            }

            _returns[@return.ReturnId] = @return;
            return Task.CompletedTask;
        }
    }

    public Task<bool> ExistsAsync(Guid returnId)
    {
        lock (_lock)
        {
            return Task.FromResult(_returns.ContainsKey(returnId));
        }
    }

    // Helper methods for testing and debugging
    public Task<int> GetCountAsync()
    {
        lock (_lock)
        {
            return Task.FromResult(_returns.Count);
        }
    }

    public Task ClearAllAsync()
    {
        lock (_lock)
        {
            _returns.Clear();
            return Task.CompletedTask;
        }
    }

    public Task<List<Return>> GetAllAsync()
    {
        lock (_lock)
        {
            return Task.FromResult(_returns.Values.ToList());
        }
    }

    public Task<List<Return>> GetByStatusAsync(ReturnStatus status)
    {
        lock (_lock)
        {
            var returns = _returns.Values.Where(r => r.Status == status).ToList();
            return Task.FromResult(returns);
        }
    }
}

/// <summary>
/// Mock implementation pentru Order Service
/// În realitate, aceasta ar comunica cu Order Management bounded context
/// </summary>
public class MockOrderService : IOrderService
{
    private readonly Dictionary<Guid, OrderDto> _orders = new();

    public MockOrderService()
    {
        // Seed some test data
        SeedTestOrders();
    }

    public Task<bool> OrderExistsAsync(Guid orderId)
    {
        return Task.FromResult(_orders.ContainsKey(orderId));
    }

    public Task<OrderDto?> GetOrderAsync(Guid orderId)
    {
        _orders.TryGetValue(orderId, out var order);
        return Task.FromResult(order);
    }

    public void AddTestOrder(OrderDto order)
    {
        _orders[order.OrderId] = order;
    }

    private void SeedTestOrders()
    {
        // Comandă livrată recent (eligibilă pentru retur)
        var order1 = new OrderDto(
            Guid.Parse("11111111-1111-1111-1111-111111111111"),
            Guid.Parse("22222222-2222-2222-2222-222222222222"),
            "Ion Popescu",
            "ion.popescu@example.com",
            DateTime.UtcNow.AddDays(-5), // Livrată acum 5 zile
            "Delivered",
            299.99m,
            "Card"
        );

        var order2 = new OrderDto(
            Guid.Parse("33333333-3333-3333-3333-333333333333"),
            Guid.Parse("44444444-4444-4444-4444-444444444444"),
            "Maria Ionescu",
            "maria.ionescu@example.com",
            DateTime.UtcNow.AddDays(-20), // Livrată acum 20 zile (la limită pentru retur standard)
            "Delivered",
            150.00m,
            "BankTransfer"
        );

        var order3 = new OrderDto(
            Guid.Parse("55555555-5555-5555-5555-555555555555"),
            Guid.Parse("66666666-6666-6666-6666-666666666666"),
            "Alexandru Stan",
            "alex.stan@example.com",
            DateTime.UtcNow.AddDays(-30), // Livrată acum 30 zile (expirat pentru retur standard)
            "Delivered",
            450.00m,
            "Cash"
        );

        _orders[order1.OrderId] = order1;
        _orders[order2.OrderId] = order2;
        _orders[order3.OrderId] = order3;
    }
}

