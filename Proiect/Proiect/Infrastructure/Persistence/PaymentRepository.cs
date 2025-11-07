// ═══════════════════════════════════════════════════════════════════════════════
// 💾 PAYMENT REPOSITORIES - INFRASTRUCTURE LAYER
// ═══════════════════════════════════════════════════════════════════════════════

namespace Proiect.Infrastructure.Persistence;

using Proiect.Domain.Payments;

// ═══════════════════════════════════════════════════════════════════════════════
// REPOSITORY INTERFACES
// ═══════════════════════════════════════════════════════════════════════════════

/// <summary>
/// Repository interface pentru Payment Aggregate
/// </summary>
public interface IPaymentRepository
{
    Task<Payment?> GetByIdAsync(Guid paymentId);
    Task<Payment?> GetByOrderIdAsync(Guid orderId);
    Task<List<Payment>> GetRecentPaymentsByEmailAsync(string customerEmail, TimeSpan timeWindow);
    Task SaveAsync(Payment payment);
    Task DeleteAsync(Guid paymentId);
}

/// <summary>
/// Repository interface pentru Refund Aggregate
/// </summary>
public interface IRefundRepository
{
    Task<Refund?> GetByIdAsync(Guid refundId);
    Task<List<Refund>> GetByPaymentIdAsync(Guid paymentId);
    Task SaveAsync(Refund refund);
    Task DeleteAsync(Guid refundId);
}

// ═══════════════════════════════════════════════════════════════════════════════
// IN-MEMORY IMPLEMENTATIONS (pentru development/testing)
// ═══════════════════════════════════════════════════════════════════════════════

/// <summary>
/// In-Memory implementation pentru Payment Repository
/// Folosește o listă în memorie pentru a stoca payments
/// În producție ar trebui înlocuit cu EF Core DbContext
/// </summary>
public class InMemoryPaymentRepository : IPaymentRepository
{
    private static readonly List<Payment> _payments = new();
    private static readonly Dictionary<Guid, string> _orderToEmail = new(); // OrderId -> CustomerEmail mapping

    public Task<Payment?> GetByIdAsync(Guid paymentId)
    {
        var payment = _payments.FirstOrDefault(p => p.PaymentId == paymentId);
        return Task.FromResult(payment);
    }

    public Task<Payment?> GetByOrderIdAsync(Guid orderId)
    {
        var payment = _payments.FirstOrDefault(p => p.OrderId == orderId);
        return Task.FromResult(payment);
    }

    public Task<List<Payment>> GetRecentPaymentsByEmailAsync(string customerEmail, TimeSpan timeWindow)
    {
        var cutoffTime = DateTime.UtcNow - timeWindow;
        
        // Găsește toate OrderId-urile pentru acest email
        var orderIds = _orderToEmail
            .Where(kvp => kvp.Value == customerEmail)
            .Select(kvp => kvp.Key)
            .ToList();
        
        // Găsește toate payments pentru aceste orders create după cutoffTime
        var recentPayments = _payments
            .Where(p => orderIds.Contains(p.OrderId) && p.CreatedDate >= cutoffTime)
            .ToList();
        
        return Task.FromResult(recentPayments);
    }

    public Task SaveAsync(Payment payment)
    {
        // Remove existing payment cu același ID (update)
        _payments.RemoveAll(p => p.PaymentId == payment.PaymentId);
        
        // Add payment
        _payments.Add(payment);
        
        return Task.CompletedTask;
    }

    public Task DeleteAsync(Guid paymentId)
    {
        _payments.RemoveAll(p => p.PaymentId == paymentId);
        return Task.CompletedTask;
    }

    // Helper method pentru a asocia OrderId cu CustomerEmail
    public static void AssociateOrderWithEmail(Guid orderId, string customerEmail)
    {
        _orderToEmail[orderId] = customerEmail;
    }
}

/// <summary>
/// In-Memory implementation pentru Refund Repository
/// </summary>
public class InMemoryRefundRepository : IRefundRepository
{
    private static readonly List<Refund> _refunds = new();

    public Task<Refund?> GetByIdAsync(Guid refundId)
    {
        var refund = _refunds.FirstOrDefault(r => r.RefundId == refundId);
        return Task.FromResult(refund);
    }

    public Task<List<Refund>> GetByPaymentIdAsync(Guid paymentId)
    {
        var refunds = _refunds
            .Where(r => r.PaymentId == paymentId)
            .ToList();
        
        return Task.FromResult(refunds);
    }

    public Task SaveAsync(Refund refund)
    {
        // Remove existing refund cu același ID (update)
        _refunds.RemoveAll(r => r.RefundId == refund.RefundId);
        
        // Add refund
        _refunds.Add(refund);
        
        return Task.CompletedTask;
    }

    public Task DeleteAsync(Guid refundId)
    {
        _refunds.RemoveAll(r => r.RefundId == refundId);
        return Task.CompletedTask;
    }
}

// ═══════════════════════════════════════════════════════════════════════════════
// ENTITY FRAMEWORK CORE IMPLEMENTATIONS (pentru producție)
// ═══════════════════════════════════════════════════════════════════════════════

// Decomentează și adaptează când vei configura EF Core:

/*
public class PaymentRepository : IPaymentRepository
{
    private readonly PaymentDbContext _context;

    public PaymentRepository(PaymentDbContext context)
    {
        _context = context;
    }

    public async Task<Payment?> GetByIdAsync(Guid paymentId)
    {
        return await _context.Payments
            .FirstOrDefaultAsync(p => p.PaymentId == paymentId);
    }

    public async Task<Payment?> GetByOrderIdAsync(Guid orderId)
    {
        return await _context.Payments
            .FirstOrDefaultAsync(p => p.OrderId == orderId);
    }

    public async Task<List<Payment>> GetRecentPaymentsByEmailAsync(string customerEmail, TimeSpan timeWindow)
    {
        var cutoffTime = DateTime.UtcNow - timeWindow;
        
        return await _context.Payments
            .Where(p => p.CustomerEmail == customerEmail && p.CreatedDate >= cutoffTime)
            .ToListAsync();
    }

    public async Task SaveAsync(Payment payment)
    {
        var existing = await _context.Payments
            .FirstOrDefaultAsync(p => p.PaymentId == payment.PaymentId);

        if (existing == null)
        {
            await _context.Payments.AddAsync(payment);
        }
        else
        {
            _context.Entry(existing).CurrentValues.SetValues(payment);
        }

        await _context.SaveChangesAsync();
    }

    public async Task DeleteAsync(Guid paymentId)
    {
        var payment = await GetByIdAsync(paymentId);
        if (payment != null)
        {
            _context.Payments.Remove(payment);
            await _context.SaveChangesAsync();
        }
    }
}

public class RefundRepository : IRefundRepository
{
    private readonly PaymentDbContext _context;

    public RefundRepository(PaymentDbContext context)
    {
        _context = context;
    }

    public async Task<Refund?> GetByIdAsync(Guid refundId)
    {
        return await _context.Refunds
            .FirstOrDefaultAsync(r => r.RefundId == refundId);
    }

    public async Task<List<Refund>> GetByPaymentIdAsync(Guid paymentId)
    {
        return await _context.Refunds
            .Where(r => r.PaymentId == paymentId)
            .ToListAsync();
    }

    public async Task SaveAsync(Refund refund)
    {
        var existing = await _context.Refunds
            .FirstOrDefaultAsync(r => r.RefundId == refund.RefundId);

        if (existing == null)
        {
            await _context.Refunds.AddAsync(refund);
        }
        else
        {
            _context.Entry(existing).CurrentValues.SetValues(refund);
        }

        await _context.SaveChangesAsync();
    }

    public async Task DeleteAsync(Guid refundId)
    {
        var refund = await GetByIdAsync(refundId);
        if (refund != null)
        {
            _context.Refunds.Remove(refund);
            await _context.SaveChangesAsync();
        }
    }
}
*/

