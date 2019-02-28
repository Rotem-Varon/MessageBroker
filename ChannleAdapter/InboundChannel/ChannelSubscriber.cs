using LoggingProvider.Generic;
using MessageBroker.Messaging.Extensions;
using Microsoft.ServiceBus.Messaging;
using System;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Threading.Tasks;

namespace MessageBroker.Messaging.Inbound
{
    public sealed class ChannelSubscriber : ChannelFactory, IMessageListener
    {
        readonly SubscriptionClient _subscriptionClient;
        readonly string _subscriptionName;
        readonly string _channelName;
        int _maxNumOfHandlers; //MaxConcurrentReadersInMessagePump

        public ChannelSubscriber(
            string connectionString,
            string channelName,
            string subscriptionName,
            ushort maxConcurrentReaders,
            ILogger logger)
            : base(connectionString, logger)
        {
            if (string.IsNullOrWhiteSpace(channelName))
                throw new ArgumentException("Channel name was not provided");
            if (string.IsNullOrWhiteSpace(subscriptionName))
                throw new ArgumentException("Subscription name was not provided");
            _subscriptionName = subscriptionName;
            _channelName = channelName;
            SetMaxConcurrentReadersInMessagePump(maxConcurrentReaders);
            _subscriptionClient = _factory.CreateSubscriptionClient(channelName, subscriptionName);
        }

        private void SetMaxConcurrentReadersInMessagePump(ushort maxConcurrentReaders)
        {
            const int reasonableMaxNumOfHandlers = 1000;

            if (maxConcurrentReaders < 1 || maxConcurrentReaders > reasonableMaxNumOfHandlers)
                throw new ArgumentException(
                    $"Invalid value for {nameof(maxConcurrentReaders)}. Probably configuration issue. Channel info: { ToString()}");

            _maxNumOfHandlers = maxConcurrentReaders;
        }

        [ExcludeFromCodeCoverage]
        public void ListenAsync<T>(Func<T, Task> onMessageReceived)
        {
            if (onMessageReceived == null) throw new ArgumentNullException(nameof(onMessageReceived));

            var options = new OnMessageOptions
            {
                MaxConcurrentCalls = _maxNumOfHandlers,
                AutoComplete = false
            };
            _logger.LogInfo("Listening with OnMessage options. " + Environment.NewLine +
                                                   $"AutoComplete: {options.AutoComplete}, " + Environment.NewLine +
                                                   $"MaxConcurrentCalls: {options.MaxConcurrentCalls}, " + Environment.NewLine +
                                                   $"AutoRenewTimeout: {options.AutoRenewTimeout}." +
                                                   $"Channel info: {ToString()}");
            // TODO: use await with SemaphoreSlim in finally https://stackoverflow.com/questions/13636648/wait-for-a-void-async-method
            _subscriptionClient.OnMessageAsync(CreateBrokeredMessageHandler(onMessageReceived), options);
        }

        [ExcludeFromCodeCoverage]
        public void TerminateAsync()
        {
            if (_subscriptionClient != null && !_subscriptionClient.IsClosed)
                _subscriptionClient.Close();
        }

        [ExcludeFromCodeCoverage]
        private Func<BrokeredMessage, Task> CreateBrokeredMessageHandler<T>(Func<T, Task> handleMessage)
        {
            return async (brokeredMessage) =>
            {
                _logger.LogInfo($"Handling BrokeredMessage: {brokeredMessage.MessageId}. Channel info: { ToString()}");

                try
                {
                    Stream stream = brokeredMessage.GetBody<Stream>();
                    string msgPayload;
                    using (TextReader reader = new StreamReader(stream))
                    {
                        msgPayload = await reader.ReadToEndAsync();
                    }
                    _logger.LogInfo($"BrokeredMessage [{brokeredMessage.MessageId}] content: {msgPayload}. Channel info: { ToString()}");
                    await handleMessage(msgPayload.ReadFromJson<T>());
                    await brokeredMessage.CompleteAsync();
                }
                catch (Exception e)
                {
                    _logger.LogError($"Failed to Handle Message: {brokeredMessage.MessageId}, " +
                                                          $"with Exception: {e.GetBaseException().Message}," +
                                                          $"Channel info: {ToString()}");
                }
            };
        }

        public override string ToString()
        {
            return $"{_channelName}:{_subscriptionName}";
        }
    }
}