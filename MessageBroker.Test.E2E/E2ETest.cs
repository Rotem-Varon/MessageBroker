using LoggingProvider.AppInsights;
using LoggingProvider.Generic;
using MessageBroker.Channel.Managment;
using MessageBroker.Inbound.Store;
using MessageBroker.Messaging;
using MessageBroker.Outbound.Store;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace MessageBroker.Test.E2E
{
    public sealed class MessageBrokerE2ETest
    {
        ILogger _logger;
        IChannelStats _channelStats;
        IInboundChannelsLifeCycle _inboundChannelsLifeCycle;
        IOutboundChannelsLifeCycle _outboundChannelsLifeCycle;
        IInboundChannelStore _inboundChannelStore;
        IOutboundChannelStore _outboundChannelStore;

        string _connectionString = "Add your azure service bus connection string here";
        ushort _maxConcurrentReaders = 10;

        string _messageBrokerE2ETestTopic = "MessageBrokerE2ETestTopic" + DateTime.Now.Ticks;
        string _messageBrokerE2ETestSub = "MessageBrokerE2ETestSub";

        const int ProcessingTime = 3000;

        [SetUp]
        public void Init()
        {
            _logger = new LoggingProviderAppInsights(nameof(MessageBrokerE2ETest) + nameof(Init));

            _inboundChannelStore = new InboundChannelStore(_connectionString, _logger);
            _outboundChannelStore = new OutboundChannelStore(_connectionString, _logger);

            CreateMessagingEntities();
            InitializeInboundChannels();
            InitializeOutboundChannels();
            InitializeChannelStatistics();
        }
        [TearDown]
        public void Cleanup()
        {
            TermInboundChannels();
            TermOutboundChannels();
            //Thread.Sleep(70000); //time for logs to flush appInsights
        }

        private void CreateMessagingEntities()
        {
            var messagingEntitiesFactory = new MessagingEntitiesFactory(_connectionString, _logger);
            var builder = new MessagingEntitiesBuilder(messagingEntitiesFactory);
            builder.BuildMessagingEntitiesStructure(_messageBrokerE2ETestTopic, _messageBrokerE2ETestSub);
        }

        private void InitializeChannelStatistics()
        {
            _channelStats = new ChannelStats(_connectionString, _logger);
        }

        private void InitializeInboundChannels()
        {
            _inboundChannelsLifeCycle = new InboundChannelsLifeCycle(_inboundChannelStore);
            _inboundChannelsLifeCycle.InitInboundChannels(_messageBrokerE2ETestTopic, new List<string>() { _messageBrokerE2ETestSub }, _maxConcurrentReaders);
        }

        private void InitializeOutboundChannels()
        {
            _outboundChannelsLifeCycle = new OutboundChannelsLifeCycle(_outboundChannelStore);
            _outboundChannelsLifeCycle.InitOutboundChannel(_messageBrokerE2ETestTopic);
        }

        private void TermInboundChannels()
        {
            _inboundChannelsLifeCycle.TermInboundChannels();
        }

        private void TermOutboundChannels()
        {
            _outboundChannelsLifeCycle.TermOutboundChannels();
        }

        [Test]
        [TestCase(3)]
        public async Task AllSentMsgsConsumed(int numOfTestMsgs)
        {
            //we expect not msgs on a newly created channel
            var channelMsgCount = await _channelStats.GetMessageCountForSubscriptionAsync(_messageBrokerE2ETestTopic, _messageBrokerE2ETestSub);
            Assert.AreEqual(0, channelMsgCount);

            //send msgs
            var testMsgIds = new List<Guid>();
            var testMsgSender = _outboundChannelStore.GetOutboundChannel(_messageBrokerE2ETestTopic).Single();
            var random = new Random();
            for (int i = 0; i < numOfTestMsgs; i++)
            {
                var testMsgId = Guid.NewGuid();

                var testMsg = new TestMsgDto()
                {
                    CorrelationId = testMsgId,
                    Temperature = random.Next()
                };

                await testMsgSender.SendAsync(new TypedMessage
                {
                    Body = testMsg
                });
                testMsgIds.Add(testMsgId);
            }

            for (int i = 0; i < numOfTestMsgs; i++)
            {
                channelMsgCount = await _channelStats.GetMessageCountForSubscriptionAsync(_messageBrokerE2ETestTopic, _messageBrokerE2ETestSub);
                if (channelMsgCount > 0)
                    Thread.Sleep(ProcessingTime);
                else
                    break;
            }
            Assert.AreEqual(numOfTestMsgs, channelMsgCount);

            var channelSubscriber =
                _inboundChannelStore.GetChannelSubscriber(_messageBrokerE2ETestTopic, _messageBrokerE2ETestSub).Single();
            channelSubscriber.ListenAsync<TestMsgDto>((msg) =>
            {
                if (testMsgIds.Contains(msg.CorrelationId))
                    testMsgIds.Remove(msg.CorrelationId);
                return Task.CompletedTask;
            });


            for (int i = 0; i < numOfTestMsgs; i++)
            {
                if (testMsgIds.Count > 0)
                    Thread.Sleep(ProcessingTime);
                else
                    break;
            }


            //we expect all msgs read at this point
            channelMsgCount = await _channelStats.GetMessageCountForSubscriptionAsync(_messageBrokerE2ETestTopic, _messageBrokerE2ETestSub);
            Assert.AreEqual(0, channelMsgCount);
        }
    }
}
