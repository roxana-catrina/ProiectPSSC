// ═══════════════════════════════════════════════════════════════════════════════
// 📋 COMMANDS - RETURNS MANAGEMENT CONTEXT
// ═══════════════════════════════════════════════════════════════════════════════
// Comenzi pentru bounded context-ul Returns
// Data: November 7, 2025
// ═══════════════════════════════════════════════════════════════════════════════

using MediatR;
using ReturnsManagement.Domain.Returns;
using ReturnsManagement.Domain.Returns.Events;

namespace ReturnsManagement.Application.Returns.Commands;

/// <summary>
/// Base interface pentru toate comenzile
/// </summary>
public interface ICommand<out TResponse> : IRequest<TResponse>
{
}

// ═══════════════════════════════════════════════════════════════════════════════
// COMMAND DTOs
// ═══════════════════════════════════════════════════════════════════════════════

public record ReturnItemDto(
    Guid ProductId,
    string ProductName,
    int Quantity,
    decimal UnitPrice,
    string ProductCategory
);

public record ReceivedItemDto(
    Guid ProductId,
    int QuantityReceived,
    ProductCondition Condition,
    string ConditionNotes,
    bool AcceptableForResale
);

// ═══════════════════════════════════════════════════════════════════════════════
// 1️⃣ RequestReturnCommand - Comandă pentru solicitarea unui retur
// Eveniment rezultat: ReturnRequested
// ═══════════════════════════════════════════════════════════════════════════════

/// <summary>
/// Comandă pentru inițierea unui retur de către client
/// 
/// VALIDĂRI:
/// - Comanda originală trebuie să existe
/// - Comanda trebuie să fie în status "Delivered" sau "Paid"
/// - Perioada de retur nu a expirat (max 14-30 zile)
/// - Cantitatea returnată <= cantitatea comandată
/// - Motivul returului este valid și specificat
/// - Clientul este proprietarul comenzii
/// - Produsele pot fi returnate (nu sunt "non-returnable")
/// </summary>
public record RequestReturnCommand(
    Guid OrderId,
    Guid CustomerId,
    string CustomerName,
    string CustomerEmail,
    List<ReturnItemDto> Items,
    ReturnReason Reason,
    string DetailedDescription,
    DateTime OrderDeliveryDate,
    string ProductCategory
) : ICommand<RequestReturnResult>;

public record RequestReturnResult(
    bool Success,
    Guid? ReturnId,
    string RmaCode,
    string Message,
    List<string> ValidationErrors
);

// ═══════════════════════════════════════════════════════════════════════════════
// 2️⃣ ApproveReturnCommand - Comandă pentru aprobarea unui retur
// Eveniment rezultat: ReturnApproved
// ═══════════════════════════════════════════════════════════════════════════════

/// <summary>
/// Comandă pentru aprobarea unui retur de către manager
/// 
/// VALIDĂRI:
/// - Returul există și este în status "Requested"
/// - Utilizatorul are permisiuni de aprobare
/// - Politica de retur permite aprobarea
/// - Motivul este considerat valid
/// 
/// REGULI DE BUSINESS:
/// - Doar managerii pot aproba retururi
/// - Retururi peste o anumită valoare necesită aprobare suplimentară
/// </summary>
public record ApproveReturnCommand(
    Guid ReturnId,
    Guid ApprovedBy,
    string ApproverName,
    string ApprovalNotes,
    bool ApplyRestockingFee,
    string ApproverRole // "Manager", "Supervisor", "Administrator"
) : ICommand<ApproveReturnResult>;

public record ApproveReturnResult(
    bool Success,
    Guid ReturnId,
    string RmaCode,
    decimal ApprovedAmount,
    decimal RestockingFee,
    string Message,
    List<string> ValidationErrors
);

// ═══════════════════════════════════════════════════════════════════════════════
// 3️⃣ ReceiveReturnCommand - Comandă pentru primirea produselor returnate
// Eveniment rezultat: ReturnReceived
// ═══════════════════════════════════════════════════════════════════════════════

/// <summary>
/// Comandă pentru marcarea returului ca fiind primit în depozit
/// 
/// VALIDĂRI:
/// - Returul este în status "Approved"
/// - Cantitatea primită <= cantitatea aprobată
/// - Starea produsului este documentată (Intact/Damaged/Opened)
/// - Cod de tracking valid (dacă există)
/// 
/// REGULI DE BUSINESS:
/// - Se verifică starea fizică a produselor
/// - Produsele deteriorate pot afecta rambursarea
/// </summary>
public record ReceiveReturnCommand(
    Guid ReturnId,
    Guid ReceivedBy,
    string ReceiverName,
    List<ReceivedItemDto> ReceivedItems,
    string TrackingNumber,
    string WarehouseLocation,
    string InspectionNotes
) : ICommand<ReceiveReturnResult>;

public record ReceiveReturnResult(
    bool Success,
    Guid ReturnId,
    string RmaCode,
    bool AllItemsReceived,
    int TotalItemsReceived,
    string Message,
    List<string> ValidationErrors
);

// ═══════════════════════════════════════════════════════════════════════════════
// 4️⃣ AcceptReturnCommand - Comandă pentru acceptarea finală a returului
// Eveniment rezultat: ReturnAccepted
// ═══════════════════════════════════════════════════════════════════════════════

/// <summary>
/// Comandă pentru acceptarea finală a returului și procesarea rambursării
/// 
/// VALIDĂRI:
/// - Returul este în status "Received"
/// - Toate produsele au fost inspectate
/// - Metoda de rambursare este validă
/// - Contul/cardul pentru rambursare există
/// 
/// REGULI DE BUSINESS:
/// - Rambursarea se face prin aceeași metodă ca plata originală
/// - Taxa de restocking poate fi aplicată (ex: 10% pentru produse deschise)
/// </summary>
public record AcceptReturnCommand(
    Guid ReturnId,
    Guid AcceptedBy,
    string AccepterName,
    RefundMethod RefundMethod,
    string RefundReference,
    string Notes,
    bool InventoryUpdated
) : ICommand<AcceptReturnResult>;

public record AcceptReturnResult(
    bool Success,
    Guid ReturnId,
    string RmaCode,
    decimal RefundAmount,
    decimal RestockingFee,
    decimal FinalRefundAmount,
    string RefundMethod,
    string RefundReference,
    string Message,
    List<string> ValidationErrors
);

// ═══════════════════════════════════════════════════════════════════════════════
// 5️⃣ RejectReturnCommand - Comandă pentru respingerea unui retur (BONUS)
// Eveniment rezultat: ReturnRejected
// ═══════════════════════════════════════════════════════════════════════════════

/// <summary>
/// Comandă pentru respingerea unui retur
/// </summary>
public record RejectReturnCommand(
    Guid ReturnId,
    Guid RejectedBy,
    string RejectorName,
    string RejectionReason,
    string DetailedExplanation,
    bool NotifyCustomer
) : ICommand<RejectReturnResult>;

public record RejectReturnResult(
    bool Success,
    Guid ReturnId,
    string RmaCode,
    string Message,
    List<string> ValidationErrors
);

// ═══════════════════════════════════════════════════════════════════════════════
// 6️⃣ CancelReturnCommand - Comandă pentru anularea unui retur de către client
// ═══════════════════════════════════════════════════════════════════════════════

/// <summary>
/// Comandă pentru anularea unui retur de către client
/// Poate fi folosită doar dacă returul nu a fost încă trimis
/// </summary>
public record CancelReturnCommand(
    Guid ReturnId,
    Guid CustomerId,
    string CancellationReason
) : ICommand<CancelReturnResult>;

public record CancelReturnResult(
    bool Success,
    Guid ReturnId,
    string Message,
    List<string> ValidationErrors
);

// ═══════════════════════════════════════════════════════════════════════════════
// 7️⃣ GetReturnStatusCommand - Query pentru obținerea statusului unui retur
// ═══════════════════════════════════════════════════════════════════════════════

/// <summary>
/// Query pentru obținerea statusului și detaliilor unui retur
/// </summary>
public record GetReturnStatusCommand(
    Guid ReturnId,
    Guid? CustomerId // Opțional, pentru verificarea proprietății
) : ICommand<GetReturnStatusResult>;

public record GetReturnStatusResult(
    bool Success,
    Guid ReturnId,
    string RmaCode,
    ReturnStatus Status,
    string StatusDescription,
    DateTime RequestedAt,
    DateTime? ApprovedAt,
    DateTime? ReceivedAt,
    DateTime? AcceptedAt,
    decimal TotalAmount,
    decimal RefundAmount,
    int DaysUntilExpiration,
    List<ReturnItemStatusDto> Items,
    string Message
);

public record ReturnItemStatusDto(
    Guid ProductId,
    string ProductName,
    int QuantityRequested,
    int QuantityReceived,
    ProductCondition? Condition,
    string ConditionNotes
);

