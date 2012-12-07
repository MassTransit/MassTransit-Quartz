namespace MassTransit.QuartzIntegration.Tests
{
    using System.Threading;
    using Magnum.Extensions;
    using NUnit.Framework;
    using Quartz;
    using Quartz.Impl;
    using Quartz.Simpl;


    [TestFixture]
    public class When_scheduling_a_job_using_quartz
    {

        [Test]
        public void Should_return_the_properties()
        {
            var factory = new StdSchedulerFactory();
            var scheduler = factory.GetScheduler();
            scheduler.Start();

            IJobDetail jobDetail = JobBuilder.Create<MyJob>()
            .UsingJobData("Body", "By Jake")
                .Build();

            ITrigger trigger = TriggerBuilder.Create()
                .ForJob(jobDetail)
                .StartAt(1.Seconds().FromUtcNow())
                .Build();

            scheduler.ScheduleJob(jobDetail, trigger);

            Assert.IsTrue(MyJob.Signaled.WaitOne(Utils.Timeout));

            Assert.AreEqual("By Jake", MyJob.SignaledBody);
        }

        [Test]
        public void Should_return_the_properties_with_custom_factory()
        {
            var factory = new StdSchedulerFactory();
            var scheduler = factory.GetScheduler();
            scheduler.JobFactory = new MassTransitJobFactory(null);
            scheduler.Start();

            IJobDetail jobDetail = JobBuilder.Create<MyJob>()
            .UsingJobData("Body", "By Jake")
                .Build();

            ITrigger trigger = TriggerBuilder.Create()
                .ForJob(jobDetail)
                .StartAt(1.Seconds().FromUtcNow())
                .Build();

            scheduler.ScheduleJob(jobDetail, trigger);

            Assert.IsTrue(MyJob.Signaled.WaitOne(Utils.Timeout));

            Assert.AreEqual("By Jake", MyJob.SignaledBody);
        }


        class MyJob :
            IJob
        {
            public static ManualResetEvent Signaled { get; private set; }
            public static string SignaledBody { get; private set; }

            static MyJob()
            {
                Signaled = new ManualResetEvent(false);
            }

            public void Execute(IJobExecutionContext context)
            {
                Signaled.Set();
                SignaledBody = Body;
            }

            public string Body { get; set; }
        }
    }
}