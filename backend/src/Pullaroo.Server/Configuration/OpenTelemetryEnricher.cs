using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Serilog.Core;
using Serilog.Events;

namespace Pullaroo.Server.Configuration;

internal class OpenTelemetryEnricher : ILogEventEnricher
{
    public void Enrich(LogEvent logEvent, ILogEventPropertyFactory propertyFactory)
    {
        if (Activity.Current != null)
        {
            foreach (var bag in Activity.Current.Baggage)
            {
                logEvent.AddPropertyIfAbsent(propertyFactory.CreateProperty(bag.Key, bag.Value));
            }
        }
    }
}
