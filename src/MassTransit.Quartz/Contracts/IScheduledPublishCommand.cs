namespace MassTransit.Quartz
{
    using System;

    public interface IScheduledPublishCommand
    {
        Guid CorrelationId { get; set; }
        DateTimeOffset WhenToSchedule { get; set; }
        object Payload { get; set; }
    }
}