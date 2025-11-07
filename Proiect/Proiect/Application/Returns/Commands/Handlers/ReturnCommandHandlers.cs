// ═══════════════════════════════════════════════════════════════════════════════
// 📋 COMMAND HANDLERS - RETURNS MANAGEMENT CONTEXT
// ═══════════════════════════════════════════════════════════════════════════════
// Handlers pentru procesarea comenzilor din bounded context-ul Returns
// Data: November 7, 2025
// ═══════════════════════════════════════════════════════════════════════════════

namespace ReturnsManagement.Application.Returns.Commands.Handlers;

using MediatR;
using ReturnsManagement.Domain.Returns;
using ReturnsManagement.Domain.Returns.Services;

// ═══════════════════════════════════════════════════════════════════════════════
// REPOSITORY INTERFACE
// ═══════════════════════════════════════════════════════════════════════════════

/// <summary>
/// Repository interface pentru Return aggregate
/// </summary>
public interface IReturnRepository
{
    Task<Return?> GetByIdAsync(Guid returnId);
    Task<Return?> GetByRmaCodeAsync(string rmaCode);
    Task<List<Return>> GetByOrderIdAsync(Guid orderId);
    Task<List<Return>> GetByCustomerIdAsync(Guid customerId);
    Task AddAsync(Return @return);
    Task UpdateAsync(Return @return);
    Task<bool> ExistsAsync(Guid returnId);
}

/// <summary>
/// Interface pentru comunicare cu alte bounded contexts
/// </summary>
public interface IOrderService
{
    Task<bool> OrderExistsAsync(Guid orderId);
    Task<OrderDto?> GetOrderAsync(Guid orderId);
}

public record OrderDto(
    Guid OrderId,
    Guid CustomerId,
    string CustomerName,
    string CustomerEmail,
    DateTime DeliveryDate,
    string Status,
    decimal TotalAmount,
    string PaymentMethod
);

// ═══════════════════════════════════════════════════════════════════════════════
// 1️⃣ RequestReturnCommandHandler
// ═══════════════════════════════════════════════════════════════════════════════

/// <summary>
/// Handler pentru comanda RequestReturn
/// Creează un nou retur și generează evenimentul ReturnRequested
/// </summary>
public class RequestReturnCommandHandler : IRequestHandler<RequestReturnCommand, RequestReturnResult>
{
    private readonly IReturnRepository _returnRepository;
    private readonly IOrderService _orderService;
    private readonly ReturnEligibilityService _eligibilityService;
    private readonly ReturnPolicyService _policyService;

    public RequestReturnCommandHandler(
        IReturnRepository returnRepository,
        IOrderService orderService,
        ReturnEligibilityService eligibilityService,
        ReturnPolicyService policyService)
    {
        _returnRepository = returnRepository;
        _orderService = orderService;
        _eligibilityService = eligibilityService;
        _policyService = policyService;
    }

    public async Task<RequestReturnResult> Handle(RequestReturnCommand request, CancellationToken cancellationToken)
    {
        var validationErrors = new List<string>();

        try
        {
            // Validare 1: Verifică existența comenzii
            var order = await _orderService.GetOrderAsync(request.OrderId);
            if (order == null)
            {
                validationErrors.Add($"Order {request.OrderId} not found");
                return new RequestReturnResult(false, null, string.Empty, "Order not found", validationErrors);
            }

            // Validare 2: Verifică că clientul este proprietarul comenzii
            if (order.CustomerId != request.CustomerId)
            {
                validationErrors.Add("Customer is not the owner of this order");
                return new RequestReturnResult(false, null, string.Empty, "Unauthorized", validationErrors);
            }

            // Validare 3: Verifică statusul comenzii
            if (order.Status != "Delivered" && order.Status != "Paid")
            {
                validationErrors.Add($"Order must be in 'Delivered' or 'Paid' status. Current status: {order.Status}");
                return new RequestReturnResult(false, null, string.Empty, "Invalid order status", validationErrors);
            }

            // Validare 4: Verifică eligibilitatea pentru retur
            var eligibility = _eligibilityService.CheckEligibility(
                order.DeliveryDate,
                request.ProductCategory,
                order.TotalAmount);

            if (!eligibility.IsEligible)
            {
                validationErrors.Add(eligibility.Reason);
                return new RequestReturnResult(false, null, string.Empty, "Return not eligible", validationErrors);
            }

            // Validare 5: Verifică dacă există deja un retur activ pentru această comandă
            var existingReturns = await _returnRepository.GetByOrderIdAsync(request.OrderId);
            if (existingReturns.Any(r => r.Status != ReturnStatus.Completed && r.Status != ReturnStatus.Rejected))
            {
                validationErrors.Add("An active return already exists for this order");
                return new RequestReturnResult(false, null, string.Empty, "Duplicate return", validationErrors);
            }

            // Obține politica aplicabilă
            var policy = eligibility.ApplicablePolicy;

            // Validare 6: Verifică cantitățile
            if (request.Items == null || !request.Items.Any())
            {
                validationErrors.Add("Return must contain at least one item");
                return new RequestReturnResult(false, null, string.Empty, "No items specified", validationErrors);
            }

            foreach (var item in request.Items)
            {
                if (item.Quantity <= 0)
                {
                    validationErrors.Add($"Invalid quantity for product {item.ProductName}");
                }
            }

            if (validationErrors.Any())
            {
                return new RequestReturnResult(false, null, string.Empty, "Validation failed", validationErrors);
            }

            // Creare retur folosind Factory Method
            var items = request.Items.Select(i => (i.ProductId, i.ProductName, i.Quantity, i.UnitPrice)).ToList();
            
            var @return = Return.RequestReturn(
                request.OrderId,
                request.CustomerId,
                request.CustomerName,
                request.CustomerEmail,
                request.OrderDeliveryDate,
                items,
                request.Reason,
                request.DetailedDescription,
                policy
            );

            // Validare invarianți
            @return.ValidateInvariants();

            // Salvare în repository
            await _returnRepository.AddAsync(@return);

            // Aici ar trebui să publicăm evenimentele de domeniu
            // PublishDomainEvents(@return.DomainEvents);

            return new RequestReturnResult(
                true,
                @return.ReturnId,
                @return.RmaCode.Value,
                $"Return request created successfully. RMA Code: {@return.RmaCode.Value}. You have {eligibility.DaysRemaining} days remaining in the return window.",
                new List<string>()
            );
        }
        catch (Exception ex)
        {
            validationErrors.Add(ex.Message);
            return new RequestReturnResult(false, null, string.Empty, "Error creating return request", validationErrors);
        }
    }
}

// ═══════════════════════════════════════════════════════════════════════════════
// 2️⃣ ApproveReturnCommandHandler
// ═══════════════════════════════════════════════════════════════════════════════

/// <summary>
/// Handler pentru comanda ApproveReturn
/// Aprobă un retur și generează evenimentul ReturnApproved
/// </summary>
public class ApproveReturnCommandHandler : IRequestHandler<ApproveReturnCommand, ApproveReturnResult>
{
    private readonly IReturnRepository _returnRepository;
    private readonly ReturnAuthorizationService _authorizationService;

    public ApproveReturnCommandHandler(
        IReturnRepository returnRepository,
        ReturnAuthorizationService authorizationService)
    {
        _returnRepository = returnRepository;
        _authorizationService = authorizationService;
    }

    public async Task<ApproveReturnResult> Handle(ApproveReturnCommand request, CancellationToken cancellationToken)
    {
        var validationErrors = new List<string>();

        try
        {
            // Validare 1: Verifică existența returului
            var @return = await _returnRepository.GetByIdAsync(request.ReturnId);
            if (@return == null)
            {
                validationErrors.Add($"Return {request.ReturnId} not found");
                return new ApproveReturnResult(false, request.ReturnId, string.Empty, 0, 0, "Return not found", validationErrors);
            }

            // Validare 2: Verifică autorizația
            var userRole = ParseUserRole(request.ApproverRole);
            var authResult = _authorizationService.CanApproveReturn(
                userRole,
                @return.TotalAmount,
                @return.Reason
            );

            if (!authResult.IsAuthorized)
            {
                validationErrors.Add(authResult.Reason);
                if (authResult.RequiresEscalation)
                {
                    validationErrors.Add($"This return requires escalation to {authResult.EscalationLevel}");
                }
                return new ApproveReturnResult(false, request.ReturnId, @return.RmaCode.Value, 0, 0, 
                    "Insufficient authorization", validationErrors);
            }

            // Aprobare retur
            @return.Approve(
                request.ApprovedBy,
                request.ApproverName,
                request.ApprovalNotes,
                request.ApplyRestockingFee
            );

            // Validare invarianți
            @return.ValidateInvariants();

            // Update repository
            await _returnRepository.UpdateAsync(@return);

            // Publicare evenimente
            // PublishDomainEvents(@return.DomainEvents);

            return new ApproveReturnResult(
                true,
                @return.ReturnId,
                @return.RmaCode.Value,
                @return.RefundAmount.Amount,
                @return.RestockingFee.Amount,
                $"Return approved successfully. Refund amount: {@return.RefundAmount}",
                new List<string>()
            );
        }
        catch (Exception ex)
        {
            validationErrors.Add(ex.Message);
            return new ApproveReturnResult(false, request.ReturnId, string.Empty, 0, 0, 
                "Error approving return", validationErrors);
        }
    }

    private ReturnAuthorizationService.UserRole ParseUserRole(string role)
    {
        return role?.ToLower() switch
        {
            "manager" => ReturnAuthorizationService.UserRole.Manager,
            "supervisor" => ReturnAuthorizationService.UserRole.Supervisor,
            "administrator" => ReturnAuthorizationService.UserRole.Administrator,
            _ => ReturnAuthorizationService.UserRole.CustomerService
        };
    }
}

// ═══════════════════════════════════════════════════════════════════════════════
// 3️⃣ ReceiveReturnCommandHandler
// ═══════════════════════════════════════════════════════════════════════════════

/// <summary>
/// Handler pentru comanda ReceiveReturn
/// Marchează produsele ca fiind primite și generează evenimentul ReturnReceived
/// </summary>
public class ReceiveReturnCommandHandler : IRequestHandler<ReceiveReturnCommand, ReceiveReturnResult>
{
    private readonly IReturnRepository _returnRepository;

    public ReceiveReturnCommandHandler(IReturnRepository returnRepository)
    {
        _returnRepository = returnRepository;
    }

    public async Task<ReceiveReturnResult> Handle(ReceiveReturnCommand request, CancellationToken cancellationToken)
    {
        var validationErrors = new List<string>();

        try
        {
            // Validare 1: Verifică existența returului
            var @return = await _returnRepository.GetByIdAsync(request.ReturnId);
            if (@return == null)
            {
                validationErrors.Add($"Return {request.ReturnId} not found");
                return new ReceiveReturnResult(false, request.ReturnId, string.Empty, false, 0, 
                    "Return not found", validationErrors);
            }

            // Validare 2: Verifică că returul este în status Approved
            if (@return.Status != ReturnStatus.Approved)
            {
                validationErrors.Add($"Cannot receive return in status {@return.Status}. Expected: Approved");
                return new ReceiveReturnResult(false, request.ReturnId, @return.RmaCode.Value, false, 0,
                    "Invalid status", validationErrors);
            }

            // Validare 3: Verifică că toate produsele din cerere fac parte din retur
            foreach (var item in request.ReceivedItems)
            {
                if (!@return.Items.Any(i => i.ProductId == item.ProductId))
                {
                    validationErrors.Add($"Product {item.ProductId} is not part of this return");
                }
            }

            if (validationErrors.Any())
            {
                return new ReceiveReturnResult(false, request.ReturnId, @return.RmaCode.Value, false, 0,
                    "Validation failed", validationErrors);
            }

            // Procesare primire produse
            var receivedItems = request.ReceivedItems.Select<ReceivedItemDto, (Guid, int, ProductCondition, string, bool)>(i => 
                (i.ProductId, i.QuantityReceived, i.Condition, i.ConditionNotes, i.AcceptableForResale)
            ).ToList();

            @return.ReceiveProducts(
                request.ReceivedBy,
                request.ReceiverName,
                receivedItems,
                request.TrackingNumber,
                request.WarehouseLocation,
                request.InspectionNotes
            );

            // Validare invarianți
            @return.ValidateInvariants();

            // Update repository
            await _returnRepository.UpdateAsync(@return);

            // Publicare evenimente
            // PublishDomainEvents(@return.DomainEvents);

            var totalReceived = request.ReceivedItems.Sum(i => i.QuantityReceived);
            var allItemsReceived = @return.Items.All(i => i.IsFullyReceived());

            return new ReceiveReturnResult(
                true,
                @return.ReturnId,
                @return.RmaCode.Value,
                allItemsReceived,
                totalReceived,
                $"Return received successfully. Total items: {totalReceived}. All items received: {allItemsReceived}",
                new List<string>()
            );
        }
        catch (Exception ex)
        {
            validationErrors.Add(ex.Message);
            return new ReceiveReturnResult(false, request.ReturnId, string.Empty, false, 0,
                "Error receiving return", validationErrors);
        }
    }
}

// ═══════════════════════════════════════════════════════════════════════════════
// 4️⃣ AcceptReturnCommandHandler
// ═══════════════════════════════════════════════════════════════════════════════

/// <summary>
/// Handler pentru comanda AcceptReturn
/// Acceptă returul final și generează evenimentul ReturnAccepted
/// </summary>
public class AcceptReturnCommandHandler : IRequestHandler<AcceptReturnCommand, AcceptReturnResult>
{
    private readonly IReturnRepository _returnRepository;
    private readonly RefundCalculationService _refundCalculationService;

    public AcceptReturnCommandHandler(
        IReturnRepository returnRepository,
        RefundCalculationService refundCalculationService)
    {
        _returnRepository = returnRepository;
        _refundCalculationService = refundCalculationService;
    }

    public async Task<AcceptReturnResult> Handle(AcceptReturnCommand request, CancellationToken cancellationToken)
    {
        var validationErrors = new List<string>();

        try
        {
            // Validare 1: Verifică existența returului
            var @return = await _returnRepository.GetByIdAsync(request.ReturnId);
            if (@return == null)
            {
                validationErrors.Add($"Return {request.ReturnId} not found");
                return new AcceptReturnResult(false, request.ReturnId, string.Empty, 0, 0, 0, string.Empty, string.Empty,
                    "Return not found", validationErrors);
            }

            // Validare 2: Verifică statusul
            if (@return.Status != ReturnStatus.Received)
            {
                validationErrors.Add($"Cannot accept return in status {@return.Status}. Expected: Received");
                return new AcceptReturnResult(false, request.ReturnId, @return.RmaCode.Value, 0, 0, 0, 
                    string.Empty, string.Empty, "Invalid status", validationErrors);
            }

            // Validare 3: Verifică că toate produsele au fost inspectate
            if (@return.Items.Any(i => i.ReceivedCondition == null))
            {
                validationErrors.Add("All items must be inspected before accepting the return");
                return new AcceptReturnResult(false, request.ReturnId, @return.RmaCode.Value, 0, 0, 0,
                    string.Empty, string.Empty, "Incomplete inspection", validationErrors);
            }

            // Validare 4: Validare suma de rambursat
            if (!_refundCalculationService.ValidateRefundAmount(@return.RefundAmount, @return.TotalAmount))
            {
                validationErrors.Add("Invalid refund amount");
                return new AcceptReturnResult(false, request.ReturnId, @return.RmaCode.Value, 0, 0, 0,
                    string.Empty, string.Empty, "Invalid refund amount", validationErrors);
            }

            // Acceptare retur
            @return.AcceptAndProcessRefund(
                request.AcceptedBy,
                request.AccepterName,
                request.RefundMethod,
                request.RefundReference,
                request.Notes,
                request.InventoryUpdated
            );

            // Validare invarianți
            @return.ValidateInvariants();

            // Update repository
            await _returnRepository.UpdateAsync(@return);

            // Publicare evenimente
            // PublishDomainEvents(@return.DomainEvents);
            // Evenimentul ReturnAccepted va declanșa procesarea rambursării în Payment Context

            return new AcceptReturnResult(
                true,
                @return.ReturnId,
                @return.RmaCode.Value,
                @return.TotalAmount.Amount,
                @return.RestockingFee.Amount,
                @return.RefundAmount.Amount,
                request.RefundMethod.ToString(),
                request.RefundReference,
                $"Return accepted successfully. Refund of {@return.RefundAmount} will be processed via {request.RefundMethod}",
                new List<string>()
            );
        }
        catch (Exception ex)
        {
            validationErrors.Add(ex.Message);
            return new AcceptReturnResult(false, request.ReturnId, string.Empty, 0, 0, 0, string.Empty, string.Empty,
                "Error accepting return", validationErrors);
        }
    }
}

// ═══════════════════════════════════════════════════════════════════════════════
// 5️⃣ RejectReturnCommandHandler
// ═══════════════════════════════════════════════════════════════════════════════

/// <summary>
/// Handler pentru comanda RejectReturn
/// Respinge un retur și generează evenimentul ReturnRejected
/// </summary>
public class RejectReturnCommandHandler : IRequestHandler<RejectReturnCommand, RejectReturnResult>
{
    private readonly IReturnRepository _returnRepository;
    private readonly ReturnAuthorizationService _authorizationService;

    public RejectReturnCommandHandler(
        IReturnRepository returnRepository,
        ReturnAuthorizationService authorizationService)
    {
        _returnRepository = returnRepository;
        _authorizationService = authorizationService;
    }

    public async Task<RejectReturnResult> Handle(RejectReturnCommand request, CancellationToken cancellationToken)
    {
        var validationErrors = new List<string>();

        try
        {
            // Validare 1: Verifică existența returului
            var @return = await _returnRepository.GetByIdAsync(request.ReturnId);
            if (@return == null)
            {
                validationErrors.Add($"Return {request.ReturnId} not found");
                return new RejectReturnResult(false, request.ReturnId, string.Empty, "Return not found", validationErrors);
            }

            // Validare 2: Verifică că utilizatorul poate respinge returul
            // Presupunem că RejectorName conține și rolul
            // În practică, ar trebui verificat din context/claims

            // Respingere retur
            @return.Reject(
                request.RejectedBy,
                request.RejectorName,
                request.RejectionReason,
                request.DetailedExplanation
            );

            // Update repository
            await _returnRepository.UpdateAsync(@return);

            // Publicare evenimente + notificare client
            // PublishDomainEvents(@return.DomainEvents);
            // if (request.NotifyCustomer) { SendRejectionNotification(@return); }

            return new RejectReturnResult(
                true,
                @return.ReturnId,
                @return.RmaCode.Value,
                $"Return rejected. Reason: {request.RejectionReason}",
                new List<string>()
            );
        }
        catch (Exception ex)
        {
            validationErrors.Add(ex.Message);
            return new RejectReturnResult(false, request.ReturnId, string.Empty, "Error rejecting return", validationErrors);
        }
    }
}

// ═══════════════════════════════════════════════════════════════════════════════
// 6️⃣ GetReturnStatusCommandHandler
// ═══════════════════════════════════════════════════════════════════════════════

/// <summary>
/// Handler pentru query GetReturnStatus
/// Returnează statusul și detaliile unui retur
/// </summary>
public class GetReturnStatusCommandHandler : IRequestHandler<GetReturnStatusCommand, GetReturnStatusResult>
{
    private readonly IReturnRepository _returnRepository;

    public GetReturnStatusCommandHandler(IReturnRepository returnRepository)
    {
        _returnRepository = returnRepository;
    }

    public async Task<GetReturnStatusResult> Handle(GetReturnStatusCommand request, CancellationToken cancellationToken)
    {
        var @return = await _returnRepository.GetByIdAsync(request.ReturnId);
        
        if (@return == null)
        {
            return new GetReturnStatusResult(
                false, request.ReturnId, string.Empty, ReturnStatus.Requested, string.Empty,
                DateTime.MinValue, null, null, null, 0, 0, 0, new List<ReturnItemStatusDto>(),
                "Return not found"
            );
        }

        // Verifică proprietatea (dacă CustomerId este furnizat)
        if (request.CustomerId.HasValue && @return.CustomerId != request.CustomerId.Value)
        {
            return new GetReturnStatusResult(
                false, request.ReturnId, string.Empty, ReturnStatus.Requested, string.Empty,
                DateTime.MinValue, null, null, null, 0, 0, 0, new List<ReturnItemStatusDto>(),
                "Unauthorized access"
            );
        }

        var items = @return.Items.Select(i => new ReturnItemStatusDto(
            i.ProductId,
            i.ProductName,
            i.QuantityRequested,
            i.QuantityReceived,
            i.ReceivedCondition,
            i.ConditionNotes
        )).ToList();

        return new GetReturnStatusResult(
            true,
            @return.ReturnId,
            @return.RmaCode.Value,
            @return.Status,
            GetStatusDescription(@return.Status),
            @return.RequestedAt,
            @return.ApprovedAt,
            @return.ReceivedAt,
            @return.AcceptedAt,
            @return.TotalAmount.Amount,
            @return.RefundAmount.Amount,
            @return.ReturnWindow.DaysRemaining,
            items,
            "Return status retrieved successfully"
        );
    }

    private string GetStatusDescription(ReturnStatus status)
    {
        return status switch
        {
            ReturnStatus.Requested => "Return request submitted, awaiting approval",
            ReturnStatus.Approved => "Return approved, please ship the items back",
            ReturnStatus.Received => "Items received, undergoing inspection",
            ReturnStatus.Accepted => "Return accepted, refund is being processed",
            ReturnStatus.Completed => "Return completed, refund issued",
            ReturnStatus.Rejected => "Return request rejected",
            ReturnStatus.Cancelled => "Return cancelled",
            _ => "Unknown status"
        };
    }
}

