using System.Collections.Concurrent;

namespace TeamsFileNotifier.Global
{
    public class MessageBroker
    {
        // Key: message type, Value: list of handlers
        private readonly ConcurrentDictionary<Type, List<Delegate>> _subscribers = new();

        // Subscribe to a message type T
        public void Subscribe<T>(Action<T> handler)
        {
            var handlers = _subscribers.GetOrAdd(typeof(T), _ => new List<Delegate>());
            lock (handlers)
            {
                handlers.Add(handler);
            }
        }

        // Unsubscribe
        public void Unsubscribe<T>(Action<T> handler)
        {
            if (_subscribers.TryGetValue(typeof(T), out var handlers))
            {
                lock (handlers)
                {
                    handlers.Remove(handler);
                }
            }
        }

        // Publish a message of type T
        public void Publish<T>(T message)
        {
            if (_subscribers.TryGetValue(typeof(T), out var handlersCopy))
            {
                List<Delegate> handlersSnapshot;
                lock (handlersCopy)
                {
                    handlersSnapshot = new List<Delegate>(handlersCopy);
                }

                foreach (var handler in handlersSnapshot)
                {
                    if (handler is Action<T> action)
                    {
                        action(message);
                    }
                }
            }
        }
    }
}
