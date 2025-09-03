using Domain.Common;
using Domain.Exceptions;

namespace Domain.Entities;

public class OutboxEvent : BaseEntity
{
    public string EventType { get; private set; } = string.Empty;
    public string EventData { get; private set; } = string.Empty;
    public DateTime CreatedAt { get; private set; } = DateTime.UtcNow;
    public bool Processed { get; private set; }
    public DateTime? ProcessedAt { get; private set; }
    public int RetryCount { get; private set; }
    public int MaxRetries { get; private set; } = 5;
    public DateTime? NextRetryAt { get; private set; }
    public string? Error { get; private set; }
    public string? ProcessedBy { get; private set; }

    private OutboxEvent() { }

    public static OutboxEvent Create(object domainEvent, int maxRetries = 5)
    {
        if (domainEvent == null) throw new DomainException("Domain event required");

        var eventType = domainEvent.GetType().FullName ?? domainEvent.GetType().Name;
        var eventData = System.Text.Json.JsonSerializer.Serialize(domainEvent);

        return new OutboxEvent
        {
            EventType = eventType,
            EventData = eventData,
            MaxRetries = maxRetries,
            NextRetryAt = DateTime.UtcNow
        };
    }

    public void MarkAsProcessed(string processedBy = "System")
    {
        if (Processed) return;

        Processed = true;
        ProcessedAt = DateTime.UtcNow;
        ProcessedBy = processedBy;
        Error = null;
    }

    public void MarkAsFailed(string error)
    {
        if (string.IsNullOrWhiteSpace(error)) throw new DomainException("Error message required");

        RetryCount++;
        Error = error.Length > 1000 ? error.Substring(0, 1000) : error;

        if (RetryCount >= MaxRetries)
        {
            NextRetryAt = null;
        }
        else
        {
            var delaySeconds = Math.Pow(2, RetryCount);
            NextRetryAt = DateTime.UtcNow.AddSeconds(delaySeconds);
        }
    }

    public bool ShouldRetry()
    {
        return !Processed && 
               RetryCount < MaxRetries && 
               NextRetryAt.HasValue && 
               DateTime.UtcNow >= NextRetryAt.Value;
    }

    public bool HasFinallyFailed()
    {
        return !Processed && RetryCount >= MaxRetries;
    }

    public T? DeserializeEvent<T>() where T : class
    {
        try
        {
            return System.Text.Json.JsonSerializer.Deserialize<T>(EventData);
        }
        catch
        {
            return null;
        }
    }

    public object? DeserializeEvent()
    {
        try
        {
            var type = Type.GetType(EventType);
            if (type == null) return null;

            return System.Text.Json.JsonSerializer.Deserialize(EventData, type);
        }
        catch
        {
            return null;
        }
    }
}
