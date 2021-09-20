using Prometheus;

public class MetricsSQL
{
    private static readonly Histogram _requestsHistogram =
        Prometheus.Metrics.CreateHistogram("sql_request_duration",
            "DurationOfSql",
            new HistogramConfiguration()
            {
                Buckets = Histogram.ExponentialBuckets(0.001, 2, 16),
                LabelNames = new[]{"sqlQueryName"},
            });

    public static void MeasureSentRequestToDatabreakers(
        string sqlQueryName,
        double responseTimeSeconds)
    {
        _requestsHistogram
            .WithLabels(sqlQueryName)
            .Observe(responseTimeSeconds);
    }
}