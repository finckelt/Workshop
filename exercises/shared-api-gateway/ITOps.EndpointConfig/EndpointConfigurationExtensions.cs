using NServiceBus;
using NServiceBus.Logging;
using NServiceBus.Persistence;
using System;
using System.IO;

namespace ITOps.EndpointConfig
{
    public static class EndpointConfigurationExtensions
    {
        static readonly ILog Log = LogManager.GetLogger(typeof(EndpointConfigurationExtensions));

        public static EndpointConfiguration Configure(
            this EndpointConfiguration endpointConfiguration,
            string connectionString,
            Action<RoutingSettings<MsmqTransport>> configureRouting)
        {
            Log.Info("Configuring endpoint...");

            var licensePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "..\\..\\..\\..\\..\\License.xml");
            endpointConfiguration.LicensePath(licensePath);
            endpointConfiguration.UseSerialization<NewtonsoftSerializer>();
            endpointConfiguration.Recoverability().Delayed(c => c.NumberOfRetries(0));

            var transport = endpointConfiguration.UseTransport<MsmqTransport>();
            transport.DisableDeadLetterQueueing();

            var routing = transport.Routing();

            endpointConfiguration.UsePersistence<NHibernatePersistence>().ConnectionString(connectionString);

            endpointConfiguration.SendFailedMessagesTo("error");
            endpointConfiguration.AuditProcessedMessagesTo("audit");

            var conventions = endpointConfiguration.Conventions();
            conventions.DefiningCommandsAs(t => t.Namespace != null && t.Namespace.StartsWith("Divergent") && t.Namespace.EndsWith("Commands") && t.Name.EndsWith("Command"));
            conventions.DefiningEventsAs(t => t.Namespace != null && t.Namespace.StartsWith("Divergent") && t.Namespace.EndsWith("Events") && t.Name.EndsWith("Event"));

            endpointConfiguration.EnableInstallers();

            configureRouting?.Invoke(routing);

            return endpointConfiguration;
        }
    }
}
