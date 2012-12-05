namespace MassTransit.Quartz
{
    using System;

    public static class ScheduleMessageExtensions
    {
        public static void SchedulePublish<T>(this IServiceBus bus, DateTimeOffset whenToSend, T message)
        {
            IServiceBus cmdBus = ServiceBusFactory.New(x =>
                                                           {
                                                               x.UseRabbitMq();
                                                               x.ReceiveFrom(
                                                                   "rabbitmq://localhost/scheduled_task_control");
                                                           });
            cmdBus.Publish(new ScheduledPublishCommand(NewId.NextGuid(), whenToSend, message));
        }
    }
}