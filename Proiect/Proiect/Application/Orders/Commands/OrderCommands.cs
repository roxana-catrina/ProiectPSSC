// ═══════════════════════════════════════════════════════════════════════════════
// 📋 COMMANDS - ORDER MANAGEMENT CONTEXT
// ═══════════════════════════════════════════════════════════════════════════════

namespace OrderManagement.Application.Orders.Commands;

using MediatR;

/// <summary>
/// Base interface pentru toate comenzile
/// </summary>
public interface ICommand<out TResponse> : IRequest<TResponse>
{
}

// ═══════════════════════════════════════════════════════════════════════════════
// COMMAND DTOs
// ═══════════════════════════════════════════════════════════════════════════════

public record OrderItemDto(
    Guid ProductId,
    string ProductName,
    int Quantity,
    decimal UnitPrice
);

public record ShippingAddressDto(
    string Street,
    string City,
    string County,
    string PostalCode,
    string Country = "Romania"
);

public record CustomerInfoDto(
    string Name,
    string Email,
    string PhoneNumber
);

// ═══════════════════════════════════════════════════════════════════════════════
// 1️⃣ PlaceOrderCommand - Comandă pentru plasarea unei comenzi noi
// ═══════════════════════════════════════════════════════════════════════════════

public record PlaceOrderCommand(
    Guid CustomerId,
    CustomerInfoDto CustomerInfo,
    List<OrderItemDto> OrderItems,
    ShippingAddressDto ShippingAddress,
    string PaymentMethod
) : ICommand<PlaceOrderResult>;

public record PlaceOrderResult(
    bool Success,
    Guid? OrderId,
    string Message,
    List<string> ValidationErrors
);

/// <summary>
/// Validator pentru PlaceOrderCommand
/// </summary>
public class PlaceOrderCommandValidator
{
    public List<string> Validate(PlaceOrderCommand command)
    {
        var errors = new List<string>();

        // Validare CustomerId
        if (command.CustomerId == Guid.Empty)
            errors.Add("CustomerId is required");

        // Validare CustomerInfo
        if (command.CustomerInfo == null)
            errors.Add("CustomerInfo is required");
        else
        {
            if (string.IsNullOrWhiteSpace(command.CustomerInfo.Name))
                errors.Add("Customer name is required");
            if (string.IsNullOrWhiteSpace(command.CustomerInfo.Email))
                errors.Add("Customer email is required");
            if (string.IsNullOrWhiteSpace(command.CustomerInfo.PhoneNumber))
                errors.Add("Customer phone number is required");
        }

        // Validare OrderItems
        if (command.OrderItems == null || !command.OrderItems.Any())
            errors.Add("Order must have at least one item");
        else
        {
            for (int i = 0; i < command.OrderItems.Count; i++)
            {
                var item = command.OrderItems[i];
                if (item.ProductId == Guid.Empty)
                    errors.Add($"Item {i + 1}: ProductId is required");
                if (item.Quantity <= 0)
                    errors.Add($"Item {i + 1}: Quantity must be positive");
                if (item.UnitPrice <= 0)
                    errors.Add($"Item {i + 1}: UnitPrice must be positive");
            }
        }

        // Validare ShippingAddress
        if (command.ShippingAddress == null)
            errors.Add("ShippingAddress is required");
        else
        {
            if (string.IsNullOrWhiteSpace(command.ShippingAddress.Street))
                errors.Add("Street is required");
            if (string.IsNullOrWhiteSpace(command.ShippingAddress.City))
                errors.Add("City is required");
            if (string.IsNullOrWhiteSpace(command.ShippingAddress.County))
                errors.Add("County is required");
            if (string.IsNullOrWhiteSpace(command.ShippingAddress.PostalCode))
                errors.Add("PostalCode is required");
        }

        // Validare PaymentMethod
        if (string.IsNullOrWhiteSpace(command.PaymentMethod))
            errors.Add("PaymentMethod is required");

        return errors;
    }
}

// ═══════════════════════════════════════════════════════════════════════════════
// 2️⃣ ValidateOrderCommand - Comandă pentru validarea unei comenzi
// ═══════════════════════════════════════════════════════════════════════════════

public record ValidateOrderCommand(
    Guid OrderId
) : ICommand<ValidateOrderResult>;

public record ValidateOrderResult(
    bool Success,
    bool IsValid,
    string Message,
    List<string> ValidationErrors
);

/// <summary>
/// Validator pentru ValidateOrderCommand
/// </summary>
public class ValidateOrderCommandValidator
{
    public List<string> Validate(ValidateOrderCommand command)
    {
        var errors = new List<string>();

        if (command.OrderId == Guid.Empty)
            errors.Add("OrderId is required");

        return errors;
    }
}

// ═══════════════════════════════════════════════════════════════════════════════
// 3️⃣ ConfirmOrderCommand - Comandă pentru confirmarea unei comenzi
// ═══════════════════════════════════════════════════════════════════════════════

public record ConfirmOrderCommand(
    Guid OrderId,
    Guid ConfirmedBy,
    DateTime EstimatedDeliveryDate
) : ICommand<ConfirmOrderResult>;

public record ConfirmOrderResult(
    bool Success,
    string Message,
    List<string> ValidationErrors
);

/// <summary>
/// Validator pentru ConfirmOrderCommand
/// </summary>
public class ConfirmOrderCommandValidator
{
    public List<string> Validate(ConfirmOrderCommand command)
    {
        var errors = new List<string>();

        if (command.OrderId == Guid.Empty)
            errors.Add("OrderId is required");

        if (command.ConfirmedBy == Guid.Empty)
            errors.Add("ConfirmedBy is required");

        if (command.EstimatedDeliveryDate <= DateTime.UtcNow)
            errors.Add("EstimatedDeliveryDate must be in the future");

        return errors;
    }
}

// ═══════════════════════════════════════════════════════════════════════════════
// 4️⃣ RequestCancellationCommand - Comandă pentru solicitarea anulării
// ═══════════════════════════════════════════════════════════════════════════════

public record RequestCancellationCommand(
    Guid OrderId,
    string Reason,
    Guid RequestedBy
) : ICommand<RequestCancellationResult>;

public record RequestCancellationResult(
    bool Success,
    string Message,
    List<string> ValidationErrors
);

/// <summary>
/// Validator pentru RequestCancellationCommand
/// </summary>
public class RequestCancellationCommandValidator
{
    public List<string> Validate(RequestCancellationCommand command)
    {
        var errors = new List<string>();

        if (command.OrderId == Guid.Empty)
            errors.Add("OrderId is required");

        if (string.IsNullOrWhiteSpace(command.Reason))
            errors.Add("Cancellation reason is required");

        if (command.RequestedBy == Guid.Empty)
            errors.Add("RequestedBy is required");

        return errors;
    }
}

// ═══════════════════════════════════════════════════════════════════════════════
// 5️⃣ CancelOrderCommand - Comandă pentru anularea efectivă
// ═══════════════════════════════════════════════════════════════════════════════

public record CancelOrderCommand(
    Guid OrderId,
    string Reason,
    Guid CancelledBy
) : ICommand<CancelOrderResult>;

public record CancelOrderResult(
    bool Success,
    string Message,
    List<string> ValidationErrors
);

/// <summary>
/// Validator pentru CancelOrderCommand
/// </summary>
public class CancelOrderCommandValidator
{
    public List<string> Validate(CancelOrderCommand command)
    {
        var errors = new List<string>();

        if (command.OrderId == Guid.Empty)
            errors.Add("OrderId is required");

        if (string.IsNullOrWhiteSpace(command.Reason))
            errors.Add("Cancellation reason is required");

        if (command.CancelledBy == Guid.Empty)
            errors.Add("CancelledBy is required");

        return errors;
    }
}

// ═══════════════════════════════════════════════════════════════════════════════
// 6️⃣ RequestModificationCommand - Comandă pentru solicitarea modificării
// ═══════════════════════════════════════════════════════════════════════════════

public record RequestModificationCommand(
    Guid OrderId,
    string Description,
    Guid RequestedBy,
    Dictionary<string, object> Changes
) : ICommand<RequestModificationResult>;

public record RequestModificationResult(
    bool Success,
    string Message,
    List<string> ValidationErrors
);

/// <summary>
/// Validator pentru RequestModificationCommand
/// </summary>
public class RequestModificationCommandValidator
{
    public List<string> Validate(RequestModificationCommand command)
    {
        var errors = new List<string>();

        if (command.OrderId == Guid.Empty)
            errors.Add("OrderId is required");

        if (string.IsNullOrWhiteSpace(command.Description))
            errors.Add("Modification description is required");

        if (command.RequestedBy == Guid.Empty)
            errors.Add("RequestedBy is required");

        if (command.Changes == null || !command.Changes.Any())
            errors.Add("Changes are required");

        return errors;
    }
}

// ═══════════════════════════════════════════════════════════════════════════════
// 7️⃣ ModifyOrderCommand - Comandă pentru modificarea efectivă
// ═══════════════════════════════════════════════════════════════════════════════

public record ModifyOrderCommand(
    Guid OrderId,
    List<OrderItemDto> NewOrderItems = null,
    ShippingAddressDto NewShippingAddress = null,
    string NewPaymentMethod = null
) : ICommand<ModifyOrderResult>;

public record ModifyOrderResult(
    bool Success,
    string Message,
    List<string> ValidationErrors
);

/// <summary>
/// Validator pentru ModifyOrderCommand
/// </summary>
public class ModifyOrderCommandValidator
{
    public List<string> Validate(ModifyOrderCommand command)
    {
        var errors = new List<string>();

        if (command.OrderId == Guid.Empty)
            errors.Add("OrderId is required");

        // Cel puțin o modificare trebuie specificată
        if (command.NewOrderItems == null 
            && command.NewShippingAddress == null 
            && string.IsNullOrWhiteSpace(command.NewPaymentMethod))
        {
            errors.Add("At least one field must be modified");
        }

        // Validare NewOrderItems dacă sunt specificate
        if (command.NewOrderItems != null)
        {
            if (!command.NewOrderItems.Any())
                errors.Add("Order must have at least one item");

            for (int i = 0; i < command.NewOrderItems.Count; i++)
            {
                var item = command.NewOrderItems[i];
                if (item.ProductId == Guid.Empty)
                    errors.Add($"Item {i + 1}: ProductId is required");
                if (item.Quantity <= 0)
                    errors.Add($"Item {i + 1}: Quantity must be positive");
                if (item.UnitPrice <= 0)
                    errors.Add($"Item {i + 1}: UnitPrice must be positive");
            }
        }

        // Validare NewShippingAddress dacă este specificată
        if (command.NewShippingAddress != null)
        {
            if (string.IsNullOrWhiteSpace(command.NewShippingAddress.Street))
                errors.Add("Street is required");
            if (string.IsNullOrWhiteSpace(command.NewShippingAddress.City))
                errors.Add("City is required");
            if (string.IsNullOrWhiteSpace(command.NewShippingAddress.County))
                errors.Add("County is required");
        }

        return errors;
    }
}

