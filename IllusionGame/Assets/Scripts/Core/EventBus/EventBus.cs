using System.Collections.Generic;
using UnityEngine;

public static class EventBus<T> where T : struct {
    private static readonly List<IEventListener<T>> EventListeners = new();
    
    public static void Subscribe(IEventListener<T> eventListener) {
        if (!EventListeners.Contains(eventListener)) {
            EventListeners.Add(eventListener);
        }
    }

    public static void Unsubscribe(IEventListener<T> eventListener) {
        if (EventListeners.Contains(eventListener)) {
            EventListeners.Remove(eventListener);
        }
    }

    public static void Raise(T eventData) {
        foreach (var listener in EventListeners) {
            listener.OnEventRaised(eventData);
        }
    }
    
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void Reset() {
        EventListeners.Clear();
    }
}
