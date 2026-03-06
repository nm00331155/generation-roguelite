using System.Collections.Generic;

namespace GenerationRoguelite.Events;

public sealed class EventCache
{
    private readonly Queue<EventData> _queue = new();

    public int TargetSize { get; set; } = 24;

    public int Count => _queue.Count;

    public void Enqueue(EventData eventData)
    {
        _queue.Enqueue(eventData);
        while (_queue.Count > TargetSize)
        {
            _queue.Dequeue();
        }
    }

    public bool TryDequeue(out EventData eventData)
    {
        if (_queue.Count == 0)
        {
            eventData = null!;
            return false;
        }

        eventData = _queue.Dequeue();
        return true;
    }

    public void Clear()
    {
        _queue.Clear();
    }
}
