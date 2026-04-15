using System;
using System.Collections.Generic;
using System.Diagnostics.Metrics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using InstrumentationInterface;

namespace Instrumentation
{
    public class CustomMetrics : ICustomMetrics, IDisposable
    {
        private readonly Meter _meter;
        private readonly Counter<long> _loginInvocationsCounter;
        private readonly Histogram<double> _requestDurationHistogram;
        private int _activeUserCount;

        // The ObservableGauge is created with a callback that reads the current _activeUserCount.
        // Note: We don't keep a reference to the ObservableGauge since the Meter tracks it internally.
        public CustomMetrics()
        {
            _meter = new Meter("PharmaGo.CustomMetrics");

            _loginInvocationsCounter = _meter.CreateCounter<long>(
                "login_invocations",
                unit: "1",
                description: "Counts the number of login invocations"
            );

            _requestDurationHistogram = _meter.CreateHistogram<double>(
                "request_duration",
                unit: "ms",
                description: "Records the duration of requests in milliseconds"
            );

            // Create an observable gauge for active user count.
            // This gauge will invoke the lambda to return the current value of _activeUserCount.
            _meter.CreateObservableGauge(
                "active_user_count",
                () => new Measurement<int>[] { new Measurement<int>(_activeUserCount) },
                unit: "1",
                description: "The current number of active users"
            );
        }

        // Increment the login invocation counter
        public void LoginInvocations(long value = 1)
        {
            Console.WriteLine("Logging invocation");
            _loginInvocationsCounter.Add(value);
        }

        // Update the active user count; the observable gauge's callback will reflect this new value.
        public void SetActiveUserCount(int count)
        {
            _activeUserCount = count;
        }

        // Record the request duration in milliseconds using the histogram.
        public void RequestDuration(double milliseconds)
        {
            _requestDurationHistogram.Record(milliseconds);
        }

        public void Dispose()
        {
            _meter?.Dispose();
        }
    }
}
