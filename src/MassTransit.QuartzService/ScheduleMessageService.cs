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
namespace MassTransit.QuartzService
{
    using System;
    using Configuration;
    using Quartz;
    using Quartz.Impl;
    using QuartzIntegration;
    using Topshelf;


    public class ScheduleMessageService :
        ServiceControl
    {
        readonly int _consumerLimit;
        readonly Uri _controlQueueUri;
        readonly IScheduler _scheduler;
        IServiceBus _bus;

        public ScheduleMessageService(IConfigurationProvider configurationProvider)
        {
            _controlQueueUri = configurationProvider.GetServiceBusUriFromSetting("ControlQueueName");
            _consumerLimit = configurationProvider.GetSetting("ConsumerLimit", Math.Min(2, Environment.ProcessorCount));

            _scheduler = CreateScheduler();
        }

        public bool Start(HostControl hostControl)
        {
            try
            {
                _bus = ServiceBusFactory.New(x =>
                    {
                        // just support everything by default
                        x.UseMsmq();
                        x.UseRabbitMq();
                        x.UseJsonSerializer();

                        // move this to app.config
                        x.ReceiveFrom(_controlQueueUri);
                        x.SetConcurrentConsumerLimit(_consumerLimit);

                        x.Subscribe(s =>
                            {
                                s.Consumer(() => new ScheduleMessageConsumer(_scheduler));
                                s.Consumer(() => new CancelScheduledMessageConsumer(_scheduler));
                            });
                    });

                _scheduler.JobFactory = new MassTransitJobFactory(_bus);
            }
            catch (Exception)
            {
                _scheduler.Shutdown();
                throw;
            }

            _scheduler.Start();

            return true;
        }

        public bool Stop(HostControl hostControl)
        {
            _scheduler.Standby();

            if (_bus != null)
                _bus.Dispose();

            _scheduler.Shutdown();

            return true;
        }

        static IScheduler CreateScheduler()
        {
            ISchedulerFactory schedulerFactory = new StdSchedulerFactory();

            IScheduler scheduler = schedulerFactory.GetScheduler();

            return scheduler;
        }
    }
}