namespace MassTransit.QuartzIntegration
{
    using Contracts;
    using Quartz;
    using Quartz.Impl;


//    public class TaskSchdulerService 
//    {
//        private IServiceBus _commandBus;
//        private IScheduler _scheduler;
//
//        public TaskSchdulerService()
//        {
//            ISchedulerFactory schedulerFactory = new StdSchedulerFactory();
//            _scheduler = schedulerFactory.GetScheduler();
//        }
//
//        #region ServiceControl Members
//
//        public bool Start(HostControl hostControl)
//        {
//            _scheduler.Start();
//            _commandBus = ServiceBusFactory.New(x =>
//                                                    {
//                                                        x.UseRabbitMq();
//                                                        x.ReceiveFrom("rabbitmq://localhost/scheduled_task_control");
//                                                        x.SetConcurrentConsumerLimit(1);
//                                                        x.Subscribe(s => s.Consumer<CommandTaskScheduler>());
//                                                    });
//            return true;
//        }
//
//        public bool Stop(HostControl hostControl)
//        {
//            _scheduler.Shutdown();
//            return true;
//        }
//
//        #endregion
//    }
}