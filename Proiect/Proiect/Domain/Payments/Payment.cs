// ═══════════════════════════════════════════════════════════════════════════════
// 💳 PAYMENT AGGREGATE ROOT - DOMAIN MODEL
// ═══════════════════════════════════════════════════════════════════════════════

namespace Proiect.Domain.Payments;

// ═══════════════════════════════════════════════════════════════════════════════
// VALUE OBJECTS
// ═══════════════════════════════════════════════════════════════════════════════

/// <summary>
/// Value Object pentru sume monetare
/// Immutable, validare în constructor
/// </summary>
public record Money
{
    public decimal Amount { get; init; }
    public string Currency { get; init; }

    public Money(decimal amount, string currency)
    {
        if (amount < 0)
            throw new ArgumentException("Amount cannot be negative", nameof(amount));
        
        if (string.IsNullOrWhiteSpace(currency))
            throw new ArgumentException("Currency cannot be empty", nameof(currency));
        
        var validCurrencies = new[] { "EUR", "USD", "RON", "GBP" };
        if (!validCurrencies.Contains(currency.ToUpper()))
            throw new ArgumentException($"Invalid currency. Must be one of: {string.Join(", ", validCurrencies)}", nameof(currency));

        Amount = amount;
        Currency = currency.ToUpper();
    }

    public static Money operator +(Money a, Money b)
    {
        if (a.Currency != b.Currency)
            throw new InvalidOperationException("Cannot add money with different currencies");
        
        return new Money(a.Amount + b.Amount, a.Currency);
    }

    public static Money operator -(Money a, Money b)
    {
        if (a.Currency != b.Currency)
            throw new InvalidOperationException("Cannot subtract money with different currencies");
        
        return new Money(a.Amount - b.Amount, a.Currency);
    }

    public static bool operator >(Money a, Money b)
    {
        if (a.Currency != b.Currency)
            throw new InvalidOperationException("Cannot compare money with different currencies");
        
        return a.Amount > b.Amount;
    }

    public static bool operator <(Money a, Money b)
    {
        if (a.Currency != b.Currency)
            throw new InvalidOperationException("Cannot compare money with different currencies");
        
        return a.Amount < b.Amount;
    }

    public static bool operator >=(Money a, Money b) => a > b || a.Amount == b.Amount;
    public static bool operator <=(Money a, Money b) => a < b || a.Amount == b.Amount;
}

/// <summary>
/// Value Object pentru detalii de plată (card)
/// </summary>
public record PaymentDetails
{
    public string MaskedCardNumber { get; init; }
    public string CardHolderName { get; init; }
    public string ExpiryDate { get; init; }

    public PaymentDetails(string maskedCardNumber, string cardHolderName, string expiryDate)
    {
        if (string.IsNullOrWhiteSpace(maskedCardNumber))
            throw new ArgumentException("Card number cannot be empty", nameof(maskedCardNumber));
        
        if (string.IsNullOrWhiteSpace(cardHolderName))
            throw new ArgumentException("Card holder name cannot be empty", nameof(cardHolderName));

        MaskedCardNumber = maskedCardNumber;
        CardHolderName = cardHolderName;
        ExpiryDate = expiryDate;
    }
}

/// <summary>
/// Value Object pentru informații de tranzacție de la gateway
/// </summary>
public record TransactionInfo
{
    public string TransactionId { get; init; }
    public string AuthorizationCode { get; init; }
    public DateTime ProcessedAt { get; init; }
    public string GatewayResponse { get; init; }

    public TransactionInfo(string transactionId, string authorizationCode, DateTime processedAt, string gatewayResponse)
    {
        if (string.IsNullOrWhiteSpace(transactionId))
            throw new ArgumentException("Transaction ID cannot be empty", nameof(transactionId));

        TransactionId = transactionId;
        AuthorizationCode = authorizationCode;
        ProcessedAt = processedAt;
        GatewayResponse = gatewayResponse;
    }
}

/// <summary>
/// Value Object pentru motivul rambursării
/// </summary>
public record RefundReason
{
    public string Reason { get; init; }
    public RefundReasonCategory Category { get; init; }
    public string RequestedBy { get; init; }
    public DateTime RequestedAt { get; init; }

    public RefundReason(string reason, RefundReasonCategory category, string requestedBy, DateTime requestedAt)
    {
        if (string.IsNullOrWhiteSpace(reason))
            throw new ArgumentException("Reason cannot be empty", nameof(reason));
        
        if (string.IsNullOrWhiteSpace(requestedBy))
            throw new ArgumentException("RequestedBy cannot be empty", nameof(requestedBy));

        Reason = reason;
        Category = category;
        RequestedBy = requestedBy;
        RequestedAt = requestedAt;
    }
}

// ═══════════════════════════════════════════════════════════════════════════════
// ENUMS
// ═══════════════════════════════════════════════════════════════════════════════

public enum PaymentStatus
{
    Pending,
    Processing,
    Completed,
    Failed,
    Cancelled
}

public enum PaymentMethod
{
    CreditCard,
    DebitCard,
    PayPal,
    BankTransfer,
    Cash
}

public enum RefundStatus
{
    Initiated,
    Processing,
    Completed,
    Failed,
    Cancelled
}

public enum RefundReasonCategory
{
    CustomerRequest,
    Fraud,
    Error,
    OrderCancellation,
    Duplicate
}

// ═══════════════════════════════════════════════════════════════════════════════
// PAYMENT AGGREGATE ROOT
// ═══════════════════════════════════════════════════════════════════════════════

/// <summary>
/// Payment Aggregate Root
/// Gestionează procesarea unei plăți pentru o comandă
/// </summary>
public class Payment
{
    // Aggregate ID
    public Guid PaymentId { get; private set; }
    
    // Foreign Key către Order Aggregate
    public Guid OrderId { get; private set; }
    
    // Properties
    public Money Amount { get; private set; }
    public PaymentStatus Status { get; private set; }
    public PaymentMethod PaymentMethod { get; private set; }
    public PaymentDetails? PaymentDetails { get; private set; }
    public TransactionInfo? TransactionInfo { get; private set; }
    
    public DateTime CreatedDate { get; private set; }
    public DateTime? ProcessedDate { get; private set; }
    public int RetryCount { get; private set; }
    public string? FailureReason { get; private set; }
    
    // Domain Events
    private List<object> _domainEvents = new();
    public IReadOnlyCollection<object> DomainEvents => _domainEvents.AsReadOnly();

    // Private constructor for EF Core
    private Payment() { }

    // ═══════════════════════════════════════════════════════════════════════════════
    // FACTORY METHOD - Create New Payment
    // ═══════════════════════════════════════════════════════════════════════════════
    
    public static Payment Create(
        Guid orderId,
        Money amount,
        PaymentMethod paymentMethod,
        PaymentDetails? paymentDetails = null)
    {
        // Validări
        if (orderId == Guid.Empty)
            throw new ArgumentException("OrderId cannot be empty", nameof(orderId));
        
        if (amount.Amount <= 0)
            throw new ArgumentException("Payment amount must be greater than zero", nameof(amount));
        
        // Validare pentru metode de plată cu card
        if ((paymentMethod == PaymentMethod.CreditCard || paymentMethod == PaymentMethod.DebitCard) 
            && paymentDetails == null)
        {
            throw new ArgumentException("Payment details required for card payments", nameof(paymentDetails));
        }

        var payment = new Payment
        {
            PaymentId = Guid.NewGuid(),
            OrderId = orderId,
            Amount = amount,
            Status = PaymentStatus.Pending,
            PaymentMethod = paymentMethod,
            PaymentDetails = paymentDetails,
            CreatedDate = DateTime.UtcNow,
            RetryCount = 0
        };

        payment.AddDomainEvent(new PaymentCreated(payment.PaymentId, payment.OrderId, payment.Amount));
        
        return payment;
    }

    // ═══════════════════════════════════════════════════════════════════════════════
    // BUSINESS METHODS
    // ═══════════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Marchează payment-ul ca fiind în curs de procesare
    /// </summary>
    public void StartProcessing()
    {
        // Invariant: Poate trece în Processing doar din Pending
        if (Status != PaymentStatus.Pending)
            throw new InvalidOperationException($"Cannot start processing payment in status {Status}");

        Status = PaymentStatus.Processing;
        AddDomainEvent(new PaymentProcessingStarted(PaymentId, OrderId));
    }

    /// <summary>
    /// Completează payment-ul cu succes
    /// EVENIMENT: PaymentCompleted
    /// </summary>
    public void Complete(TransactionInfo transactionInfo)
    {
        // Validări
        if (transactionInfo == null)
            throw new ArgumentNullException(nameof(transactionInfo));

        // Invariant: Poate fi completat doar din Processing
        if (Status != PaymentStatus.Processing)
            throw new InvalidOperationException($"Cannot complete payment in status {Status}");
        
        // Invariant: Nu poate fi completat de două ori
        if (TransactionInfo != null)
            throw new InvalidOperationException("Payment already completed");

        Status = PaymentStatus.Completed;
        TransactionInfo = transactionInfo;
        ProcessedDate = DateTime.UtcNow;

        // 🎯 EVENIMENT PRINCIPAL: PaymentCompleted
        AddDomainEvent(new PaymentCompleted(
            PaymentId,
            OrderId,
            Amount,
            transactionInfo.TransactionId,
            ProcessedDate.Value));
    }

    /// <summary>
    /// Marchează payment-ul ca eșuat
    /// </summary>
    public void Fail(string failureReason)
    {
        // Invariant: Poate eșua doar din Processing
        if (Status != PaymentStatus.Processing)
            throw new InvalidOperationException($"Cannot fail payment in status {Status}");

        // Invariant: Nu poate depăși numărul maxim de retry-uri
        if (RetryCount >= 3)
        {
            Status = PaymentStatus.Failed;
            FailureReason = $"Max retries exceeded. Last error: {failureReason}";
            ProcessedDate = DateTime.UtcNow;
            
            AddDomainEvent(new PaymentFailed(PaymentId, OrderId, FailureReason));
        }
        else
        {
            RetryCount++;
            Status = PaymentStatus.Pending; // Permite retry
            FailureReason = failureReason;
            
            AddDomainEvent(new PaymentRetrying(PaymentId, OrderId, RetryCount, failureReason));
        }
    }

    /// <summary>
    /// Anulează payment-ul
    /// </summary>
    public void Cancel(string reason)
    {
        // Invariant: Poate fi anulat doar din Pending
        if (Status != PaymentStatus.Pending)
            throw new InvalidOperationException($"Cannot cancel payment in status {Status}");

        Status = PaymentStatus.Cancelled;
        FailureReason = reason;
        ProcessedDate = DateTime.UtcNow;

        AddDomainEvent(new PaymentCancelled(PaymentId, OrderId, reason));
    }

    /// <summary>
    /// Verifică dacă payment-ul poate fi rambursat
    /// </summary>
    public bool CanBeRefunded()
    {
        return Status == PaymentStatus.Completed;
    }

    // ═══════════════════════════════════════════════════════════════════════════════
    // DOMAIN EVENTS MANAGEMENT
    // ═══════════════════════════════════════════════════════════════════════════════

    private void AddDomainEvent(object domainEvent)
    {
        _domainEvents.Add(domainEvent);
    }

    public void ClearDomainEvents()
    {
        _domainEvents.Clear();
    }
}

// ═══════════════════════════════════════════════════════════════════════════════
// REFUND AGGREGATE ROOT
// ═══════════════════════════════════════════════════════════════════════════════

/// <summary>
/// Refund Aggregate Root
/// Gestionează procesul de rambursare a unei plăți
/// </summary>
public class Refund
{
    // Aggregate ID
    public Guid RefundId { get; private set; }
    
    // Foreign Keys
    public Guid PaymentId { get; private set; }
    public Guid OrderId { get; private set; }
    
    // Properties
    public Money RefundAmount { get; private set; }
    public Money OriginalPaymentAmount { get; private set; }
    public RefundStatus Status { get; private set; }
    public RefundReason RefundReason { get; private set; }
    public TransactionInfo? TransactionInfo { get; private set; }
    
    public DateTime CreatedDate { get; private set; }
    public DateTime? ProcessedDate { get; private set; }
    public int RetryCount { get; private set; }
    public string? FailureReason { get; private set; }
    
    // Domain Events
    private List<object> _domainEvents = new();
    public IReadOnlyCollection<object> DomainEvents => _domainEvents.AsReadOnly();

    // Private constructor for EF Core
    private Refund() { }

    // ═══════════════════════════════════════════════════════════════════════════════
    // FACTORY METHOD - Initiate Refund
    // ═══════════════════════════════════════════════════════════════════════════════
    
    public static Refund Initiate(
        Guid paymentId,
        Guid orderId,
        Money refundAmount,
        Money originalPaymentAmount,
        RefundReason refundReason)
    {
        // Validări
        if (paymentId == Guid.Empty)
            throw new ArgumentException("PaymentId cannot be empty", nameof(paymentId));
        
        if (orderId == Guid.Empty)
            throw new ArgumentException("OrderId cannot be empty", nameof(orderId));
        
        // Invariant: Suma de rambursat trebuie să fie > 0
        if (refundAmount.Amount <= 0)
            throw new ArgumentException("Refund amount must be greater than zero", nameof(refundAmount));
        
        // Invariant: Suma de rambursat nu poate depăși suma originală
        if (refundAmount > originalPaymentAmount)
            throw new ArgumentException("Refund amount cannot exceed original payment amount");
        
        if (refundReason == null)
            throw new ArgumentNullException(nameof(refundReason));

        var refund = new Refund
        {
            RefundId = Guid.NewGuid(),
            PaymentId = paymentId,
            OrderId = orderId,
            RefundAmount = refundAmount,
            OriginalPaymentAmount = originalPaymentAmount,
            Status = RefundStatus.Initiated,
            RefundReason = refundReason,
            CreatedDate = DateTime.UtcNow,
            RetryCount = 0
        };

        // 🎯 EVENIMENT PRINCIPAL: RefundInitiated
        refund.AddDomainEvent(new RefundInitiated(
            refund.RefundId,
            refund.PaymentId,
            refund.OrderId,
            refund.RefundAmount,
            refund.RefundReason.Reason,
            refund.RefundReason.Category));

        return refund;
    }

    // ═══════════════════════════════════════════════════════════════════════════════
    // BUSINESS METHODS
    // ═══════════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Marchează refund-ul ca fiind în curs de procesare
    /// </summary>
    public void StartProcessing()
    {
        // Invariant: Poate trece în Processing doar din Initiated
        if (Status != RefundStatus.Initiated)
            throw new InvalidOperationException($"Cannot start processing refund in status {Status}");

        Status = RefundStatus.Processing;
        AddDomainEvent(new RefundProcessingStarted(RefundId, PaymentId, OrderId));
    }

    /// <summary>
    /// Completează refund-ul cu succes
    /// EVENIMENT: RefundCompleted
    /// </summary>
    public void Complete(TransactionInfo transactionInfo)
    {
        // Validări
        if (transactionInfo == null)
            throw new ArgumentNullException(nameof(transactionInfo));

        // Invariant: Poate fi completat doar din Processing
        if (Status != RefundStatus.Processing)
            throw new InvalidOperationException($"Cannot complete refund in status {Status}");
        
        // Invariant: Nu poate fi completat de două ori
        if (TransactionInfo != null)
            throw new InvalidOperationException("Refund already completed");
        
        // Invariant: ProcessedAt trebuie să fie după CreatedDate
        if (transactionInfo.ProcessedAt < CreatedDate)
            throw new InvalidOperationException("ProcessedAt cannot be before CreatedDate");

        Status = RefundStatus.Completed;
        TransactionInfo = transactionInfo;
        ProcessedDate = DateTime.UtcNow;

        // 🎯 EVENIMENT PRINCIPAL: RefundCompleted
        AddDomainEvent(new RefundCompleted(
            RefundId,
            PaymentId,
            OrderId,
            RefundAmount,
            transactionInfo.TransactionId,
            ProcessedDate.Value));
    }

    /// <summary>
    /// Marchează refund-ul ca eșuat
    /// </summary>
    public void Fail(string failureReason)
    {
        // Invariant: Poate eșua doar din Processing
        if (Status != RefundStatus.Processing)
            throw new InvalidOperationException($"Cannot fail refund in status {Status}");

        // Invariant: Nu poate depăși numărul maxim de retry-uri
        if (RetryCount >= 3)
        {
            Status = RefundStatus.Failed;
            FailureReason = $"Max retries exceeded. Last error: {failureReason}";
            ProcessedDate = DateTime.UtcNow;
            
            AddDomainEvent(new RefundFailed(RefundId, PaymentId, OrderId, FailureReason));
        }
        else
        {
            RetryCount++;
            Status = RefundStatus.Initiated; // Permite retry
            FailureReason = failureReason;
            
            AddDomainEvent(new RefundRetrying(RefundId, PaymentId, OrderId, RetryCount, failureReason));
        }
    }

    /// <summary>
    /// Anulează refund-ul
    /// </summary>
    public void Cancel(string reason)
    {
        // Invariant: Poate fi anulat doar din Initiated
        if (Status != RefundStatus.Initiated)
            throw new InvalidOperationException($"Cannot cancel refund in status {Status}");

        Status = RefundStatus.Cancelled;
        FailureReason = reason;
        ProcessedDate = DateTime.UtcNow;

        AddDomainEvent(new RefundCancelled(RefundId, PaymentId, OrderId, reason));
    }

    // ═══════════════════════════════════════════════════════════════════════════════
    // DOMAIN EVENTS MANAGEMENT
    // ═══════════════════════════════════════════════════════════════════════════════

    private void AddDomainEvent(object domainEvent)
    {
        _domainEvents.Add(domainEvent);
    }

    public void ClearDomainEvents()
    {
        _domainEvents.Clear();
    }
}
