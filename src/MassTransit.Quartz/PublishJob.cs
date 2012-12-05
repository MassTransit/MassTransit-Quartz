using IJob = Quartz.IJob;
using IJobExecutionContext = Quartz.IJobExecutionContext;

namespace MassTransit.Quartz
{
    public class PublishJob :
        IJob
    {
        #region IJob Members

        public void Execute(IJobExecutionContext context)
        {
            //var bus = context.JobDetail.JobDataMap.Get("bus") as IServiceBus;
            //var message = context.JobDetail.JobDataMap.Get("message");
            //bus.Publish(message);
        }

        #endregion
    }
}