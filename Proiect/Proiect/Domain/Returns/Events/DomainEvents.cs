// ═══════════════════════════════════════════════════════════════════════════════
// 📦 BOUNDED CONTEXT: RETURNS MANAGEMENT - DOMAIN EVENTS
// ═══════════════════════════════════════════════════════════════════════════════
// Implementare DDD în C# pentru evenimentele de domeniu
// Data: November 7, 2025
// ═══════════════════════════════════════════════════════════════════════════════

namespace ReturnsManagement.Domain.Returns.Events;

// ═══════════════════════════════════════════════════════════════════════════════
// BASE DOMAIN EVENT
// ═══════════════════════════════════════════════════════════════════════════════

/// <summary>
/// Interfață de bază pentru toate evenimentele de domeniu
/// </summary>
public interface IDomainEvent
{
    DateTime OccurredOn { get; }
    Guid EventId { get; }
}

public abstract record DomainEvent : IDomainEvent
{
    public Guid EventId { get; init; } = Guid.NewGuid();
    public DateTime OccurredOn { get; init; } = DateTime.UtcNow;
}

// ═══════════════════════════════════════════════════════════════════════════════
// 1️⃣ ReturnRequested - Eveniment când clientul solicită un retur
// ═══════════════════════════════════════════════════════════════════════════════

/// <summary>
/// Eveniment declanșat când un client solicită returnarea produselor
/// Comandă care declanșează: RequestReturn
/// </summary>
public record ReturnRequested : DomainEvent
{
    public Guid ReturnId { get; init; }
    public Guid OrderId { get; init; }
    public Guid CustomerId { get; init; }
    public string CustomerName { get; init; }
    public string CustomerEmail { get; init; }
    public List<ReturnItemRequested> Items { get; init; }
    public string ReturnReason { get; init; }
    public string DetailedDescription { get; init; }
    public decimal TotalAmount { get; init; }
    public DateTime RequestedAt { get; init; }
    public string RmaCode { get; init; } // Return Merchandise Authorization Code

    public ReturnRequested(
        Guid returnId,
        Guid orderId,
        Guid customerId,
        string customerName,
        string customerEmail,
        List<ReturnItemRequested> items,
        string returnReason,
        string detailedDescription,
        decimal totalAmount,
        string rmaCode)
    {
        ReturnId = returnId;
        OrderId = orderId;
        CustomerId = customerId;
        CustomerName = customerName;
        CustomerEmail = customerEmail;
        Items = items;
        ReturnReason = returnReason;
        DetailedDescription = detailedDescription;
        TotalAmount = totalAmount;
        RequestedAt = DateTime.UtcNow;
        RmaCode = rmaCode;
    }
}

public record ReturnItemRequested(
    Guid ProductId,
    string ProductName,
    int Quantity,
    decimal UnitPrice,
    decimal TotalPrice
);

// ═══════════════════════════════════════════════════════════════════════════════
// 2️⃣ ReturnApproved - Eveniment când returul este aprobat
// ═══════════════════════════════════════════════════════════════════════════════

/// <summary>
/// Eveniment declanșat când un retur este aprobat de către manager
/// Comandă care declanșează: ApproveReturn
/// </summary>
public record ReturnApproved : DomainEvent
{
    public Guid ReturnId { get; init; }
    public Guid OrderId { get; init; }
    public Guid ApprovedBy { get; init; }
    public string ApproverName { get; init; }
    public DateTime ApprovedAt { get; init; }
    public string ApprovalNotes { get; init; }
    public string RmaCode { get; init; }
    public decimal ApprovedAmount { get; init; }
    public bool RestockingFeeApplied { get; init; }
    public decimal RestockingFeePercentage { get; init; }
    public List<Guid> ApprovedProductIds { get; init; }

    public ReturnApproved(
        Guid returnId,
        Guid orderId,
        Guid approvedBy,
        string approverName,
        string approvalNotes,
        string rmaCode,
        decimal approvedAmount,
        bool restockingFeeApplied,
        decimal restockingFeePercentage,
        List<Guid> approvedProductIds)
    {
        ReturnId = returnId;
        OrderId = orderId;
        ApprovedBy = approvedBy;
        ApproverName = approverName;
        ApprovedAt = DateTime.UtcNow;
        ApprovalNotes = approvalNotes;
        RmaCode = rmaCode;
        ApprovedAmount = approvedAmount;
        RestockingFeeApplied = restockingFeeApplied;
        RestockingFeePercentage = restockingFeePercentage;
        ApprovedProductIds = approvedProductIds;
    }
}

// ═══════════════════════════════════════════════════════════════════════════════
// 3️⃣ ReturnReceived - Eveniment când produsele returnate sunt primite
// ═══════════════════════════════════════════════════════════════════════════════

/// <summary>
/// Eveniment declanșat când produsele returnate sunt primite în depozit
/// Comandă care declanșează: ReceiveReturn
/// </summary>
public record ReturnReceived : DomainEvent
{
    public Guid ReturnId { get; init; }
    public Guid OrderId { get; init; }
    public string RmaCode { get; init; }
    public DateTime ReceivedAt { get; init; }
    public Guid ReceivedBy { get; init; }
    public string ReceiverName { get; init; }
    public List<ReturnItemReceived> ReceivedItems { get; init; }
    public string TrackingNumber { get; init; }
    public string WarehouseLocation { get; init; }
    public string InspectionNotes { get; init; }
    public bool AllItemsReceived { get; init; }

    public ReturnReceived(
        Guid returnId,
        Guid orderId,
        string rmaCode,
        Guid receivedBy,
        string receiverName,
        List<ReturnItemReceived> receivedItems,
        string trackingNumber,
        string warehouseLocation,
        string inspectionNotes,
        bool allItemsReceived)
    {
        ReturnId = returnId;
        OrderId = orderId;
        RmaCode = rmaCode;
        ReceivedAt = DateTime.UtcNow;
        ReceivedBy = receivedBy;
        ReceiverName = receiverName;
        ReceivedItems = receivedItems;
        TrackingNumber = trackingNumber;
        WarehouseLocation = warehouseLocation;
        InspectionNotes = inspectionNotes;
        AllItemsReceived = allItemsReceived;
    }
}

public record ReturnItemReceived(
    Guid ProductId,
    string ProductName,
    int QuantityReceived,
    ProductCondition Condition,
    string ConditionNotes,
    bool AcceptableForResale
);


// ═══════════════════════════════════════════════════════════════════════════════
// 4️⃣ ReturnAccepted - Eveniment când returul este acceptat final
// ═══════════════════════════════════════════════════════════════════════════════

/// <summary>
/// Eveniment declanșat când returul este acceptat final și se procesează rambursarea
/// Comandă care declanșează: AcceptReturn
/// </summary>
public record ReturnAccepted : DomainEvent
{
    public Guid ReturnId { get; init; }
    public Guid OrderId { get; init; }
    public Guid CustomerId { get; init; }
    public string RmaCode { get; init; }
    public DateTime AcceptedAt { get; init; }
    public Guid AcceptedBy { get; init; }
    public string AccepterName { get; init; }
    public decimal RefundAmount { get; init; }
    public decimal RestockingFee { get; init; }
    public decimal FinalRefundAmount { get; init; }
    public string RefundMethod { get; init; }
    public string RefundReference { get; init; }
    public List<Guid> AcceptedProductIds { get; init; }
    public string Notes { get; init; }
    public bool InventoryUpdated { get; init; }

    public ReturnAccepted(
        Guid returnId,
        Guid orderId,
        Guid customerId,
        string rmaCode,
        Guid acceptedBy,
        string accepterName,
        decimal refundAmount,
        decimal restockingFee,
        string refundMethod,
        string refundReference,
        List<Guid> acceptedProductIds,
        string notes,
        bool inventoryUpdated)
    {
        ReturnId = returnId;
        OrderId = orderId;
        CustomerId = customerId;
        RmaCode = rmaCode;
        AcceptedAt = DateTime.UtcNow;
        AcceptedBy = acceptedBy;
        AccepterName = accepterName;
        RefundAmount = refundAmount;
        RestockingFee = restockingFee;
        FinalRefundAmount = refundAmount - restockingFee;
        RefundMethod = refundMethod;
        RefundReference = refundReference;
        AcceptedProductIds = acceptedProductIds;
        Notes = notes;
        InventoryUpdated = inventoryUpdated;
    }
}

// ═══════════════════════════════════════════════════════════════════════════════
// 5️⃣ ReturnRejected - Eveniment când returul este respins (BONUS)
// ═══════════════════════════════════════════════════════════════════════════════

/// <summary>
/// Eveniment declanșat când un retur este respins
/// Comandă care declanșează: RejectReturn
/// </summary>
public record ReturnRejected : DomainEvent
{
    public Guid ReturnId { get; init; }
    public Guid OrderId { get; init; }
    public Guid CustomerId { get; init; }
    public string RmaCode { get; init; }
    public DateTime RejectedAt { get; init; }
    public Guid RejectedBy { get; init; }
    public string RejectorName { get; init; }
    public string RejectionReason { get; init; }
    public string DetailedExplanation { get; init; }
    public bool CustomerNotified { get; init; }

    public ReturnRejected(
        Guid returnId,
        Guid orderId,
        Guid customerId,
        string rmaCode,
        Guid rejectedBy,
        string rejectorName,
        string rejectionReason,
        string detailedExplanation,
        bool customerNotified)
    {
        ReturnId = returnId;
        OrderId = orderId;
        CustomerId = customerId;
        RmaCode = rmaCode;
        RejectedAt = DateTime.UtcNow;
        RejectedBy = rejectedBy;
        RejectorName = rejectorName;
        RejectionReason = rejectionReason;
        DetailedExplanation = detailedExplanation;
        CustomerNotified = customerNotified;
    }
}

// ═══════════════════════════════════════════════════════════════════════════════
// 6️⃣ ReturnModified - Eveniment când returul este modificat (BONUS)
// ═══════════════════════════════════════════════════════════════════════════════

/// <summary>
/// Eveniment declanșat când detaliile unui retur sunt modificate
/// </summary>
public record ReturnModified : DomainEvent
{
    public Guid ReturnId { get; init; }
    public Guid ModifiedBy { get; init; }
    public string ModifierName { get; init; }
    public DateTime ModifiedAt { get; init; }
    public string ModificationReason { get; init; }
    public Dictionary<string, string> Changes { get; init; }

    public ReturnModified(
        Guid returnId,
        Guid modifiedBy,
        string modifierName,
        string modificationReason,
        Dictionary<string, string> changes)
    {
        ReturnId = returnId;
        ModifiedBy = modifiedBy;
        ModifierName = modifierName;
        ModifiedAt = DateTime.UtcNow;
        ModificationReason = modificationReason;
        Changes = changes;
    }
}

