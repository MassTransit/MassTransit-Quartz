namespace MassTransit.QuartzIntegration.Contracts
{
    using Quartz;


    public class CommandTaskScheduler :
        Consumes<IScheduledPublishCommand>.Context
    {
        #region Context Members

        public void Consume(IConsumeContext<IScheduledPublishCommand> message)
        {
            var jobBuilder = JobBuilder.Create<PublishJob>()
                .RequestRecovery(true);
            var jobDetail = jobBuilder.Build();
            var triggerBuilder = TriggerBuilder.Create()
                .ForJob(jobDetail)
                .StartAt(message.Message.WhenToSchedule);
            var trigger = triggerBuilder.Build();
            message.Bus.Publish(message.Message.Payload);
        }

        #endregion
    }
}