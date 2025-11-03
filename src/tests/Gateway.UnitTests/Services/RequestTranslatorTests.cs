using Xunit;
using NSubstitute;
using Gateway.Models;
using System.Net.Http;
using Gateway.Services;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Yarp.ReverseProxy.Forwarder;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Logging;

namespace Gateway.UnitTests.Services
{
    public class RequestTranslatorTests
    {
        private readonly RequestTranslator _requestTranslator;

        public RequestTranslatorTests()
        {
            var gateOptions = new GateOptions
            {
                Services = new Dictionary<string, string> { { "users", "http://localhost:5001" } },
                AllowedRoutes = new List<AllowedRoute> { new AllowedRoute { Service = "users", Methods = new[] { "GET", "POST" }, PathPrefix = "/api/users" } }
            };

            _requestTranslator = new RequestTranslator(
                Options.Create(gateOptions),
                Substitute.For<IHttpForwarder>(),
                new HttpClient(),
                Substitute.For<ICacheService>(),
                Substitute.For<IMetricsService>(),
                Substitute.For<IResiliencePolicyService>(),
                Substitute.For<ILogger<RequestTranslator>>());
        }

        [Fact]
        public void IsAllowed_ValidRequest_ShouldReturnTrue()
        {
            var request = new TranslateRequest { Service = "users", Method = "GET", Path = "/api/users/123" };
            var result = _requestTranslator.IsAllowed(request);
            result.Should().BeTrue();
        }

        [Fact]
        public void IsAllowed_InvalidService_ShouldReturnFalse()
        {
            var request = new TranslateRequest { Service = "invalid", Method = "GET", Path = "/api/users/123" };
            var result = _requestTranslator.IsAllowed(request);
            result.Should().BeFalse();
        }

    }
}
