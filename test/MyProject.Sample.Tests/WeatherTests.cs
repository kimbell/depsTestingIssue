using System.Threading.Tasks;
using MyProject.Tests.Infrastructure;
using Xunit;
using Xunit.Abstractions;

namespace MyProject.Sample.Tests
{
    public class WeatherTests
    {
        private readonly ITestOutputHelper _output;

        public WeatherTests(ITestOutputHelper output)
        {
            _output = output;
        }

        [Fact]
        public async Task GetWeather()
        {
            using(var tc = new TestContext(_output))
            {
                var response = await tc.RestClient.WeatherForecastAsync().ConfigureAwait(false);

                Assert.NotEmpty(response);
            }
        }
    }
}
