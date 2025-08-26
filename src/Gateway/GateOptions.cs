namespace Gateway;

public sealed class GateOptions
{
    public Dictionary<string, string> Services { get; init; } = new();
    public List<AllowedRoute> AllowedRoutes { get; init; } = new();
    public int DefaultTimeoutSeconds { get; init; } = 30;
    public int MaxPayloadSizeBytes { get; init; } = 10_485_760; // 10MB
    public bool EnableCaching { get; init; } = true;
    public int CacheExpirationMinutes { get; init; } = 5;
    public bool EnableMetrics { get; init; } = true;
    public bool EnableTracing { get; init; } = true;
}

public sealed class AllowedRoute
{
    public required string Service { get; init; }
    public required string[] Methods { get; init; }
    public required string PathPrefix { get; init; }
    public bool RequiresAuth { get; init; } = false;
    public string[]? RequiredRoles { get; init; }
}

public sealed class HealthChecksOptions
{
    public int CheckIntervalSeconds { get; init; } = 30;
    public int UnhealthyTimeoutSeconds { get; init; } = 10;
}

public sealed class RedisOptions
{
    public string ConnectionString { get; init; } = "localhost:6379";
    public int Database { get; init; } = 0;
}