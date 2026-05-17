using Azure.Core;
using Microsoft.Extensions.Http;
using Polly;
using Polly.Extensions.Http;
using System.Net;
using Yarp.ReverseProxy.Forwarder;

namespace YarpReverseProxy.CustomHttpHandlers
{
    /// <summary>
    /// Implements a custom IForwarderHttpClientFactory interface which injects a custom handler to replace the default 
    /// HttpMessageHandler used by the default forwarder. 
    /// </summary>
    public class CustomHttpClientFactory : IForwarderHttpClientFactory
    {
        private readonly ILogger<CustomHttpClientFactory> _logger;

        public CustomHttpClientFactory(ILogger<CustomHttpClientFactory> logger)
        {
            _logger = logger;
        }

        public HttpMessageInvoker CreateClient(ForwarderHttpClientContext context)
        {
            // Base handler (same as YARP default)
            var handler = new SocketsHttpHandler
            {
                AllowAutoRedirect = false,
                AutomaticDecompression = DecompressionMethods.None,
                UseProxy = false,
                PooledConnectionLifetime = TimeSpan.FromMinutes(2),
                ConnectTimeout = TimeSpan.FromSeconds(10)
            };

            // Polly policies
            var retryPolicy = HttpPolicyExtensions
                .HandleTransientHttpError()
                // .WaitAndRetryAsync(3, retryAttempt => TimeSpan.FromMilliseconds(200 * retryAttempt));
                .WaitAndRetryAsync(5, retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)));

            var circuitBreakerPolicy = HttpPolicyExtensions
                .HandleTransientHttpError()
                .CircuitBreakerAsync(
                    handledEventsAllowedBeforeBreaking: 5,
                    durationOfBreak: TimeSpan.FromSeconds(30)
                );

            // Combine policies
            var policyWrap = Policy.WrapAsync(retryPolicy, circuitBreakerPolicy);

            // Wrap handler with Polly
            // Note: PolicyHttpMessageHandler is in Microsoft.Extensions.Http; namespace
            var policyHandler = new PolicyHttpMessageHandler(policyWrap)
            {
                InnerHandler = handler
            };

            _logger.LogInformation(">>>>>>  >>>>>   >>>>   >>> CustomHttpClientFactory configured with retry and circuit breaker policies for cluster {ClusterId}.", context.ClusterId);

            return new HttpMessageInvoker(policyHandler, disposeHandler: true);
        }
    }
}
