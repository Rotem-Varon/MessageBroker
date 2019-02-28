using System.Threading.Tasks;

namespace MessageBroker.Messaging.Outbound
{
    public interface IMessageSender
    {
        Task SendAsync(TypedMessage TypedMessage);
        Task TerminateAsync();
    }
}