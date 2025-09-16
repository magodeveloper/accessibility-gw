using Gateway;
using Gateway.Models;

namespace Gateway.UnitTests.Helpers;

public static class TestDataFactory
{
    public static GateOptions CreateValidGateOptions()
    {
        return new GateOptions();
    }

    public static TranslateRequest CreateValidTranslateRequest()
    {
        return new TranslateRequest
        {
            Service = "users",
            Method = "GET",
            Path = "/api/users/123"
        };
    }
}
