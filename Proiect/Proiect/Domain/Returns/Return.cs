// ═══════════════════════════════════════════════════════════════════════════════
// 📦 BOUNDED CONTEXT: RETURNS MANAGEMENT - AGGREGATE ROOT
// ═══════════════════════════════════════════════════════════════════════════════
// Implementare DDD în C# pentru agregatul Return
// Data: November 7, 2025
// ═══════════════════════════════════════════════════════════════════════════════

using ReturnsManagement.Domain.Returns.Events;

namespace ReturnsManagement.Domain.Returns;

// ═══════════════════════════════════════════════════════════════════════════════
// ENUMS & CONSTANTS
// ═══════════════════════════════════════════════════════════════════════════════

public enum ReturnStatus
{
    Requested,      // Cerere de retur inițiată
    Approved,       // Aprobat de manager
    Rejected,       // Respins
    Received,       // Primit în depozit
    Accepted,       // Acceptat final, se procesează rambursarea
    Completed,      // Rambursare finalizată
    Cancelled       // Anulat de client
}

public enum ProductCondition
{
    Intact,          // Produs intact, în ambalaj original
    Opened,          // Ambalaj deschis dar produs nefolosit
    Used,            // Produs folosit dar funcțional
    Damaged,         // Produs deteriorat
    Defective        // Produs defect
}

public enum ReturnReason
{
    DefectiveProduct,       // Produs defect
    WrongItemReceived,      // Produs greșit livrat
    NotAsDescribed,         // Nu corespunde descrierii
    ChangedMind,            // Client și-a schimbat decizia
    SizeIssue,              // Problemă de mărime
    QualityIssue,           // Problemă de calitate
    DamagedInTransit,       // Deteriorat în transport
    Other                   // Altele
}

public enum RefundMethod
{
    OriginalPaymentMethod,  // Rambursare prin metoda de plată originală
    BankTransfer,           // Transfer bancar
    StoreCredit,            // Credit în magazin
    Cash                    // Numerar
}

// ═══════════════════════════════════════════════════════════════════════════════
// VALUE OBJECTS
// ═══════════════════════════════════════════════════════════════════════════════

/// <summary>
/// Value Object pentru Money
/// INVARIANT: Amount >= 0
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
            throw new ArgumentException("Currency cannot be empty", nameof(currency));

        Amount = amount;
        Currency = currency;
    }

    public Money Add(Money other)
    {
        if (Currency != other.Currency)
            throw new InvalidOperationException($"Cannot add money with different currencies: {Currency} and {other.Currency}");
        
        return new Money(Amount + other.Amount, Currency);
    }

    public Money Subtract(Money other)
    {
        if (Currency != other.Currency)
            throw new InvalidOperationException($"Cannot subtract money with different currencies: {Currency} and {other.Currency}");
        
        if (Amount < other.Amount)
            throw new InvalidOperationException("Result would be negative");
        
        return new Money(Amount - other.Amount, Currency);
    }

    public Money ApplyPercentage(decimal percentage)
    {
        if (percentage < 0 || percentage > 100)
            throw new ArgumentException("Percentage must be between 0 and 100", nameof(percentage));
        
        return new Money(Amount * percentage / 100, Currency);
    }

    public static Money Zero(string currency = "RON") => new Money(0, currency);

    public override string ToString() => $"{Amount:N2} {Currency}";
}

/// <summary>
/// Value Object pentru Return Policy
/// Definește regulile de retur pentru un produs sau categorie
/// </summary>
public record ReturnPolicy
{
    public int ReturnPeriodDays { get; init; }
    public bool IsReturnable { get; init; }
    public decimal RestockingFeePercentage { get; init; }
    public List<ReturnReason> AllowedReasons { get; init; }
    public bool RequiresOriginalPackaging { get; init; }
    public string PolicyDescription { get; init; }

    public ReturnPolicy(
        int returnPeriodDays,
        bool isReturnable = true,
        decimal restockingFeePercentage = 0,
        List<ReturnReason>? allowedReasons = null,
        bool requiresOriginalPackaging = false,
        string policyDescription = "")
    {
        if (returnPeriodDays < 0)
            throw new ArgumentException("Return period cannot be negative", nameof(returnPeriodDays));
        
        if (restockingFeePercentage < 0 || restockingFeePercentage > 100)
            throw new ArgumentException("Restocking fee must be between 0 and 100", nameof(restockingFeePercentage));

        ReturnPeriodDays = returnPeriodDays;
        IsReturnable = isReturnable;
        RestockingFeePercentage = restockingFeePercentage;
        AllowedReasons = allowedReasons ?? Enum.GetValues<ReturnReason>().ToList();
        RequiresOriginalPackaging = requiresOriginalPackaging;
        PolicyDescription = policyDescription;
    }

    public static ReturnPolicy StandardPolicy() => new ReturnPolicy(14, true, 0);
    public static ReturnPolicy ExtendedPolicy() => new ReturnPolicy(30, true, 0);
    public static ReturnPolicy NonReturnable() => new ReturnPolicy(0, false, 0);
}

/// <summary>
/// Value Object pentru RMA (Return Merchandise Authorization) Code
/// </summary>
public record RmaCode
{
    public string Value { get; init; }

    public RmaCode(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new ArgumentException("RMA code cannot be empty", nameof(value));
        
        Value = value;
    }

    public static RmaCode Generate(Guid returnId)
    {
        // Format: RMA-YYYYMMDD-{first 8 chars of GUID}
        var datePart = DateTime.UtcNow.ToString("yyyyMMdd");
        var guidPart = returnId.ToString().Substring(0, 8).ToUpper();
        return new RmaCode($"RMA-{datePart}-{guidPart}");
    }

    public override string ToString() => Value;
}

/// <summary>
/// Value Object pentru Return Window
/// Calculează perioada validă de retur
/// </summary>
public record ReturnWindow
{
    public DateTime OrderDeliveryDate { get; init; }
    public int ReturnPeriodDays { get; init; }
    public DateTime ExpirationDate { get; init; }
    public bool IsExpired => DateTime.UtcNow > ExpirationDate;
    public int DaysRemaining => Math.Max(0, (ExpirationDate - DateTime.UtcNow).Days);

    public ReturnWindow(DateTime orderDeliveryDate, int returnPeriodDays)
    {
        if (returnPeriodDays < 0)
            throw new ArgumentException("Return period cannot be negative", nameof(returnPeriodDays));

        OrderDeliveryDate = orderDeliveryDate;
        ReturnPeriodDays = returnPeriodDays;
        ExpirationDate = orderDeliveryDate.AddDays(returnPeriodDays);
    }

    public void EnsureNotExpired()
    {
        if (IsExpired)
            throw new InvalidOperationException(
                $"Return window has expired. Last day for return was {ExpirationDate:yyyy-MM-dd}");
    }
}

// ═══════════════════════════════════════════════════════════════════════════════
// ENTITIES
// ═══════════════════════════════════════════════════════════════════════════════

/// <summary>
/// Entity: ReturnItem
/// Reprezintă un produs individual din returul
/// </summary>
public class ReturnItem
{
    public Guid ReturnItemId { get; private set; }
    public Guid ProductId { get; private set; }
    public string ProductName { get; private set; }
    public int QuantityRequested { get; private set; }
    public int QuantityReceived { get; private set; }
    public Money UnitPrice { get; private set; }
    public Money TotalPrice => new Money(UnitPrice.Amount * QuantityRequested, UnitPrice.Currency);
    public ProductCondition? ReceivedCondition { get; private set; }
    public string ConditionNotes { get; private set; }
    public bool AcceptableForResale { get; private set; }

    private ReturnItem() { } // For EF Core

    public ReturnItem(
        Guid productId,
        string productName,
        int quantityRequested,
        Money unitPrice)
    {
        if (quantityRequested <= 0)
            throw new ArgumentException("Quantity must be positive", nameof(quantityRequested));
        
        if (string.IsNullOrWhiteSpace(productName))
            throw new ArgumentException("Product name cannot be empty", nameof(productName));

        ReturnItemId = Guid.NewGuid();
        ProductId = productId;
        ProductName = productName;
        QuantityRequested = quantityRequested;
        QuantityReceived = 0;
        UnitPrice = unitPrice;
        ConditionNotes = string.Empty;
        AcceptableForResale = false;
    }

    /// <summary>
    /// Marchează itemul ca fiind primit cu o anumită stare
    /// </summary>
    public void MarkAsReceived(int quantityReceived, ProductCondition condition, string notes, bool acceptableForResale)
    {
        if (quantityReceived < 0)
            throw new ArgumentException("Quantity received cannot be negative", nameof(quantityReceived));
        
        if (quantityReceived > QuantityRequested)
            throw new InvalidOperationException(
                $"Cannot receive more items ({quantityReceived}) than requested ({QuantityRequested})");

        QuantityReceived = quantityReceived;
        ReceivedCondition = condition;
        ConditionNotes = notes ?? string.Empty;
        AcceptableForResale = acceptableForResale;
    }

    public bool IsFullyReceived() => QuantityReceived == QuantityRequested;
}

// ═══════════════════════════════════════════════════════════════════════════════
// AGGREGATE ROOT: Return
// ═══════════════════════════════════════════════════════════════════════════════

/// <summary>
/// Aggregate Root: Return
/// Gestionează ciclul complet de viață al unui retur
/// 
/// INVARIANȚI:
/// 1. Status poate progresa doar în ordine: Requested → Approved → Received → Accepted
/// 2. Cantitatea returnată trebuie să fie pozitivă și <= cantitatea comandată
/// 3. Returul trebuie făcut în perioada permisă
/// 4. Valoarea totală = Σ(ReturnItem.Quantity × ReturnItem.UnitPrice)
/// 5. RefundAmount <= TotalAmount <= OriginalOrderAmount
/// </summary>
public class Return
{
    // Properties
    public Guid ReturnId { get; private set; }
    public Guid OrderId { get; private set; }
    public Guid CustomerId { get; private set; }
    public string CustomerName { get; private set; }
    public string CustomerEmail { get; private set; }
    public RmaCode RmaCode { get; private set; }
    public ReturnStatus Status { get; private set; }
    public ReturnReason Reason { get; private set; }
    public string DetailedDescription { get; private set; }
    
    private readonly List<ReturnItem> _items = new();
    public IReadOnlyList<ReturnItem> Items => _items.AsReadOnly();
    
    public ReturnPolicy Policy { get; private set; }
    public ReturnWindow ReturnWindow { get; private set; }
    
    public Money TotalAmount { get; private set; }
    public Money RestockingFee { get; private set; }
    public Money RefundAmount { get; private set; }
    public RefundMethod RefundMethod { get; private set; }
    
    public DateTime RequestedAt { get; private set; }
    public DateTime? ApprovedAt { get; private set; }
    public DateTime? ReceivedAt { get; private set; }
    public DateTime? AcceptedAt { get; private set; }
    
    public Guid? ApprovedBy { get; private set; }
    public Guid? ReceivedBy { get; private set; }
    public Guid? AcceptedBy { get; private set; }
    
    public string ApprovalNotes { get; private set; }
    public string InspectionNotes { get; private set; }
    public string TrackingNumber { get; private set; }

    // Domain Events
    private readonly List<IDomainEvent> _domainEvents = new();
    public IReadOnlyList<IDomainEvent> DomainEvents => _domainEvents.AsReadOnly();

    private Return() { } // For EF Core

    // ═══════════════════════════════════════════════════════════════════════════
    // FACTORY METHOD: RequestReturn
    // Comandă: RequestReturn → Eveniment: ReturnRequested
    // ═══════════════════════════════════════════════════════════════════════════
    
    /// <summary>
    /// Factory method pentru crearea unui retur nou
    /// Validări:
    /// - Perioada de retur nu a expirat
    /// - Produsele sunt returnabile
    /// - Cantitățile sunt valide
    /// - Motivul este acceptabil
    /// </summary>
    public static Return RequestReturn(
        Guid orderId,
        Guid customerId,
        string customerName,
        string customerEmail,
        DateTime orderDeliveryDate,
        List<(Guid ProductId, string ProductName, int Quantity, decimal UnitPrice)> items,
        ReturnReason reason,
        string detailedDescription,
        ReturnPolicy policy)
    {
        // Validare: Verifică perioada de retur
        var returnWindow = new ReturnWindow(orderDeliveryDate, policy.ReturnPeriodDays);
        returnWindow.EnsureNotExpired();

        // Validare: Verifică dacă produsele sunt returnabile
        if (!policy.IsReturnable)
            throw new InvalidOperationException("Products in this order are not returnable according to the policy");

        // Validare: Verifică dacă motivul este permis
        if (!policy.AllowedReasons.Contains(reason))
            throw new InvalidOperationException($"Return reason '{reason}' is not allowed by the policy");

        // Validare: Verifică că există produse
        if (items == null || items.Count == 0)
            throw new ArgumentException("Return must contain at least one item", nameof(items));

        // Creare instanță
        var returnId = Guid.NewGuid();
        var rmaCode = RmaCode.Generate(returnId);

        var @return = new Return
        {
            ReturnId = returnId,
            OrderId = orderId,
            CustomerId = customerId,
            CustomerName = customerName ?? throw new ArgumentNullException(nameof(customerName)),
            CustomerEmail = customerEmail ?? throw new ArgumentNullException(nameof(customerEmail)),
            RmaCode = rmaCode,
            Status = ReturnStatus.Requested,
            Reason = reason,
            DetailedDescription = detailedDescription ?? string.Empty,
            Policy = policy,
            ReturnWindow = returnWindow,
            RequestedAt = DateTime.UtcNow,
            RestockingFee = Money.Zero(),
            RefundAmount = Money.Zero(),
            RefundMethod = RefundMethod.OriginalPaymentMethod,
            ApprovalNotes = string.Empty,
            InspectionNotes = string.Empty,
            TrackingNumber = string.Empty
        };

        // Adaugă items
        foreach (var (productId, productName, quantity, unitPrice) in items)
        {
            var item = new ReturnItem(productId, productName, quantity, new Money(unitPrice));
            @return._items.Add(item);
        }

        // Calculează total
        @return.CalculateTotalAmount();

        // Generează eveniment de domeniu
        var returnedItems = @return.Items.Select(i => new ReturnItemRequested(
            i.ProductId,
            i.ProductName,
            i.QuantityRequested,
            i.UnitPrice.Amount,
            i.TotalPrice.Amount
        )).ToList();

        @return.AddDomainEvent(new ReturnRequested(
            returnId,
            orderId,
            customerId,
            customerName,
            customerEmail,
            returnedItems,
            reason.ToString(),
            detailedDescription,
            @return.TotalAmount.Amount,
            rmaCode.Value
        ));

        return @return;
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // COMMAND: ApproveReturn → Event: ReturnApproved
    // ═══════════════════════════════════════════════════════════════════════════
    
    /// <summary>
    /// Aprobă returul
    /// Validări:
    /// - Returul trebuie să fie în status Requested
    /// - Utilizatorul are permisiuni (verificat în handler)
    /// </summary>
    public void Approve(Guid approvedBy, string approverName, string notes, bool applyRestockingFee)
    {
        // Validare status
        if (Status != ReturnStatus.Requested)
            throw new InvalidOperationException($"Cannot approve return in status {Status}. Expected: Requested");

        // Aplică taxa de restocking dacă este cazul
        if (applyRestockingFee && Policy.RestockingFeePercentage > 0)
        {
            RestockingFee = TotalAmount.ApplyPercentage(Policy.RestockingFeePercentage);
        }

        // Calculează suma de rambursat
        RefundAmount = TotalAmount.Subtract(RestockingFee);

        // Actualizează status
        Status = ReturnStatus.Approved;
        ApprovedAt = DateTime.UtcNow;
        ApprovedBy = approvedBy;
        ApprovalNotes = notes ?? string.Empty;

        // Generează eveniment
        AddDomainEvent(new ReturnApproved(
            ReturnId,
            OrderId,
            approvedBy,
            approverName,
            notes,
            RmaCode.Value,
            RefundAmount.Amount,
            applyRestockingFee,
            Policy.RestockingFeePercentage,
            Items.Select(i => i.ProductId).ToList()
        ));
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // COMMAND: ReceiveReturn → Event: ReturnReceived
    // ═══════════════════════════════════════════════════════════════════════════
    
    /// <summary>
    /// Marchează returul ca fiind primit în depozit
    /// Validări:
    /// - Returul trebuie să fie în status Approved
    /// - Cantitățile primite trebuie să fie valide
    /// </summary>
    public void ReceiveProducts(
        Guid receivedBy,
        string receiverName,
        List<(Guid ProductId, int Quantity, ProductCondition Condition, string Notes, bool AcceptableForResale)> receivedItems,
        string trackingNumber,
        string warehouseLocation,
        string inspectionNotes)
    {
        // Validare status
        if (Status != ReturnStatus.Approved)
            throw new InvalidOperationException($"Cannot receive return in status {Status}. Expected: Approved");

        // Marchează fiecare item ca fiind primit
        foreach (var (productId, quantity, condition, notes, acceptableForResale) in receivedItems)
        {
            var item = _items.FirstOrDefault(i => i.ProductId == productId);
            if (item == null)
                throw new InvalidOperationException($"Product {productId} is not part of this return");

            item.MarkAsReceived(quantity, condition, notes, acceptableForResale);
        }

        // Actualizează status
        Status = ReturnStatus.Received;
        ReceivedAt = DateTime.UtcNow;
        ReceivedBy = receivedBy;
        TrackingNumber = trackingNumber ?? string.Empty;
        InspectionNotes = inspectionNotes ?? string.Empty;

        // Verifică dacă toate produsele au fost primite
        bool allItemsReceived = _items.All(i => i.IsFullyReceived());

        // Generează eveniment
        var receivedItemsEvent = _items.Select(i => new ReturnItemReceived(
            i.ProductId,
            i.ProductName,
            i.QuantityReceived,
            i.ReceivedCondition ?? ProductCondition.Intact,
            i.ConditionNotes,
            i.AcceptableForResale
        )).ToList();

        AddDomainEvent(new ReturnReceived(
            ReturnId,
            OrderId,
            RmaCode.Value,
            receivedBy,
            receiverName,
            receivedItemsEvent,
            trackingNumber,
            warehouseLocation,
            inspectionNotes,
            allItemsReceived
        ));
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // COMMAND: AcceptReturn → Event: ReturnAccepted
    // ═══════════════════════════════════════════════════════════════════════════
    
    /// <summary>
    /// Acceptă returul final și procesează rambursarea
    /// Validări:
    /// - Returul trebuie să fie în status Received
    /// - Toate produsele au fost inspectate
    /// </summary>
    public void AcceptAndProcessRefund(
        Guid acceptedBy,
        string accepterName,
        RefundMethod refundMethod,
        string refundReference,
        string notes,
        bool inventoryUpdated)
    {
        // Validare status
        if (Status != ReturnStatus.Received)
            throw new InvalidOperationException($"Cannot accept return in status {Status}. Expected: Received");

        // Validare: toate produsele au fost inspectate
        if (_items.Any(i => i.ReceivedCondition == null))
            throw new InvalidOperationException("All items must be inspected before accepting the return");

        // Actualizează status
        Status = ReturnStatus.Accepted;
        AcceptedAt = DateTime.UtcNow;
        AcceptedBy = acceptedBy;
        RefundMethod = refundMethod;

        // Generează eveniment
        AddDomainEvent(new ReturnAccepted(
            ReturnId,
            OrderId,
            CustomerId,
            RmaCode.Value,
            acceptedBy,
            accepterName,
            TotalAmount.Amount,
            RestockingFee.Amount,
            refundMethod.ToString(),
            refundReference,
            Items.Select(i => i.ProductId).ToList(),
            notes,
            inventoryUpdated
        ));
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // COMMAND: RejectReturn → Event: ReturnRejected (BONUS)
    // ═══════════════════════════════════════════════════════════════════════════
    
    /// <summary>
    /// Respinge returul
    /// </summary>
    public void Reject(Guid rejectedBy, string rejectorName, string rejectionReason, string detailedExplanation)
    {
        // Validare: poate fi respins doar dacă e Requested sau Received
        if (Status != ReturnStatus.Requested && Status != ReturnStatus.Received)
            throw new InvalidOperationException($"Cannot reject return in status {Status}");

        if (string.IsNullOrWhiteSpace(rejectionReason))
            throw new ArgumentException("Rejection reason must be provided", nameof(rejectionReason));

        Status = ReturnStatus.Rejected;

        AddDomainEvent(new ReturnRejected(
            ReturnId,
            OrderId,
            CustomerId,
            RmaCode.Value,
            rejectedBy,
            rejectorName,
            rejectionReason,
            detailedExplanation,
            true
        ));
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // HELPER METHODS
    // ═══════════════════════════════════════════════════════════════════════════

    private void CalculateTotalAmount()
    {
        if (_items.Count == 0)
        {
            TotalAmount = Money.Zero();
            return;
        }

        var total = Money.Zero(_items[0].UnitPrice.Currency);
        foreach (var item in _items)
        {
            total = total.Add(item.TotalPrice);
        }
        TotalAmount = total;
    }

    private void AddDomainEvent(IDomainEvent domainEvent)
    {
        _domainEvents.Add(domainEvent);
    }

    public void ClearDomainEvents()
    {
        _domainEvents.Clear();
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // INVARIANT VALIDATION
    // ═══════════════════════════════════════════════════════════════════════════
    
    /// <summary>
    /// Validează că toate invarianții sunt respectați
    /// </summary>
    public void ValidateInvariants()
    {
        // Invariant 1: Status progression
        // (verificat în fiecare metodă de tranziție)

        // Invariant 2: Cantități pozitive
        foreach (var item in _items)
        {
            if (item.QuantityRequested <= 0)
                throw new InvalidOperationException("All item quantities must be positive");
            
            if (item.QuantityReceived > item.QuantityRequested)
                throw new InvalidOperationException("Received quantity cannot exceed requested quantity");
        }

        // Invariant 3: Perioada de retur (verificată la crearea returului)
        
        // Invariant 4: Valoare totală
        var calculatedTotal = Money.Zero(TotalAmount.Currency);
        foreach (var item in _items)
        {
            calculatedTotal = calculatedTotal.Add(item.TotalPrice);
        }
        
        if (calculatedTotal.Amount != TotalAmount.Amount)
            throw new InvalidOperationException("Total amount does not match sum of item totals");

        // Invariant 5: Refund amount
        if (RefundAmount.Amount > TotalAmount.Amount)
            throw new InvalidOperationException("Refund amount cannot exceed total amount");
    }
}

