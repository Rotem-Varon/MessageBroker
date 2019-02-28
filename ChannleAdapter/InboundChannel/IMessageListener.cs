using System;
using System.Threading.Tasks;

namespace MessageBroker.Messaging.Inbound
{
    public interface IMessageListener
    {
        void ListenAsync<T>(Func<T, Task> onMessageReceived);
        void TerminateAsync();
    }
}