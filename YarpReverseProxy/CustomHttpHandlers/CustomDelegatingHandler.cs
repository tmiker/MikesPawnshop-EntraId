using Polly.Wrap;

namespace YarpReverseProxy.CustomHttpHandlers
{
    public class CustomDelegatingHandler : DelegatingHandler
    {
        // NOTE: This class was riginally intended for use in in CustomHttpClientFactory, but this
        // class was replaced with PolicyHttpMessageHandler from Microsoft.Extensions.Http; namespace 
        private readonly AsyncPolicyWrap _policyWrap;
                
        public CustomDelegatingHandler(AsyncPolicyWrap policyWrap)
        {
            _policyWrap = policyWrap;
        }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            return _policyWrap.ExecuteAsync(() => base.SendAsync(request, cancellationToken));
        }
    }
}
