using System.Text.Json;
using Confeccao.Api.Infrastructure;
using Confeccao.Domain.Entities;

namespace Confeccao.Api.Common.Events;

/// <summary>
/// Single funnel for recording pipeline-relevant events. Currently just appends to
/// the events table; future work plugs SignalR/notification handlers in here without
/// any call-site changes.
///
/// Caller is responsible for the surrounding SaveChanges/transaction — this just
/// adds the event entity to the change tracker.
/// </summary>
public class PipelineEventLog
{
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);

    private readonly ConfeccaoDbContext _db;

    public PipelineEventLog(ConfeccaoDbContext db) => _db = db;

    public void Record(
        string eventType,
        object payload,
        Guid? orderId = null,
        Guid? pipelineItemId = null,
        Guid? userId = null)
    {
        _db.PipelineEvents.Add(new PipelineEvent
        {
            Id = Guid.NewGuid(),
            EventType = eventType,
            PayloadJson = JsonSerializer.Serialize(payload, Json),
            OrderId = orderId,
            PipelineItemId = pipelineItemId,
            UserId = userId,
        });
    }
}
