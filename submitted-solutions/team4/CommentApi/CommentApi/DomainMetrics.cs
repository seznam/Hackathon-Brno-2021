using Prometheus;
using System;

public static class DomainMetrics
{
    public static readonly Histogram ReturnedDatabaseComments =
     Metrics.CreateHistogram("returned_database_comments", "help",
         new HistogramConfiguration
         {
             Buckets = Histogram.LinearBuckets(start: 0, width: 5, count: 10)
         });
    internal static readonly Histogram RequestedMaxComments = Metrics.CreateHistogram("requested_max_comments", "help",
         new HistogramConfiguration
         {
             Buckets = Histogram.LinearBuckets(start: 0, width: 5, count: 10)
         });

    public static readonly Histogram RequestedCommentsLevel =
     Metrics.CreateHistogram("requested_comments_level", "help",
         new HistogramConfiguration
         {
             Buckets = Histogram.LinearBuckets(start: 0, width: 2, count: 10)
         });
}
