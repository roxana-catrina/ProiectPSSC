// ═══════════════════════════════════════════════════════════════════════════════
// 🔧 DOMAIN SERVICES - ORDER MANAGEMENT CONTEXT
// ═══════════════════════════════════════════════════════════════════════════════

namespace OrderManagement.Domain.Orders.Services;

using OrderManagement.Domain.Orders;

// ═══════════════════════════════════════════════════════════════════════════════
// 1️⃣ ORDER VALIDATION SERVICE
// ═══════════════════════════════════════════════════════════════════════════════

/// <summary>
/// Domain Service pentru validări complexe care implică multiple agregate sau servicii externe
/// </summary>
public interface IOrderValidationService
{
    Task<OrderValidationResult> ValidateOrderAsync(Order order);
    Task<bool> ValidateProductAvailabilityAsync(List<OrderItem> orderItems);
    Task<bool> ValidateShippingAddressAsync(ShippingAddress address);
    Task<bool> ValidateCustomerAsync(Guid customerId);
}

public record OrderValidationResult(
    bool IsValid,
    string? RejectionReason,
    List<string> Errors
);

public class OrderValidationService : IOrderValidationService
{
    private readonly IInventoryService _inventoryService; // Anti-Corruption Layer pentru INVENTORY context
    private readonly IShippingService _shippingService;   // Anti-Corruption Layer pentru SHIPPING context
    private readonly ICustomerService _customerService;   // Anti-Corruption Layer pentru CUSTOMER context

    public OrderValidationService(
        IInventoryService inventoryService,
        IShippingService shippingService,
        ICustomerService customerService)
    {
        _inventoryService = inventoryService;
        _shippingService = shippingService;
        _customerService = customerService;
    }

    public async Task<OrderValidationResult> ValidateOrderAsync(Order order)
    {
        var errors = new List<string>();

        // 1. Validare disponibilitate produse în stoc
        var productsAvailable = await ValidateProductAvailabilityAsync(order.OrderItems.ToList());
        if (!productsAvailable)
        {
            errors.Add("One or more products are not available in sufficient quantity");
        }

        // 2. Validare adresă de livrare (zonă acoperită)
        var addressValid = await ValidateShippingAddressAsync(order.ShippingAddress);
        if (!addressValid)
        {
            errors.Add("Shipping address is not in covered delivery zone");
        }

        // 3. Validare client (nu este blocat, nu are comenzi frauduloase)
        var customerValid = await ValidateCustomerAsync(order.CustomerId);
        if (!customerValid)
        {
            errors.Add("Customer is not eligible to place orders");
        }

        // 4. Validare prețuri (verifică dacă prețurile nu s-au modificat între timp)
        var pricesValid = await ValidatePricingAsync(order.OrderItems.ToList());
        if (!pricesValid)
        {
            errors.Add("Product prices have changed. Please review your order");
        }

        if (errors.Any())
        {
            return new OrderValidationResult(
                IsValid: false,
                RejectionReason: string.Join("; ", errors),
                Errors: errors
            );
        }

        return new OrderValidationResult(
            IsValid: true,
            RejectionReason: null,
            Errors: new List<string>()
        );
    }

    public async Task<bool> ValidateProductAvailabilityAsync(List<OrderItem> orderItems)
    {
        // Verifică cu INVENTORY context dacă toate produsele sunt disponibile
        foreach (var item in orderItems)
        {
            var availableQuantity = await _inventoryService.GetAvailableQuantityAsync(item.ProductId);
            if (availableQuantity < item.Quantity)
            {
                return false;
            }
        }

        return true;
    }

    public async Task<bool> ValidateShippingAddressAsync(ShippingAddress address)
    {
        // Verifică cu SHIPPING context dacă adresa este în zona acoperită
        return await _shippingService.IsCoveredAreaAsync(
            address.City,
            address.County,
            address.PostalCode
        );
    }

    public async Task<bool> ValidateCustomerAsync(Guid customerId)
    {
        // Verifică dacă clientul există și nu este blocat
        var customer = await _customerService.GetCustomerAsync(customerId);
        return customer is not null && !customer.IsBlocked;
    }

    private async Task<bool> ValidatePricingAsync(List<OrderItem> orderItems)
    {
        // Verifică dacă prețurile din comandă corespund cu prețurile curente
        foreach (var item in orderItems)
        {
            var currentPrice = await _inventoryService.GetCurrentPriceAsync(item.ProductId);
            if (Math.Abs(currentPrice - item.UnitPrice.Amount) > 0.01m)
            {
                return false;
            }
        }

        return true;
    }
}

// ═══════════════════════════════════════════════════════════════════════════════
// 2️⃣ ORDER PRICING SERVICE
// ═══════════════════════════════════════════════════════════════════════════════

/// <summary>
/// Domain Service pentru calcularea prețurilor și aplicarea reducerilor
/// </summary>
public interface IOrderPricingService
{
    Money CalculateOrderTotal(List<OrderItem> orderItems);
    Money ApplyDiscounts(Order order, Guid customerId);
    Task<Money> CalculateShippingCostAsync(Order order);
}

public class OrderPricingService : IOrderPricingService
{
    private readonly IShippingService _shippingService;

    public OrderPricingService(IShippingService shippingService)
    {
        _shippingService = shippingService;
    }

    public Money CalculateOrderTotal(List<OrderItem> orderItems)
    {
        var total = Money.Zero();
        foreach (var item in orderItems)
        {
            total = total.Add(item.LineTotal);
        }
        return total;
    }

    public Money ApplyDiscounts(Order order, Guid customerId)
    {
        var total = order.TotalAmount;

        // Exemplu: Reducere pentru clienți fideli
        // În practică, această logică ar putea fi mai complexă
        // var customer = await _customerService.GetCustomerAsync(customerId);
        // if (customer.IsVip)
        // {
        //     var discount = total.Multiply(0.1m); // 10% reducere
        //     total = total.Subtract(discount);
        // }

        return total;
    }

    public async Task<Money> CalculateShippingCostAsync(Order order)
    {
        // Calculează costul de livrare bazat pe greutate, destinație, etc.
        var shippingCost = await _shippingService.CalculateShippingCostAsync(
            order.ShippingAddress,
            order.TotalAmount
        );

        return new Money(shippingCost);
    }
}

// ═══════════════════════════════════════════════════════════════════════════════
// 3️⃣ ORDER CANCELLATION SERVICE
// ═══════════════════════════════════════════════════════════════════════════════

/// <summary>
/// Domain Service pentru logica complexă de anulare
/// </summary>
public interface IOrderCancellationService
{
    bool CanBeCancelled(Order order);
    Money CalculateCancellationFee(Order order);
    Task ProcessCancellationAsync(Order order);
}

public class OrderCancellationService : IOrderCancellationService
{
    private readonly IPaymentService _paymentService;
    private readonly IInventoryService _inventoryService;

    public OrderCancellationService(
        IPaymentService paymentService,
        IInventoryService inventoryService)
    {
        _paymentService = paymentService;
        _inventoryService = inventoryService;
    }

    public bool CanBeCancelled(Order order)
    {
        // O comandă poate fi anulată doar înainte de expediere
        return order.Status != OrderStatus.Shipped
            && order.Status != OrderStatus.Delivered
            && order.Status != OrderStatus.Cancelled
            && order.Status != OrderStatus.Rejected;
    }

    public Money CalculateCancellationFee(Order order)
    {
        // Calculează penalizarea în funcție de stadiul comenzii
        return order.Status switch
        {
            OrderStatus.Placed => Money.Zero(),
            OrderStatus.Validated => Money.Zero(),
            OrderStatus.Confirmed => Money.Zero(),
            OrderStatus.Paid => new Money(order.TotalAmount.Amount * 0.05m), // 5% penalizare
            _ => Money.Zero()
        };
    }

    public async Task ProcessCancellationAsync(Order order)
    {
        // 1. Eliberează stocul rezervat
        await _inventoryService.ReleaseStockAsync(
            order.OrderId,
            order.OrderItems.Select(i => (i.ProductId, i.Quantity)).ToList()
        );

        // 2. Inițiază rambursarea dacă s-a plătit
        if (order.Status == OrderStatus.Paid)
        {
            var cancellationFee = CalculateCancellationFee(order);
            var refundAmount = order.TotalAmount.Subtract(cancellationFee);

            await _paymentService.InitiateRefundAsync(
                order.OrderId,
                refundAmount.Amount,
                "Order cancellation"
            );
        }
    }
}

// ═══════════════════════════════════════════════════════════════════════════════
// ANTI-CORRUPTION LAYER INTERFACES (pentru comunicare cu alte contexte)
// ═══════════════════════════════════════════════════════════════════════════════

/// <summary>
/// Interface pentru comunicare cu INVENTORY context
/// </summary>
public interface IInventoryService
{
    Task<int> GetAvailableQuantityAsync(Guid productId);
    Task<decimal> GetCurrentPriceAsync(Guid productId);
    Task ReleaseStockAsync(Guid orderId, List<(Guid ProductId, int Quantity)> items);
}

/// <summary>
/// Interface pentru comunicare cu SHIPPING context
/// </summary>
public interface IShippingService
{
    Task<bool> IsCoveredAreaAsync(string city, string county, string postalCode);
    Task<decimal> CalculateShippingCostAsync(ShippingAddress address, Money orderTotal);
}

/// <summary>
/// Interface pentru comunicare cu CUSTOMER context (dacă există)
/// </summary>
public interface ICustomerService
{
    Task<CustomerDto?> GetCustomerAsync(Guid customerId);
}

public record CustomerDto(
    Guid CustomerId,
    string Name,
    bool IsBlocked,
    bool IsVip
);

/// <summary>
/// Interface pentru comunicare cu PAYMENT context
/// </summary>
public interface IPaymentService
{
    Task InitiateRefundAsync(Guid orderId, decimal amount, string reason);
}
