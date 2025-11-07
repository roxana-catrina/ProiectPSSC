// ═══════════════════════════════════════════════════════════════════════════════
// 🌐 API CONTROLLER - RETURNS MANAGEMENT
// ═══════════════════════════════════════════════════════════════════════════════
// REST API endpoints pentru bounded context-ul Returns
// Data: November 7, 2025
// ═══════════════════════════════════════════════════════════════════════════════

using MediatR;
using Microsoft.AspNetCore.Mvc;
using ReturnsManagement.Application.Returns.Commands;
using ReturnsManagement.Domain.Returns;

namespace ReturnsManagement.Controllers;

[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public class ReturnsController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly ILogger<ReturnsController> _logger;

    public ReturnsController(IMediator mediator, ILogger<ReturnsController> logger)
    {
        _mediator = mediator;
        _logger = logger;
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // 1️⃣ POST /api/returns/request - Solicitare retur
    // ═══════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Creează o cerere de retur pentru o comandă
    /// </summary>
    /// <remarks>
    /// Exemplu request:
    /// 
    ///     POST /api/returns/request
    ///     {
    ///         "orderId": "11111111-1111-1111-1111-111111111111",
    ///         "customerId": "22222222-2222-2222-2222-222222222222",
    ///         "customerName": "Ion Popescu",
    ///         "customerEmail": "ion.popescu@example.com",
    ///         "items": [
    ///             {
    ///                 "productId": "aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa",
    ///                 "productName": "Laptop Dell XPS 15",
    ///                 "quantity": 1,
    ///                 "unitPrice": 299.99,
    ///                 "productCategory": "electronics"
    ///             }
    ///         ],
    ///         "reason": "DefectiveProduct",
    ///         "detailedDescription": "The laptop does not power on",
    ///         "orderDeliveryDate": "2025-11-02T00:00:00Z",
    ///         "productCategory": "electronics"
    ///     }
    /// </remarks>
    [HttpPost("request")]
    [ProducesResponseType(typeof(RequestReturnResult), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<RequestReturnResult>> RequestReturn(
        [FromBody] RequestReturnCommand command)
    {
        _logger.LogInformation("Processing return request for order {OrderId}", command.OrderId);

        var result = await _mediator.Send(command);

        if (!result.Success)
        {
            _logger.LogWarning("Return request failed: {Message}", result.Message);
            return BadRequest(result);
        }

        _logger.LogInformation("Return request created: {ReturnId}, RMA: {RmaCode}", 
            result.ReturnId, result.RmaCode);

        return CreatedAtAction(
            nameof(GetReturnStatus),
            new { returnId = result.ReturnId },
            result
        );
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // 2️⃣ POST /api/returns/{returnId}/approve - Aprobare retur
    // ═══════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Aprobă o cerere de retur (necesită rol de Manager/Supervisor)
    /// </summary>
    /// <remarks>
    /// Exemplu request:
    /// 
    ///     POST /api/returns/{returnId}/approve
    ///     {
    ///         "approvedBy": "99999999-9999-9999-9999-999999999999",
    ///         "approverName": "Manager George",
    ///         "approvalNotes": "Approved - defective product confirmed",
    ///         "applyRestockingFee": false,
    ///         "approverRole": "Manager"
    ///     }
    /// </remarks>
    [HttpPost("{returnId}/approve")]
    [ProducesResponseType(typeof(ApproveReturnResult), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<ApproveReturnResult>> ApproveReturn(
        Guid returnId,
        [FromBody] ApproveReturnRequest request)
    {
        _logger.LogInformation("Processing approval for return {ReturnId} by {ApproverName}", 
            returnId, request.ApproverName);

        var command = new ApproveReturnCommand(
            returnId,
            request.ApprovedBy,
            request.ApproverName,
            request.ApprovalNotes,
            request.ApplyRestockingFee,
            request.ApproverRole
        );

        var result = await _mediator.Send(command);

        if (!result.Success)
        {
            _logger.LogWarning("Return approval failed: {Message}", result.Message);
            
            if (result.ValidationErrors.Any(e => e.Contains("not found")))
                return NotFound(result);
            
            if (result.ValidationErrors.Any(e => e.Contains("authorization")))
                return StatusCode(StatusCodes.Status403Forbidden, result);
            
            return BadRequest(result);
        }

        _logger.LogInformation("Return {ReturnId} approved. Refund amount: {RefundAmount}", 
            returnId, result.ApprovedAmount);

        return Ok(result);
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // 3️⃣ POST /api/returns/{returnId}/receive - Primire produse returnate
    // ═══════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Marchează produsele returnate ca fiind primite în depozit
    /// </summary>
    /// <remarks>
    /// Exemplu request:
    /// 
    ///     POST /api/returns/{returnId}/receive
    ///     {
    ///         "receivedBy": "88888888-8888-8888-8888-888888888888",
    ///         "receiverName": "Warehouse Staff John",
    ///         "receivedItems": [
    ///             {
    ///                 "productId": "aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa",
    ///                 "quantityReceived": 1,
    ///                 "condition": "Intact",
    ///                 "conditionNotes": "Product in original packaging, unused",
    ///                 "acceptableForResale": true
    ///             }
    ///         ],
    ///         "trackingNumber": "TRACK123456",
    ///         "warehouseLocation": "WAREHOUSE-A-SHELF-15",
    ///         "inspectionNotes": "All items in good condition"
    ///     }
    /// </remarks>
    [HttpPost("{returnId}/receive")]
    [ProducesResponseType(typeof(ReceiveReturnResult), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<ReceiveReturnResult>> ReceiveReturn(
        Guid returnId,
        [FromBody] ReceiveReturnRequest request)
    {
        _logger.LogInformation("Processing receipt for return {ReturnId}", returnId);

        var command = new ReceiveReturnCommand(
            returnId,
            request.ReceivedBy,
            request.ReceiverName,
            request.ReceivedItems,
            request.TrackingNumber,
            request.WarehouseLocation,
            request.InspectionNotes
        );

        var result = await _mediator.Send(command);

        if (!result.Success)
        {
            _logger.LogWarning("Return receipt failed: {Message}", result.Message);
            
            if (result.ValidationErrors.Any(e => e.Contains("not found")))
                return NotFound(result);
            
            return BadRequest(result);
        }

        _logger.LogInformation("Return {ReturnId} received. Items: {ItemCount}, All received: {AllReceived}", 
            returnId, result.TotalItemsReceived, result.AllItemsReceived);

        return Ok(result);
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // 4️⃣ POST /api/returns/{returnId}/accept - Acceptare finală retur
    // ═══════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Acceptă returul final și procesează rambursarea
    /// </summary>
    /// <remarks>
    /// Exemplu request:
    /// 
    ///     POST /api/returns/{returnId}/accept
    ///     {
    ///         "acceptedBy": "77777777-7777-7777-7777-777777777777",
    ///         "accepterName": "Finance Manager Ana",
    ///         "refundMethod": "OriginalPaymentMethod",
    ///         "refundReference": "REFUND-2025-001",
    ///         "notes": "Full refund approved",
    ///         "inventoryUpdated": true
    ///     }
    /// </remarks>
    [HttpPost("{returnId}/accept")]
    [ProducesResponseType(typeof(AcceptReturnResult), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<AcceptReturnResult>> AcceptReturn(
        Guid returnId,
        [FromBody] AcceptReturnRequest request)
    {
        _logger.LogInformation("Processing acceptance for return {ReturnId}", returnId);

        var command = new AcceptReturnCommand(
            returnId,
            request.AcceptedBy,
            request.AccepterName,
            request.RefundMethod,
            request.RefundReference,
            request.Notes,
            request.InventoryUpdated
        );

        var result = await _mediator.Send(command);

        if (!result.Success)
        {
            _logger.LogWarning("Return acceptance failed: {Message}", result.Message);
            
            if (result.ValidationErrors.Any(e => e.Contains("not found")))
                return NotFound(result);
            
            return BadRequest(result);
        }

        _logger.LogInformation("Return {ReturnId} accepted. Final refund: {FinalRefund}", 
            returnId, result.FinalRefundAmount);

        return Ok(result);
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // 5️⃣ POST /api/returns/{returnId}/reject - Respingere retur
    // ═══════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Respinge o cerere de retur
    /// </summary>
    [HttpPost("{returnId}/reject")]
    [ProducesResponseType(typeof(RejectReturnResult), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<RejectReturnResult>> RejectReturn(
        Guid returnId,
        [FromBody] RejectReturnRequest request)
    {
        _logger.LogInformation("Processing rejection for return {ReturnId}", returnId);

        var command = new RejectReturnCommand(
            returnId,
            request.RejectedBy,
            request.RejectorName,
            request.RejectionReason,
            request.DetailedExplanation,
            request.NotifyCustomer
        );

        var result = await _mediator.Send(command);

        if (!result.Success)
        {
            if (result.ValidationErrors.Any(e => e.Contains("not found")))
                return NotFound(result);
            
            return BadRequest(result);
        }

        return Ok(result);
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // 6️⃣ GET /api/returns/{returnId} - Obținere status retur
    // ═══════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Obține statusul și detaliile unui retur
    /// </summary>
    [HttpGet("{returnId}")]
    [ProducesResponseType(typeof(GetReturnStatusResult), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<GetReturnStatusResult>> GetReturnStatus(
        Guid returnId,
        [FromQuery] Guid? customerId = null)
    {
        _logger.LogInformation("Getting status for return {ReturnId}", returnId);

        var command = new GetReturnStatusCommand(returnId, customerId);
        var result = await _mediator.Send(command);

        if (!result.Success)
        {
            return NotFound(result);
        }

        return Ok(result);
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // 7️⃣ GET /api/returns/rma/{rmaCode} - Căutare după RMA Code
    // ═══════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Caută un retur după RMA Code
    /// </summary>
    [HttpGet("rma/{rmaCode}")]
    [ProducesResponseType(typeof(GetReturnStatusResult), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<GetReturnStatusResult>> GetReturnByRmaCode(string rmaCode)
    {
        _logger.LogInformation("Searching for return with RMA code {RmaCode}", rmaCode);
        
        // This would need a separate query/command in a real implementation
        return NotFound(new { Message = "Feature not implemented in this example" });
    }
}

// ═══════════════════════════════════════════════════════════════════════════════
// REQUEST DTOs (pentru a nu expune comenzile direct)
// ═══════════════════════════════════════════════════════════════════════════════

public record ApproveReturnRequest(
    Guid ApprovedBy,
    string ApproverName,
    string ApprovalNotes,
    bool ApplyRestockingFee,
    string ApproverRole
);

public record ReceiveReturnRequest(
    Guid ReceivedBy,
    string ReceiverName,
    List<ReceivedItemDto> ReceivedItems,
    string TrackingNumber,
    string WarehouseLocation,
    string InspectionNotes
);

public record AcceptReturnRequest(
    Guid AcceptedBy,
    string AccepterName,
    RefundMethod RefundMethod,
    string RefundReference,
    string Notes,
    bool InventoryUpdated
);

public record RejectReturnRequest(
    Guid RejectedBy,
    string RejectorName,
    string RejectionReason,
    string DetailedExplanation,
    bool NotifyCustomer
);

