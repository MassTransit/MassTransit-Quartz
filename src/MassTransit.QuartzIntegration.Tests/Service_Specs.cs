// Copyright 2007-2012 Chris Patterson, Dru Sellers, Travis Smith, et. al.
//  
// Licensed under the Apache License, Version 2.0 (the "License"); you may not use
// this file except in compliance with the License. You may obtain a copy of the 
// License at 
// 
//     http://www.apache.org/licenses/LICENSE-2.0 
// 
// Unless required by applicable law or agreed to in writing, software distributed
// under the License is distributed on an "AS IS" BASIS, WITHOUT WARRANTIES OR 
// CONDITIONS OF ANY KIND, either express or implied. See the License for the 
// specific language governing permissions and limitations under the License.
namespace MassTransit.QuartzIntegration.Tests
{
    using System.Threading;
    using Magnum.Extensions;
    using NUnit.Framework;
    using Quartz;
    using Quartz.Impl;
    using Scheduling;


    [TestFixture]
    public class Using_the_quartz_service
    {
        [Test]
        public void Should_startup_properly()
        {
            _bus.SchedulePublish(5.Seconds().FromNow(), new A());

            Thread.Sleep(Utils.Timeout);
        }


        class A
        {
        }


        IScheduler _scheduler;
        IServiceBus _bus;

        [TestFixtureSetUp]
        public void Setup_quartz_service()
        {
            ISchedulerFactory schedulerFactory = new StdSchedulerFactory();
            _scheduler = schedulerFactory.GetScheduler();

            _bus = ServiceBusFactory.New(x =>
                {
                    x.ReceiveFrom("loopback://localhost/quartz");

                    x.Subscribe(s => s.Consumer(() => new ScheduleMessageConsumer(_scheduler)));
                });

            _scheduler.Start();
        }

        [TestFixtureTearDown]
        public void Teardown_quartz_service()
        {
            if (_scheduler != null)
                _scheduler.Standby();
            if (_bus != null)
                _bus.Dispose();
            if (_scheduler != null)
                _scheduler.Shutdown();
        }
    }
}