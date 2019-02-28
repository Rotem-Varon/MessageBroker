using LoggingProvider.Generic;
using MessageBroker.Common;
using MessageBroker.Messaging.Inbound;
using System;
using System.Collections.Concurrent;

namespace MessageBroker.Inbound.Store
{
    public sealed class InboundChannelStore : IInboundChannelStore
    {
        private readonly ConcurrentDictionary<string, IMessageListener> _inboundChannels;
        private readonly string _connectionString;
        private readonly ILogger _logger;

        public InboundChannelStore(string connectionString, ILogger logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(InboundChannelStore) + nameof(logger));
            _connectionString = connectionString;
            _inboundChannels = new ConcurrentDictionary<string, IMessageListener>();
        }
        public void AddInboundChannel(string channelName, string subscriptionName, ushort maxConcurrentReaders)
        {
            var channelId = GetInboundChannelId(channelName, subscriptionName);
            //todo: consider injecting a factory to decouple
            var channelSubscriber = new ChannelSubscriber(_connectionString, channelName, subscriptionName, maxConcurrentReaders, _logger);
            if (channelSubscriber == null) throw new ArgumentNullException($"Failed to add a channel subscriber. Channel: [{channelName}], Sub: [{subscriptionName}] ");
            if (!_inboundChannels.TryAdd(channelId, channelSubscriber))
                throw new Exception($"Failed to add connection. Channel Id: [{channelId}], Subscription: [{channelSubscriber}]");
        }
        public Maybe<IMessageListener> GetChannelSubscriber(string channelName, string subscriptionName)
        {
            var connId = GetInboundChannelId(channelName, subscriptionName);

            IMessageListener channelSubscriber;
            return _inboundChannels.TryGetValue(connId, out channelSubscriber) ?
                new Maybe<IMessageListener>(channelSubscriber) :
                new Maybe<IMessageListener>();
        }

        public void RemoveAllChannelsAsync()
        {
            foreach (var channelSubscriber in _inboundChannels)
            {
                channelSubscriber.Value.TerminateAsync();
            }
            _inboundChannels.Clear();
        }

        private void ValidateInput(string channelName, string subscriptionName)
        {
            if (string.IsNullOrWhiteSpace(channelName))
                throw new ArgumentException(nameof(InboundChannelStore) + nameof(channelName));
            if (string.IsNullOrWhiteSpace(subscriptionName))
                throw new ArgumentException(nameof(InboundChannelStore) + nameof(subscriptionName));
        }
        private const char channelSubscriptionDivider = '?';
        private string GetInboundChannelId(string channelName, string subscriptionName)
        {
            ValidateInput(channelName, subscriptionName);
            //unique name for a connection in the store
            return channelName + channelSubscriptionDivider + subscriptionName;
        }
    }
}