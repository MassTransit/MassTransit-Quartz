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
    using System.Linq;
    using Configuration;
    using Logging;
    using Quartz;
    using Quartz.Impl;
    using QuartzIntegration;
    using Topshelf;


    public class ScheduleMessageService :
        ServiceControl
    {
        readonly IConfigurationProvider _configurationProvider;
        readonly int _consumerLimit;
        readonly Uri _controlQueueUri;
        readonly ILog _log = Logger.Get<ScheduleMessageService>();
        readonly IScheduler _scheduler;
        IServiceBus _bus;

        public ScheduleMessageService(IConfigurationProvider configurationProvider)
        {
            _configurationProvider = configurationProvider;
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
                        x.UseRabbitMq(rmq => rmq.ConfigureRabbitMqHost(_configurationProvider));
                        x.UseJsonSerializer();

                        // move this to app.config
                        x.ReceiveFrom(_controlQueueUri);
                        x.SetConcurrentConsumerLimit(_consumerLimit);

                        x.Subscribe(s =>
                            {
                                s.Consumer(() => new ScheduleMessageConsumer(_scheduler));
                                s.Consumer(() => new CancelScheduledMessageConsumer(_scheduler));

                                s.Consumer(() => new ScheduleTimeoutConsumer(_scheduler));
                                s.Consumer(() => new CancelScheduledTimeoutConsumer(_scheduler));
                            });
                    });

                if (_log.IsInfoEnabled)
                    _log.Info(GetProbeInfo());

                _scheduler.JobFactory = new MassTransitJobFactory(_bus);

                _scheduler.Start();
            }
            catch (Exception)
            {
                _scheduler.Shutdown();
                throw;
            }

            return true;
        }

        string GetProbeInfo()
        {
            var strings = _bus.Probe().Entries
                              .Where(x => !x.Key.StartsWith("zz."))
                              .Select(x => string.Format("{0}: {1}", x.Key, x.Value));

            var probe = string.Join(Environment.NewLine, strings.ToArray());
            return probe;
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