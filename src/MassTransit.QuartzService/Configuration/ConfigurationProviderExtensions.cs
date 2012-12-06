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
namespace MassTransit.QuartzService.Configuration
{
    using System;
    using System.Configuration;
    using System.Globalization;


    public static class ConfigurationProviderExtensions
    {
        public static Uri GetServiceBusUriFromSetting(this IConfigurationProvider configurationProvider,
            string settingName)
        {
            string queueName = configurationProvider.GetSetting(settingName);
            return configurationProvider.GetServiceBusUri(queueName);
        }

        public static Uri GetServiceBusUri(this IConfigurationProvider configuration, string queueName)
        {
            string scheme = configuration.GetSetting("Scheme", "rabbitmq");
            if (string.Compare("rabbitmq", scheme, StringComparison.OrdinalIgnoreCase) == 0)
            {
                string host = configuration.GetSetting("RabbitMQHost");
                string vhost = configuration.GetSetting("RabbitMQVirtualHost");
                var builder = new UriBuilder("rabbitmq", host);

                builder.UserName = configuration.GetSetting("RabbitMQUsername");
                builder.Password = configuration.GetSetting("RabbitMQPassword");
                
                var paths = vhost.Split(new[] {'/'}, StringSplitOptions.RemoveEmptyEntries);

                builder.Path = string.Format("/{0}", string.Join("/",paths));

                return new Uri(builder.Uri, queueName);
            }

            if (string.Compare("msmq", scheme, StringComparison.OrdinalIgnoreCase) == 0)
            {
                string host = configuration.GetSetting("MSMQHost");

                var builder = new UriBuilder("msmq", host);

                return new Uri(builder.Uri, queueName);
            }

            throw new ConfigurationErrorsException(string.Format(CultureInfo.InvariantCulture,
                "An unrecognized scheme was found: {0}", scheme));
        }

        public static string GetSetting(this IConfigurationProvider configuration, string key, string defaultValue)
        {
            string value = configuration.GetSetting(key);
            if (string.IsNullOrEmpty(value))
                return defaultValue;

            return value;
        }

        public static int GetSetting(this IConfigurationProvider configuration, string key, int defaultValue)
        {
            string value = configuration.GetSetting(key);
            if (string.IsNullOrEmpty(value))
                return defaultValue;

            int result;
            if (int.TryParse(value, out result))
                return result;

            return defaultValue;
        }

        public static bool GetSetting(this IConfigurationProvider configuration, string key, bool defaultValue)
        {
            string value = configuration.GetSetting(key);
            if (string.IsNullOrEmpty(value))
                return defaultValue;

            bool result;
            if (bool.TryParse(value, out result))
                return result;

            return defaultValue;
        }
    }
}