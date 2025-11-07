// ═══════════════════════════════════════════════════════════════════════════════
// 📚 REPOSITORY INTERFACE & IMPLEMENTATION - ORDER MANAGEMENT CONTEXT
// ═══════════════════════════════════════════════════════════════════════════════

namespace OrderManagement.Infrastructure.Persistence;

using Microsoft.EntityFrameworkCore;
using OrderManagement.Domain.Orders;

/// <summary>
/// Repository Interface pentru Order Aggregate
/// Follows Aggregate Repository Pattern - lucrează DOAR cu Aggregate Root
/// </summary>
public interface IOrderRepository
{
    /// <summary>
    /// Obține o comandă după OrderId
    /// </summary>
    Task<Order?> GetByIdAsync(Guid orderId);

    /// <summary>
    /// Salvează sau actualizează o comandă
    /// </summary>
    Task SaveAsync(Order order);

    /// <summary>
    /// Obține toate comenzile unui client
    /// </summary>
    Task<List<Order>> GetByCustomerIdAsync(Guid customerId);

    /// <summary>
    /// Obține comenzi după status
    /// </summary>
    Task<List<Order>> GetOrdersByStatusAsync(OrderStatus status);

    /// <summary>
    /// Verifică dacă o comandă există
    /// </summary>
    Task<bool> ExistsAsync(Guid orderId);

    /// <summary>
    /// Obține comenzi plasate într-un interval de timp
    /// </summary>
    Task<List<Order>> GetOrdersByDateRangeAsync(DateTime startDate, DateTime endDate);

    /// <summary>
    /// Șterge o comandă (soft delete)
    /// </summary>
    Task DeleteAsync(Guid orderId);
}

// ═══════════════════════════════════════════════════════════════════════════════
// IMPLEMENTARE REPOSITORY CU ENTITY FRAMEWORK CORE
// ═══════════════════════════════════════════════════════════════════════════════

public class OrderRepository : IOrderRepository
{
    private readonly OrderManagementDbContext _context;

    public OrderRepository(OrderManagementDbContext context)
    {
        _context = context;
    }

    public async Task<Order?> GetByIdAsync(Guid orderId)
    {
        // Include OrderItems - eager loading pentru agregat complet
        return await _context.Orders
            .Include(o => o.OrderItems)
            .FirstOrDefaultAsync(o => o.OrderId == orderId);
    }

    public async Task SaveAsync(Order order)
    {
        var existingOrder = await _context.Orders
            .Include(o => o.OrderItems)
            .FirstOrDefaultAsync(o => o.OrderId == order.OrderId);

        if (existingOrder == null)
        {
            // Insert
            await _context.Orders.AddAsync(order);
        }
        else
        {
            // Update
            _context.Entry(existingOrder).CurrentValues.SetValues(order);
            
            // Actualizează OrderItems
            // Șterge items vechi
            _context.RemoveRange(existingOrder.OrderItems);
            
            // Adaugă items noi
            foreach (var item in order.OrderItems)
            {
                await _context.Set<OrderItem>().AddAsync(item);
            }
        }

        await _context.SaveChangesAsync();
    }

    public async Task<List<Order>> GetByCustomerIdAsync(Guid customerId)
    {
        return await _context.Orders
            .Include(o => o.OrderItems)
            .Where(o => o.CustomerId == customerId)
            .OrderByDescending(o => o.CreatedDate)
            .ToListAsync();
    }

    public async Task<List<Order>> GetOrdersByStatusAsync(OrderStatus status)
    {
        return await _context.Orders
            .Include(o => o.OrderItems)
            .Where(o => o.Status == status)
            .ToListAsync();
    }

    public async Task<bool> ExistsAsync(Guid orderId)
    {
        return await _context.Orders.AnyAsync(o => o.OrderId == orderId);
    }

    public async Task<List<Order>> GetOrdersByDateRangeAsync(DateTime startDate, DateTime endDate)
    {
        return await _context.Orders
            .Include(o => o.OrderItems)
            .Where(o => o.CreatedDate >= startDate && o.CreatedDate <= endDate)
            .OrderByDescending(o => o.CreatedDate)
            .ToListAsync();
    }

    public async Task DeleteAsync(Guid orderId)
    {
        var order = await GetByIdAsync(orderId);
        if (order is not null)
        {
            _context.Orders.Remove(order);
            await _context.SaveChangesAsync();
        }
    }
}

// ═══════════════════════════════════════════════════════════════════════════════
// DB CONTEXT
// ═══════════════════════════════════════════════════════════════════════════════

public class OrderManagementDbContext : DbContext
{
    public DbSet<Order> Orders { get; set; }

    public OrderManagementDbContext(DbContextOptions<OrderManagementDbContext> options)
        : base(options)
    {
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Configurare Order Aggregate
        modelBuilder.Entity<Order>(entity =>
        {
            entity.HasKey(o => o.OrderId);

            entity.Property(o => o.OrderId)
                .IsRequired();

            entity.Property(o => o.CustomerId)
                .IsRequired();

            entity.Property(o => o.Status)
                .IsRequired()
                .HasConversion<string>(); // Salvează enum ca string

            entity.Property(o => o.PaymentMethod)
                .IsRequired()
                .HasConversion<string>();

            entity.Property(o => o.CreatedDate)
                .IsRequired();

            // Value Object: CustomerInfo - owned entity
            entity.OwnsOne(o => o.CustomerInfo, customerInfo =>
            {
                customerInfo.Property(c => c.Name).IsRequired().HasMaxLength(200);
                customerInfo.Property(c => c.Email).IsRequired().HasMaxLength(100);
                customerInfo.Property(c => c.PhoneNumber).IsRequired().HasMaxLength(20);
            });

            // Value Object: ShippingAddress - owned entity
            entity.OwnsOne(o => o.ShippingAddress, address =>
            {
                address.Property(a => a.Street).IsRequired().HasMaxLength(200);
                address.Property(a => a.City).IsRequired().HasMaxLength(100);
                address.Property(a => a.County).IsRequired().HasMaxLength(100);
                address.Property(a => a.PostalCode).IsRequired().HasMaxLength(10);
                address.Property(a => a.Country).IsRequired().HasMaxLength(100);
            });

            // Value Object: Money (TotalAmount) - owned entity
            entity.OwnsOne(o => o.TotalAmount, money =>
            {
                money.Property(m => m.Amount).IsRequired().HasColumnType("decimal(18,2)");
                money.Property(m => m.Currency).IsRequired().HasMaxLength(3);
            });

            // Value Object: CancellationReason - owned entity (nullable)
            entity.OwnsOne(o => o.CancellationReason, reason =>
            {
                reason.Property(r => r.Reason).HasMaxLength(500);
                reason.Property(r => r.RequestedBy).IsRequired();
                reason.Property(r => r.RequestedAt).IsRequired();
            });

            // OrderItems collection - owned entities
            entity.OwnsMany(o => o.OrderItems, orderItem =>
            {
                orderItem.WithOwner().HasForeignKey("OrderId");
                orderItem.Property<int>("Id").ValueGeneratedOnAdd();
                orderItem.HasKey("Id");

                orderItem.Property(oi => oi.ProductId).IsRequired();
                orderItem.Property(oi => oi.ProductName).IsRequired().HasMaxLength(200);
                orderItem.Property(oi => oi.Quantity).IsRequired();

                // Value Object: UnitPrice
                orderItem.OwnsOne(oi => oi.UnitPrice, price =>
                {
                    price.Property(p => p.Amount).IsRequired().HasColumnType("decimal(18,2)");
                    price.Property(p => p.Currency).IsRequired().HasMaxLength(3);
                });

                // Value Object: LineTotal
                orderItem.OwnsOne(oi => oi.LineTotal, lineTotal =>
                {
                    lineTotal.Property(lt => lt.Amount).IsRequired().HasColumnType("decimal(18,2)");
                    lineTotal.Property(lt => lt.Currency).IsRequired().HasMaxLength(3);
                });
            });

            // Ignore DomainEvents (nu le persistăm)
            entity.Ignore(o => o.DomainEvents);

            // Indexes pentru performanță
            entity.HasIndex(o => o.CustomerId);
            entity.HasIndex(o => o.Status);
            entity.HasIndex(o => o.CreatedDate);
        });
    }
}
