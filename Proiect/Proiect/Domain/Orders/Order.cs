// ═══════════════════════════════════════════════════════════════════════════════
// 📦 BOUNDED CONTEXT: ORDER MANAGEMENT - DOMAIN LAYER
// ═══════════════════════════════════════════════════════════════════════════════
// Implementare DDD în C# pentru agregatul Order
// Data: November 7, 2025
// ═══════════════════════════════════════════════════════════════════════════════

using OrderManagement.Domain.Orders.Events;

namespace OrderManagement.Domain.Orders;

// ═══════════════════════════════════════════════════════════════════════════════
// ENUMS & CONSTANTS
// ═══════════════════════════════════════════════════════════════════════════════

public enum OrderStatus
{
    Placed,
    Validated,
    Rejected,
    Confirmed,
    Paid,
    Shipped,
    Delivered,
    Cancelled,
    Modified
}

public enum PaymentMethod
{
    Cash,
    Card,
    BankTransfer,
    OnlinePayment
}

// ═══════════════════════════════════════════════════════════════════════════════
// VALUE OBJECTS
// ═══════════════════════════════════════════════════════════════════════════════

/// <summary>
/// Value Object pentru Money - encapsulează conceptul de bani
/// </summary>
public record Money
{
    public decimal Amount { get; init; }
    public string Currency { get; init; }

    public Money(decimal amount, string currency = "RON")
    {
        if (amount < 0)
            throw new ArgumentException("Amount cannot be negative", nameof(amount));
        
        if (string.IsNullOrWhiteSpace(currency))
            throw new ArgumentException("Currency is required", nameof(currency));

        Amount = amount;
        Currency = currency;
    }

    public Money Add(Money other)
    {
        if (Currency != other.Currency)
            throw new InvalidOperationException("Cannot add different currencies");
        
        return new Money(Amount + other.Amount, Currency);
    }

    public Money Subtract(Money other)
    {
        if (Currency != other.Currency)
            throw new InvalidOperationException("Cannot subtract different currencies");
        
        return new Money(Amount - other.Amount, Currency);
    }

    public Money Multiply(int quantity)
    {
        return new Money(Amount * quantity, Currency);
    }

    public static Money Zero(string currency = "RON") => new Money(0, currency);

    public override string ToString() => $"{Amount:N2} {Currency}";
}

/// <summary>
/// Value Object pentru ShippingAddress
/// </summary>
public record ShippingAddress
{
    public string Street { get; init; }
    public string City { get; init; }
    public string County { get; init; }
    public string PostalCode { get; init; }
    public string Country { get; init; }

    public ShippingAddress(string street, string city, string county, string postalCode, string country = "Romania")
    {
        if (string.IsNullOrWhiteSpace(street))
            throw new ArgumentException("Street is required", nameof(street));
        if (string.IsNullOrWhiteSpace(city))
            throw new ArgumentException("City is required", nameof(city));
        if (string.IsNullOrWhiteSpace(county))
            throw new ArgumentException("County is required", nameof(county));
        if (string.IsNullOrWhiteSpace(postalCode))
            throw new ArgumentException("PostalCode is required", nameof(postalCode));
        if (!IsValidPostalCode(postalCode))
            throw new ArgumentException("Invalid postal code format", nameof(postalCode));

        Street = street;
        City = city;
        County = county;
        PostalCode = postalCode;
        Country = country;
    }

    private static bool IsValidPostalCode(string postalCode)
    {
        // Romanian postal code: 6 digits
        return System.Text.RegularExpressions.Regex.IsMatch(postalCode, @"^\d{6}$");
    }

    public bool IsValid() => !string.IsNullOrWhiteSpace(Street) 
                            && !string.IsNullOrWhiteSpace(City) 
                            && !string.IsNullOrWhiteSpace(County);

    public override string ToString() => $"{Street}, {City}, {County} {PostalCode}, {Country}";
}

/// <summary>
/// Value Object pentru CustomerInfo
/// </summary>
public record CustomerInfo
{
    public string Name { get; init; }
    public string Email { get; init; }
    public string PhoneNumber { get; init; }

    public CustomerInfo(string name, string email, string phoneNumber)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Name is required", nameof(name));
        if (string.IsNullOrWhiteSpace(email) || !IsValidEmail(email))
            throw new ArgumentException("Valid email is required", nameof(email));
        if (string.IsNullOrWhiteSpace(phoneNumber) || !IsValidPhoneNumber(phoneNumber))
            throw new ArgumentException("Valid phone number is required", nameof(phoneNumber));

        Name = name;
        Email = email;
        PhoneNumber = phoneNumber;
    }

    private static bool IsValidEmail(string email)
    {
        return System.Text.RegularExpressions.Regex.IsMatch(email, 
            @"^[^@\s]+@[^@\s]+\.[^@\s]+$");
    }

    private static bool IsValidPhoneNumber(string phoneNumber)
    {
        // Romanian phone format: +40XXXXXXXXX or 07XXXXXXXX
        return System.Text.RegularExpressions.Regex.IsMatch(phoneNumber, 
            @"^(\+40|0)[7]\d{8}$");
    }

    public override string ToString() => $"{Name} ({Email}, {PhoneNumber})";
}

/// <summary>
/// Value Object pentru CancellationReason
/// </summary>
public record CancellationReason
{
    public string Reason { get; init; }
    public Guid RequestedBy { get; init; }
    public DateTime RequestedAt { get; init; }

    public CancellationReason(string reason, Guid requestedBy)
    {
        if (string.IsNullOrWhiteSpace(reason))
            throw new ArgumentException("Reason is required", nameof(reason));
        if (requestedBy == Guid.Empty)
            throw new ArgumentException("RequestedBy is required", nameof(requestedBy));

        Reason = reason;
        RequestedBy = requestedBy;
        RequestedAt = DateTime.UtcNow;
    }
}

/// <summary>
/// Value Object pentru ModificationRequest
/// </summary>
public record ModificationRequest
{
    public string Description { get; init; }
    public Guid RequestedBy { get; init; }
    public DateTime RequestedAt { get; init; }
    public Dictionary<string, object> Changes { get; init; }

    public ModificationRequest(string description, Guid requestedBy, Dictionary<string, object> changes)
    {
        if (string.IsNullOrWhiteSpace(description))
            throw new ArgumentException("Description is required", nameof(description));
        if (requestedBy == Guid.Empty)
            throw new ArgumentException("RequestedBy is required", nameof(requestedBy));

        Description = description;
        RequestedBy = requestedBy;
        RequestedAt = DateTime.UtcNow;
        Changes = changes ?? new Dictionary<string, object>();
    }
}

// ═══════════════════════════════════════════════════════════════════════════════
// ENTITIES
// ═══════════════════════════════════════════════════════════════════════════════

/// <summary>
/// Entity: OrderItem - parte din agregatul Order
/// </summary>
public class OrderItem
{
    public Guid ProductId { get; private set; }
    public string ProductName { get; private set; }
    public int Quantity { get; private set; }
    public Money UnitPrice { get; private set; }
    public Money LineTotal { get; private set; }

    // Constructor pentru EF Core
    private OrderItem() { }

    public OrderItem(Guid productId, string productName, int quantity, Money unitPrice)
    {
        if (productId == Guid.Empty)
            throw new ArgumentException("ProductId is required", nameof(productId));
        if (string.IsNullOrWhiteSpace(productName))
            throw new ArgumentException("ProductName is required", nameof(productName));
        if (quantity <= 0)
            throw new ArgumentException("Quantity must be positive", nameof(quantity));
        if (unitPrice == null || unitPrice.Amount <= 0)
            throw new ArgumentException("UnitPrice must be positive", nameof(unitPrice));

        ProductId = productId;
        ProductName = productName;
        Quantity = quantity;
        UnitPrice = unitPrice;
        LineTotal = unitPrice.Multiply(quantity);
    }

    public void UpdateQuantity(int newQuantity)
    {
        if (newQuantity <= 0)
            throw new ArgumentException("Quantity must be positive", nameof(newQuantity));

        Quantity = newQuantity;
        LineTotal = UnitPrice.Multiply(newQuantity);
    }

    public void UpdatePrice(Money newPrice)
    {
        if (newPrice == null || newPrice.Amount <= 0)
            throw new ArgumentException("Price must be positive", nameof(newPrice));

        UnitPrice = newPrice;
        LineTotal = newPrice.Multiply(Quantity);
    }
}

// ═══════════════════════════════════════════════════════════════════════════════
// AGGREGATE ROOT: ORDER
// ═══════════════════════════════════════════════════════════════════════════════

/// <summary>
/// Aggregate Root: Order
/// Responsabilitate: Gestionează ciclul de viață al unei comenzi și menține invarianții
/// </summary>
public class Order
{
    private readonly List<OrderItem> _orderItems = new();
    private readonly List<IDomainEvent> _domainEvents = new();

    // Properties
    public Guid OrderId { get; private set; }
    public Guid CustomerId { get; private set; }
    public CustomerInfo CustomerInfo { get; private set; }
    public IReadOnlyCollection<OrderItem> OrderItems => _orderItems.AsReadOnly();
    public ShippingAddress ShippingAddress { get; private set; }
    public PaymentMethod PaymentMethod { get; private set; }
    public Money TotalAmount { get; private set; }
    public OrderStatus Status { get; private set; }
    public DateTime CreatedDate { get; private set; }
    public DateTime? ModifiedDate { get; private set; }
    public DateTime? ConfirmedDate { get; private set; }
    public DateTime? EstimatedDeliveryDate { get; private set; }
    public CancellationReason CancellationReason { get; private set; }
    public string RejectionReason { get; private set; }

    public IReadOnlyCollection<IDomainEvent> DomainEvents => _domainEvents.AsReadOnly();

    // Constructor privat pentru EF Core
    private Order() { }

    // Factory Method pentru creare comandă
    public static Order Create(
        Guid customerId,
        CustomerInfo customerInfo,
        List<OrderItem> orderItems,
        ShippingAddress shippingAddress,
        PaymentMethod paymentMethod)
    {
        var order = new Order
        {
            OrderId = Guid.NewGuid(),
            CustomerId = customerId,
            CustomerInfo = customerInfo,
            ShippingAddress = shippingAddress,
            PaymentMethod = paymentMethod,
            Status = OrderStatus.Placed,
            CreatedDate = DateTime.UtcNow
        };

        // Adaugă produse
        foreach (var item in orderItems)
        {
            order._orderItems.Add(item);
        }

        // Calculează total
        order.RecalculateTotal();

        // Verifică invarianți
        order.CheckInvariants();

        // Publică eveniment
        order.AddDomainEvent(new OrderPlacedEvent(
            order.OrderId,
            order.CustomerId,
            order.CustomerInfo,
            order.OrderItems.ToList(),
            order.ShippingAddress,
            order.PaymentMethod,
            order.TotalAmount,
            order.CreatedDate
        ));

        return order;
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // BUSINESS METHODS (Commands)
    // ═══════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Validează comanda - verifică disponibilitate stoc și validitatea datelor
    /// </summary>
    public void Validate()
    {
        if (Status != OrderStatus.Placed)
            throw new InvalidOperationException($"Cannot validate order in status {Status}");

        Status = OrderStatus.Validated;
        ModifiedDate = DateTime.UtcNow;

        AddDomainEvent(new OrderValidatedEvent(
            OrderId,
            DateTime.UtcNow,
            "System"
        ));
    }

    /// <summary>
    /// Respinge comanda cu un motiv
    /// </summary>
    public void Reject(string reason)
    {
        if (Status != OrderStatus.Placed)
            throw new InvalidOperationException($"Cannot reject order in status {Status}");

        if (string.IsNullOrWhiteSpace(reason))
            throw new ArgumentException("Rejection reason is required", nameof(reason));

        Status = OrderStatus.Rejected;
        RejectionReason = reason;
        ModifiedDate = DateTime.UtcNow;

        AddDomainEvent(new OrderRejectedEvent(
            OrderId,
            reason,
            DateTime.UtcNow
        ));
    }

    /// <summary>
    /// Confirmă comanda după rezervarea stocului
    /// </summary>
    public void Confirm(Guid confirmedBy, DateTime estimatedDeliveryDate)
    {
        if (Status != OrderStatus.Validated)
            throw new InvalidOperationException($"Cannot confirm order in status {Status}");

        Status = OrderStatus.Confirmed;
        ConfirmedDate = DateTime.UtcNow;
        EstimatedDeliveryDate = estimatedDeliveryDate;
        ModifiedDate = DateTime.UtcNow;

        AddDomainEvent(new OrderConfirmedEvent(
            OrderId,
            DateTime.UtcNow,
            confirmedBy,
            estimatedDeliveryDate
        ));
    }

    /// <summary>
    /// Solicită anularea comenzii
    /// </summary>
    public void RequestCancellation(string reason, Guid requestedBy)
    {
        if (!CanBeCancelled())
            throw new InvalidOperationException($"Order in status {Status} cannot be cancelled");

        if (string.IsNullOrWhiteSpace(reason))
            throw new ArgumentException("Cancellation reason is required", nameof(reason));

        AddDomainEvent(new OrderCancellationRequestedEvent(
            OrderId,
            requestedBy,
            reason,
            DateTime.UtcNow
        ));
    }

    /// <summary>
    /// Anulează comanda efectiv
    /// </summary>
    public void Cancel(CancellationReason cancellationReason)
    {
        if (!CanBeCancelled())
            throw new InvalidOperationException($"Order in status {Status} cannot be cancelled");

        if (cancellationReason == null)
            throw new ArgumentNullException(nameof(cancellationReason));

        var previousStatus = Status;
        Status = OrderStatus.Cancelled;
        CancellationReason = cancellationReason;
        ModifiedDate = DateTime.UtcNow;

        AddDomainEvent(new OrderCancelledEvent(
            OrderId,
            DateTime.UtcNow,
            cancellationReason.RequestedBy,
            cancellationReason.Reason,
            previousStatus
        ));
    }

    /// <summary>
    /// Solicită modificarea comenzii
    /// </summary>
    public void RequestModification(ModificationRequest modificationRequest)
    {
        if (!CanBeModified())
            throw new InvalidOperationException($"Order in status {Status} cannot be modified");

        if (modificationRequest == null)
            throw new ArgumentNullException(nameof(modificationRequest));

        AddDomainEvent(new OrderModificationRequestedEvent(
            OrderId,
            modificationRequest.RequestedBy,
            modificationRequest.Changes,
            modificationRequest.RequestedAt,
            modificationRequest.Description
        ));
    }

    /// <summary>
    /// Modifică comanda efectiv
    /// </summary>
    public void Modify(
        List<OrderItem> newOrderItems = null,
        ShippingAddress newShippingAddress = null,
        PaymentMethod? newPaymentMethod = null)
    {
        if (!CanBeModified())
            throw new InvalidOperationException($"Order in status {Status} cannot be modified");

        var oldValues = new Dictionary<string, object>();
        var newValues = new Dictionary<string, object>();

        // Modifică produse
        if (newOrderItems != null && newOrderItems.Any())
        {
            oldValues["OrderItems"] = _orderItems.ToList();
            _orderItems.Clear();
            _orderItems.AddRange(newOrderItems);
            newValues["OrderItems"] = newOrderItems;
            RecalculateTotal();
        }

        // Modifică adresa
        if (newShippingAddress != null)
        {
            oldValues["ShippingAddress"] = ShippingAddress;
            ShippingAddress = newShippingAddress;
            newValues["ShippingAddress"] = newShippingAddress;
        }

        // Modifică metoda de plată
        if (newPaymentMethod.HasValue)
        {
            oldValues["PaymentMethod"] = PaymentMethod;
            PaymentMethod = newPaymentMethod.Value;
            newValues["PaymentMethod"] = newPaymentMethod.Value;
        }

        Status = OrderStatus.Modified;
        ModifiedDate = DateTime.UtcNow;

        // Verifică invarianți după modificare
        CheckInvariants();

        AddDomainEvent(new OrderModifiedEvent(
            OrderId,
            oldValues,
            newValues,
            DateTime.UtcNow
        ));

        // Revine la Placed pentru re-validare
        Status = OrderStatus.Placed;
    }

    /// <summary>
    /// Marchează comanda ca plătită
    /// </summary>
    public void MarkAsPaid(Guid paymentId, Money paidAmount)
    {
        if (Status != OrderStatus.Confirmed)
            throw new InvalidOperationException($"Cannot mark order as paid in status {Status}");

        if (paidAmount.Amount < TotalAmount.Amount)
            throw new InvalidOperationException("Paid amount is less than order total");

        Status = OrderStatus.Paid;
        ModifiedDate = DateTime.UtcNow;

        // Event-ul PaymentCompleted vine din Payment context
    }

    /// <summary>
    /// Marchează comanda ca expediată
    /// </summary>
    public void MarkAsShipped(string awbNumber)
    {
        if (Status != OrderStatus.Paid)
            throw new InvalidOperationException($"Cannot ship order in status {Status}");

        Status = OrderStatus.Shipped;
        ModifiedDate = DateTime.UtcNow;

        // Event-ul OrderShipped vine din Shipping context
    }

    /// <summary>
    /// Marchează comanda ca livrată
    /// </summary>
    public void MarkAsDelivered()
    {
        if (Status != OrderStatus.Shipped)
            throw new InvalidOperationException($"Cannot mark order as delivered in status {Status}");

        Status = OrderStatus.Delivered;
        ModifiedDate = DateTime.UtcNow;

        // Event-ul OrderDelivered vine din Shipping context
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // BUSINESS RULES (Invariants)
    // ═══════════════════════════════════════════════════════════════════════════

    private void CheckInvariants()
    {
        // I1: Comenzile trebuie să aibă cel puțin un produs
        if (!_orderItems.Any())
            throw new InvalidOperationException("Order must have at least one item");

        // I2: Totalul comenzii trebuie să fie suma tuturor liniilor
        var calculatedTotal = _orderItems.Sum(item => item.LineTotal.Amount);
        if (Math.Abs(TotalAmount.Amount - calculatedTotal) > 0.01m)
            throw new InvalidOperationException("Order total does not match sum of line items");

        // I5, I6: Verifică cantități și prețuri pozitive
        if (_orderItems.Any(item => item.Quantity <= 0))
            throw new InvalidOperationException("All item quantities must be positive");

        if (_orderItems.Any(item => item.UnitPrice.Amount <= 0))
            throw new InvalidOperationException("All item prices must be positive");

        // I8: Adresa de livrare trebuie să fie validă
        if (ShippingAddress == null || !ShippingAddress.IsValid())
            throw new InvalidOperationException("Shipping address must be valid");

        // I10: CustomerId trebuie să existe
        if (CustomerId == Guid.Empty)
            throw new InvalidOperationException("CustomerId is required");

        // Business Rule: Suma minimă pentru comandă
        const decimal minimumOrderAmount = 50m;
        if (TotalAmount.Amount < minimumOrderAmount)
            throw new InvalidOperationException($"Order total must be at least {minimumOrderAmount} {TotalAmount.Currency}");
    }

    private bool CanBeCancelled()
    {
        // I4: O comandă poate fi anulată DOAR înainte de expediere
        return Status != OrderStatus.Shipped 
            && Status != OrderStatus.Delivered 
            && Status != OrderStatus.Cancelled 
            && Status != OrderStatus.Rejected;
    }

    private bool CanBeModified()
    {
        // I3: O comandă poate fi modificată DOAR în stările Placed, Validated
        return Status == OrderStatus.Placed || Status == OrderStatus.Validated;
    }

    private void RecalculateTotal()
    {
        var total = Money.Zero();
        foreach (var item in _orderItems)
        {
            total = total.Add(item.LineTotal);
        }
        TotalAmount = total;
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // DOMAIN EVENTS MANAGEMENT
    // ═══════════════════════════════════════════════════════════════════════════

    private void AddDomainEvent(IDomainEvent domainEvent)
    {
        _domainEvents.Add(domainEvent);
    }

    public void ClearDomainEvents()
    {
        _domainEvents.Clear();
    }
}

