using TicketBooking.Domain.Commerce;
using TicketBooking.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace TicketBooking.Api.Services.Commerce;

public sealed class CustomerNotificationService
{
    private readonly AppDbContext _db;

    public CustomerNotificationService(AppDbContext db)
    {
        _db = db;
    }

    public async Task<CustomerNotification?> CreateAsync(
        Guid userId,
        Guid? tenantId,
        string category,
        string title,
        string body,
        string? actionUrl = null,
        string? referenceType = null,
        Guid? referenceId = null,
        string? metadataJson = null,
        CancellationToken ct = default)
    {
        var notification = new CustomerNotification
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            TenantId = tenantId,
            Category = category,
            Title = title,
            Body = body,
            ActionUrl = actionUrl,
            ReferenceType = referenceType,
            ReferenceId = referenceId,
            MetadataJson = metadataJson,
            Status = CustomerNotificationStatus.Unread,
            CreatedAt = DateTimeOffset.UtcNow,
        };

        _db.CustomerNotifications.Add(notification);
        await _db.SaveChangesAsync(ct);
        return notification;
    }
}
