// ═══════════════════════════════════════════════════════════════════════════════
// 📦 BOUNDED CONTEXT: NOTIFICATION MANAGEMENT - DOMAIN EVENTS
// ═══════════════════════════════════════════════════════════════════════════════
// Implementare DDD în C# pentru evenimentele de domeniu
// Data: November 7, 2025
// ═══════════════════════════════════════════════════════════════════════════════

namespace NotificationManagement.Domain.Notifications.Events;

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
// 1️⃣ CustomerNotified - Eveniment când clientul este notificat cu succes
// ═══════════════════════════════════════════════════════════════════════════════

/// <summary>
/// Eveniment declanșat când o notificare a fost trimisă cu succes către client
/// Comandă care declanșează: SendNotification / ResendNotification
/// </summary>
public record CustomerNotified : DomainEvent
{
    public Guid NotificationId { get; init; }
    public Guid CustomerId { get; init; }
    public string CustomerName { get; init; }
    public string CustomerEmail { get; init; }
    public string NotificationType { get; init; }
    public string Channel { get; init; } // Email, SMS, Push, InApp
    public string Subject { get; init; }
    public DateTime SentAt { get; init; }
    public DateTime? DeliveredAt { get; init; }
    public bool IsDelivered { get; init; }
    public string DeliveryStatus { get; init; } // Sent, Delivered, Opened, Clicked
    public string TrackingId { get; init; }
    public Dictionary<string, string> Metadata { get; init; }

    public CustomerNotified(
        Guid notificationId,
        Guid customerId,
        string customerName,
        string customerEmail,
        string notificationType,
        string channel,
        string subject,
        bool isDelivered,
        string deliveryStatus,
        string trackingId,
        Dictionary<string, string>? metadata = null)
    {
        NotificationId = notificationId;
        CustomerId = customerId;
        CustomerName = customerName;
        CustomerEmail = customerEmail;
        NotificationType = notificationType;
        Channel = channel;
        Subject = subject;
        SentAt = DateTime.UtcNow;
        DeliveredAt = isDelivered ? DateTime.UtcNow : null;
        IsDelivered = isDelivered;
        DeliveryStatus = deliveryStatus;
        TrackingId = trackingId;
        Metadata = metadata ?? new Dictionary<string, string>();
    }
}

// ═══════════════════════════════════════════════════════════════════════════════
// 2️⃣ NotificationScheduled - Eveniment când notificarea este programată
// ═══════════════════════════════════════════════════════════════════════════════

/// <summary>
/// Eveniment declanșat când o notificare este programată pentru trimitere ulterioară
/// Comandă care declanșează: ScheduleNotification
/// </summary>
public record NotificationScheduled : DomainEvent
{
    public Guid NotificationId { get; init; }
    public Guid CustomerId { get; init; }
    public string NotificationType { get; init; }
    public string Channel { get; init; }
    public DateTime ScheduledFor { get; init; }
    public string Subject { get; init; }
    public string Priority { get; init; } // Low, Normal, High, Urgent
    public bool CanBeCancelled { get; init; }

    public NotificationScheduled(
        Guid notificationId,
        Guid customerId,
        string notificationType,
        string channel,
        DateTime scheduledFor,
        string subject,
        string priority,
        bool canBeCancelled = true)
    {
        NotificationId = notificationId;
        CustomerId = customerId;
        NotificationType = notificationType;
        Channel = channel;
        ScheduledFor = scheduledFor;
        Subject = subject;
        Priority = priority;
        CanBeCancelled = canBeCancelled;
    }
}

// ═══════════════════════════════════════════════════════════════════════════════
// 3️⃣ NotificationFailed - Eveniment când notificarea eșuează
// ═══════════════════════════════════════════════════════════════════════════════

/// <summary>
/// Eveniment declanșat când trimiterea unei notificări eșuează
/// </summary>
public record NotificationFailed : DomainEvent
{
    public Guid NotificationId { get; init; }
    public Guid CustomerId { get; init; }
    public string Channel { get; init; }
    public string FailureReason { get; init; }
    public string ErrorCode { get; init; }
    public string ErrorMessage { get; init; }
    public int AttemptNumber { get; init; }
    public int MaxAttempts { get; init; }
    public bool WillRetry { get; init; }
    public DateTime? NextRetryAt { get; init; }
    public DateTime FailedAt { get; init; }

    public NotificationFailed(
        Guid notificationId,
        Guid customerId,
        string channel,
        string failureReason,
        string errorCode,
        string errorMessage,
        int attemptNumber,
        int maxAttempts,
        bool willRetry,
        DateTime? nextRetryAt)
    {
        NotificationId = notificationId;
        CustomerId = customerId;
        Channel = channel;
        FailureReason = failureReason;
        ErrorCode = errorCode;
        ErrorMessage = errorMessage;
        AttemptNumber = attemptNumber;
        MaxAttempts = maxAttempts;
        WillRetry = willRetry;
        NextRetryAt = nextRetryAt;
        FailedAt = DateTime.UtcNow;
    }
}

// ═══════════════════════════════════════════════════════════════════════════════
// 4️⃣ NotificationCancelled - Eveniment când notificarea este anulată
// ═══════════════════════════════════════════════════════════════════════════════

/// <summary>
/// Eveniment declanșat când o notificare programată este anulată
/// Comandă care declanșează: CancelNotification
/// </summary>
public record NotificationCancelled : DomainEvent
{
    public Guid NotificationId { get; init; }
    public Guid CustomerId { get; init; }
    public Guid CancelledBy { get; init; }
    public string CancelledByName { get; init; }
    public string CancellationReason { get; init; }
    public DateTime WasScheduledFor { get; init; }
    public DateTime CancelledAt { get; init; }

    public NotificationCancelled(
        Guid notificationId,
        Guid customerId,
        Guid cancelledBy,
        string cancelledByName,
        string cancellationReason,
        DateTime wasScheduledFor)
    {
        NotificationId = notificationId;
        CustomerId = customerId;
        CancelledBy = cancelledBy;
        CancelledByName = cancelledByName;
        CancellationReason = cancellationReason;
        WasScheduledFor = wasScheduledFor;
        CancelledAt = DateTime.UtcNow;
    }
}

// ═══════════════════════════════════════════════════════════════════════════════
// 5️⃣ NotificationRead - Eveniment când notificarea este citită
// ═══════════════════════════════════════════════════════════════════════════════

/// <summary>
/// Eveniment declanșat când clientul citește notificarea (pentru in-app notifications)
/// Comandă care declanșează: MarkAsRead
/// </summary>
public record NotificationRead : DomainEvent
{
    public Guid NotificationId { get; init; }
    public Guid CustomerId { get; init; }
    public DateTime ReadAt { get; init; }
    public string ReadFrom { get; init; } // Web, Mobile, Email
    public string IpAddress { get; init; }
    public string UserAgent { get; init; }

    public NotificationRead(
        Guid notificationId,
        Guid customerId,
        string readFrom,
        string ipAddress,
        string userAgent)
    {
        NotificationId = notificationId;
        CustomerId = customerId;
        ReadAt = DateTime.UtcNow;
        ReadFrom = readFrom;
        IpAddress = ipAddress;
        UserAgent = userAgent;
    }
}

// ═══════════════════════════════════════════════════════════════════════════════
// 6️⃣ NotificationDelivered - Eveniment când notificarea este livrată (BONUS)
// ═══════════════════════════════════════════════════════════════════════════════

/// <summary>
/// Eveniment declanșat când se primește confirmare de livrare de la provider
/// (ex: webhook de la SendGrid, Twilio)
/// </summary>
public record NotificationDelivered : DomainEvent
{
    public Guid NotificationId { get; init; }
    public Guid CustomerId { get; init; }
    public string Channel { get; init; }
    public DateTime DeliveredAt { get; init; }
    public string ProviderMessageId { get; init; }
    public string ProviderStatus { get; init; }
    public Dictionary<string, string> ProviderMetadata { get; init; }

    public NotificationDelivered(
        Guid notificationId,
        Guid customerId,
        string channel,
        string providerMessageId,
        string providerStatus,
        Dictionary<string, string>? providerMetadata = null)
    {
        NotificationId = notificationId;
        CustomerId = customerId;
        Channel = channel;
        DeliveredAt = DateTime.UtcNow;
        ProviderMessageId = providerMessageId;
        ProviderStatus = providerStatus;
        ProviderMetadata = providerMetadata ?? new Dictionary<string, string>();
    }
}

// ═══════════════════════════════════════════════════════════════════════════════
// 7️⃣ NotificationOpened - Eveniment când email-ul este deschis (BONUS)
// ═══════════════════════════════════════════════════════════════════════════════

/// <summary>
/// Eveniment declanșat când clientul deschide email-ul (tracking pixel)
/// </summary>
public record NotificationOpened : DomainEvent
{
    public Guid NotificationId { get; init; }
    public Guid CustomerId { get; init; }
    public DateTime OpenedAt { get; init; }
    public string IpAddress { get; init; }
    public string UserAgent { get; init; }
    public string DeviceType { get; init; } // Desktop, Mobile, Tablet
    public string EmailClient { get; init; } // Gmail, Outlook, Apple Mail
    public int OpenCount { get; init; } // Câte ori a fost deschis

    public NotificationOpened(
        Guid notificationId,
        Guid customerId,
        string ipAddress,
        string userAgent,
        string deviceType,
        string emailClient,
        int openCount = 1)
    {
        NotificationId = notificationId;
        CustomerId = customerId;
        OpenedAt = DateTime.UtcNow;
        IpAddress = ipAddress;
        UserAgent = userAgent;
        DeviceType = deviceType;
        EmailClient = emailClient;
        OpenCount = openCount;
    }
}

// ═══════════════════════════════════════════════════════════════════════════════
// 8️⃣ NotificationClicked - Eveniment când se dă click pe un link (BONUS)
// ═══════════════════════════════════════════════════════════════════════════════

/// <summary>
/// Eveniment declanșat când clientul dă click pe un link din notificare
/// </summary>
public record NotificationClicked : DomainEvent
{
    public Guid NotificationId { get; init; }
    public Guid CustomerId { get; init; }
    public DateTime ClickedAt { get; init; }
    public string ClickedUrl { get; init; }
    public string LinkLabel { get; init; }
    public string IpAddress { get; init; }
    public string UserAgent { get; init; }

    public NotificationClicked(
        Guid notificationId,
        Guid customerId,
        string clickedUrl,
        string linkLabel,
        string ipAddress,
        string userAgent)
    {
        NotificationId = notificationId;
        CustomerId = customerId;
        ClickedAt = DateTime.UtcNow;
        ClickedUrl = clickedUrl;
        LinkLabel = linkLabel;
        IpAddress = ipAddress;
        UserAgent = userAgent;
    }
}

// ═══════════════════════════════════════════════════════════════════════════════
// 9️⃣ NotificationBounced - Eveniment când email-ul bounce-ează (BONUS)
// ═══════════════════════════════════════════════════════════════════════════════

/// <summary>
/// Eveniment declanșat când un email returnează (hard bounce sau soft bounce)
/// </summary>
public record NotificationBounced : DomainEvent
{
    public Guid NotificationId { get; init; }
    public Guid CustomerId { get; init; }
    public string CustomerEmail { get; init; }
    public DateTime BouncedAt { get; init; }
    public string BounceType { get; init; } // HardBounce, SoftBounce, Spam
    public string BounceReason { get; init; }
    public bool ShouldBlockFutureEmails { get; init; }

    public NotificationBounced(
        Guid notificationId,
        Guid customerId,
        string customerEmail,
        string bounceType,
        string bounceReason,
        bool shouldBlockFutureEmails)
    {
        NotificationId = notificationId;
        CustomerId = customerId;
        CustomerEmail = customerEmail;
        BouncedAt = DateTime.UtcNow;
        BounceType = bounceType;
        BounceReason = bounceReason;
        ShouldBlockFutureEmails = shouldBlockFutureEmails;
    }
}

