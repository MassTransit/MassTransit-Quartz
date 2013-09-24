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
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading;
    using Magnum.Extensions;
    using NUnit.Framework;
    using Quartz;
    using Quartz.Impl;
    using Scheduling;


    [TestFixture]
    public class Using_the_quartz_service_with_json
    {
        [Test]
        public void Should_startup_properly()
        {
            _bus.ScheduleMessage(1.Seconds().FromUtcNow(), new A {Name = "Joe"});

            Assert.IsTrue(_receivedA.WaitOne(Utils.Timeout), "Message A not handled");
            Assert.IsTrue(_receivedIA.WaitOne(Utils.Timeout), "Message IA not handled");
        }

       
        class A : IA
        {
            public string Name { get; set; }
        }


        class IA
        {
            string Id { get; set; }
        }


        IScheduler _scheduler;
        IServiceBus _bus;
        ManualResetEvent _receivedA;
        ManualResetEvent _receivedIA;

        [TestFixtureSetUp]
        public void Setup_quartz_service()
        {
            ISchedulerFactory schedulerFactory = new StdSchedulerFactory();
            _scheduler = schedulerFactory.GetScheduler();

            _receivedA = new ManualResetEvent(false);
            _receivedIA = new ManualResetEvent(false);

            _bus = ServiceBusFactory.New(x =>
                {
                    x.ReceiveFrom("loopback://localhost/quartz");
                    x.UseJsonSerializer();

                    x.Subscribe(s =>
                        {
                            s.Handler<A>(msg => _receivedA.Set());
                            s.Handler<IA>(msg => _receivedIA.Set());
                            s.Consumer(() => new ScheduleMessageConsumer(_scheduler));
                        });
                });

            _scheduler.JobFactory = new MassTransitJobFactory(_bus);
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


    [TestFixture]
    public class Using_the_quartz_service_with_xml
    {
        [Test]
        public void Should_startup_properly()
        {
            _bus.ScheduleMessage(1.Seconds().FromUtcNow(), new A {Name = "Joe"});

            Assert.IsTrue(_receivedA.WaitOne(Utils.Timeout), "Message A not handled");
            Assert.IsTrue(_receivedIA.WaitOne(Utils.Timeout), "Message IA not handled");
        }


        class A : IA
        {
            public string Name { get; set; }
        }


        class IA
        {
            string Id { get; set; }
        }


        IScheduler _scheduler;
        IServiceBus _bus;
        ManualResetEvent _receivedA;
        ManualResetEvent _receivedIA;

        [TestFixtureSetUp]
        public void Setup_quartz_service()
        {
            ISchedulerFactory schedulerFactory = new StdSchedulerFactory();
            _scheduler = schedulerFactory.GetScheduler();

            _receivedA = new ManualResetEvent(false);
            _receivedIA = new ManualResetEvent(false);

            _bus = ServiceBusFactory.New(x =>
                {
                    x.ReceiveFrom("loopback://localhost/quartz");
                    x.UseXmlSerializer();

                    x.Subscribe(s =>
                        {
                            s.Handler<A>(msg => _receivedA.Set());
                            s.Handler<IA>(msg => _receivedIA.Set());
                            s.Consumer(() => new ScheduleMessageConsumer(_scheduler));
                        });
                });

            _scheduler.JobFactory = new MassTransitJobFactory(_bus);
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


    [TestFixture]
    public class Using_the_quartz_service_and_cancelling
    {
        [Test]
        public void Should_cancel_the_scheduled_message()
        {
            ScheduledMessage<A> scheduledMessage = _bus.ScheduleMessage(8.Seconds().FromUtcNow(),
                new A {Name = "Joe"});

            _bus.CancelScheduledMessage(scheduledMessage);

            Assert.IsFalse(_receivedA.WaitOne(Utils.Timeout), "Message A handled");
            Assert.IsFalse(_receivedIA.WaitOne(1.Seconds()), "Message IA handled");
        }


        class A : IA
        {
            public string Name { get; set; }
        }


        class IA
        {
            string Id { get; set; }
        }


        IScheduler _scheduler;
        IServiceBus _bus;
        ManualResetEvent _receivedA;
        ManualResetEvent _receivedIA;

        [TestFixtureSetUp]
        public void Setup_quartz_service()
        {
            ISchedulerFactory schedulerFactory = new StdSchedulerFactory();
            _scheduler = schedulerFactory.GetScheduler();

            _receivedA = new ManualResetEvent(false);
            _receivedIA = new ManualResetEvent(false);

            _bus = ServiceBusFactory.New(x =>
                {
                    x.ReceiveFrom("loopback://localhost/quartz");
                    x.UseXmlSerializer();
                    x.SetConcurrentConsumerLimit(1);

                    x.Subscribe(s =>
                        {
                            s.Handler<A>(msg => _receivedA.Set());
                            s.Handler<IA>(msg => _receivedIA.Set());
                            s.Consumer(() => new ScheduleMessageConsumer(_scheduler));
                            s.Consumer(() => new CancelScheduledMessageConsumer(_scheduler));
                        });
                });

            _scheduler.JobFactory = new MassTransitJobFactory(_bus);
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

    [TestFixture]
    public class Using_the_quartz_service_with_headers
    {
        [Test]
        public void Should_not_loose_headers()
        {
            var headers = new KeyValuePair<string, string>[0];
            _bus.SubscribeContextHandler<A>(ctx =>
            {
                headers = ctx.Headers.ToArray();
                _receivedA.Set();
            });

            _bus.ScheduleMessage(1.Seconds().FromUtcNow(), new A { Name = "Joe" },
                ctx =>
                {
                    ctx.SetHeader("ATest", "AValue");
                });

            Assert.IsTrue(_receivedA.WaitOne(Utils.Timeout), "Message A not handled");
            Assert.IsNotEmpty(headers, "No Headers were sent");
            Assert.AreEqual("AValue", headers.First(h => h.Key == "ATest").Value);
        }

    

        class A 
        {
            public string Name { get; set; }
        }



        IScheduler _scheduler;
        IServiceBus _bus;
        ManualResetEvent _receivedA;

        [TestFixtureSetUp]
        public void Setup_quartz_service()
        {
            ISchedulerFactory schedulerFactory = new StdSchedulerFactory();
            _scheduler = schedulerFactory.GetScheduler();

            _receivedA = new ManualResetEvent(false);

            _bus = ServiceBusFactory.New(x =>
            {
                x.ReceiveFrom("loopback://localhost/quartz");
                x.UseJsonSerializer();

                x.Subscribe(s =>
                {
                    s.Consumer(() => new ScheduleMessageConsumer(_scheduler));
                });
            });

            _scheduler.JobFactory = new MassTransitJobFactory(_bus);
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