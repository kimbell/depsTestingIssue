using MyProject.Sample.Tests.External;
using MyProject.Testing;

namespace MyProject.Tests.Infrastructure
{
    internal class TestContext : BaseTestContext<Startup>
    {
        private RestClient _restClient;

        public TestContext(Xunit.Abstractions.ITestOutputHelper output) : base(output)
        {
        }
        /// <summary>
        /// Provides access to the strongly typed Rest client
        /// </summary>
        public RestClient RestClient
        {
            get
            {
                return _restClient ??= new RestClient(HttpClient)
                {
                    BaseUrl = "http://weathertest"
                };
            }
        }
    }
}
