namespace MassTransit.Quartz
{
    using System;

    public class ScheduledPublishCommand :
        IScheduledPublishCommand
    {
        public ScheduledPublishCommand(Guid correlationId, DateTimeOffset whenToSchedule, object payload)
        {
            CorrelationId = correlationId;
            WhenToSchedule = whenToSchedule;
            Payload = payload;
        }

        #region IScheduledPublishCommand Members

        public Guid CorrelationId { get; set; }
        public DateTimeOffset WhenToSchedule { get; set; }
        public object Payload { get; set; }

        #endregion
    }
}