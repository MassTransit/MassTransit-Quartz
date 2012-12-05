namespace MassTransit.QuartzIntegration.Tests
{
    using Magnum.Extensions;
    using NUnit.Framework;
    using Scheduling;


    [TestFixture]
    public class Syntax_Specs
    {
        [Test]
        public void Should_have_a_clean_syntax()
        {
            IServiceBus bus= ServiceBusFactory.New(x => x.ReceiveFrom("loopback://localhost/client"));
            using (bus)
            {
                var scheduledMessage = bus.SchedulePublish(5.Seconds().FromNow(), new A());

                Assert.IsNotNull(scheduledMessage);
            }
        }


        class A
        {
        }
    }
}
