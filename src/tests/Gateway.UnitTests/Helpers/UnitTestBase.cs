using Moq;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Gateway.UnitTests.Helpers;

public abstract class UnitTestBase
{
    protected static Mock<ILogger<T>> CreateMockLogger<T>()
    {
        return new Mock<ILogger<T>>();
    }

    protected static IOptionsMonitor<T> CreateOptionsMonitor<T>(T value) where T : class
    {
        var mock = new Mock<IOptionsMonitor<T>>();
        mock.Setup(x => x.CurrentValue).Returns(value);
        mock.Setup(x => x.Get(It.IsAny<string>())).Returns(value);
        return mock.Object;
    }

    protected static ILogger<T> CreateLogger<T>()
    {
        return CreateMockLogger<T>().Object;
    }
}