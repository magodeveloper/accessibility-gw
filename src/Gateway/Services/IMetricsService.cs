using System.Diagnostics;

namespace Gateway.Services;

public interface IMetricsService
{
    Activity? StartActivity(string operationName, string service, string method, string path);
    void RecordRequest(string service, string method, int statusCode, double responseTimeMs, bool fromCache = false);
    Dictionary<string, object> GetMetrics();
    void ResetMetrics();
}
