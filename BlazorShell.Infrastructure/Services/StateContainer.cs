using System;
using System.Collections.Generic;
using System.Threading;
using System.Collections.Concurrent;
using BlazorShell.Application.Interfaces;

namespace BlazorShell.Infrastructure.Services;

public class StateContainer : IStateContainer
{
    private readonly ConcurrentDictionary<string, object> _state = new();
    private readonly List<WeakReference<EventHandler<StateChangedEventArgs>>> _handlers = new();

    public T? GetState<T>(string key) where T : class
    {
        return _state.TryGetValue(key, out var value) ? value as T : null;
    }

    public void SetState<T>(string key, T value) where T : class
    {
        _state[key] = value!;
        Publish(new StateChangedEventArgs(key, null, value));
    }

    public bool RemoveState(string key)
    {
        if (_state.TryRemove(key, out var oldValue))
        {
            Publish(new StateChangedEventArgs(key, oldValue, null));
            return true;
        }
        return false;
    }

    public void ClearState()
    {
        _state.Clear();
        Publish(new StateChangedEventArgs(null, null, null));
    }

    public IDisposable Subscribe(EventHandler<StateChangedEventArgs> handler)
    {
        lock (_handlers)
        {
            _handlers.Add(new WeakReference<EventHandler<StateChangedEventArgs>>(handler));
        }
        return new Subscription(this, handler);
    }

    private void Publish(StateChangedEventArgs args)
    {
        List<WeakReference<EventHandler<StateChangedEventArgs>>> handlers;
        lock (_handlers)
        {
            handlers = _handlers.ToList();
        }
        foreach (var weak in handlers)
        {
            if (weak.TryGetTarget(out var h))
            {
                h(this, args);
            }
        }
    }

    private void Unsubscribe(EventHandler<StateChangedEventArgs> handler)
    {
        lock (_handlers)
        {
            _handlers.RemoveAll(w => !w.TryGetTarget(out var h) || h == handler);
        }
    }

    private sealed class Subscription : IDisposable
    {
        private readonly StateContainer _parent;
        private EventHandler<StateChangedEventArgs>? _handler;
        public Subscription(StateContainer parent, EventHandler<StateChangedEventArgs> handler)
        {
            _parent = parent;
            _handler = handler;
        }
        public void Dispose()
        {
            var h = Interlocked.Exchange(ref _handler, null);
            if (h != null)
            {
                _parent.Unsubscribe(h);
            }
        }
    }
}
