using System;

namespace MessageBroker.Test.E2E
{
    class TestMsgDto
    {
        public Guid CorrelationId { get; set; }
        public int Temperature { get; set; }
    }
}
