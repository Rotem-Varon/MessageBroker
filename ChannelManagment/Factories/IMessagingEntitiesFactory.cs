using System.Threading.Tasks;

namespace MessageBroker.Channel.Managment
{
    public interface IMessagingEntitiesFactory
    {
        Task DeleteChannelAsync(string channel);
        Task CreateChannelAsync(string channel);
        Task CreateOrUpdateSubscriptionAsync(string channel, string subscription, string sqlFilterQuery = null);
    }
}