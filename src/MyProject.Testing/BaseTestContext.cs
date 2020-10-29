using System;
using System.Diagnostics.CodeAnalysis;
using System.IO.Abstractions.TestingHelpers;
using System.Net.Http;
using System.Text.Json;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Xunit.Abstractions;

namespace MyProject.Testing
{
    /// <summary>
    /// Use this as the basis for tests that target a full application (not nuget package)
    /// </summary>
    /// <typeparam name="TStartup">The Startup class</typeparam>
    public abstract class BaseTestContext<TStartup> : WebApplicationFactory<TStartup> where TStartup : class
    {
        private readonly TestOutputLoggerProvider _loggerProvider;
        private HttpClient? _httpClient;
        private JsonSerializerOptions? _jsonSerializerOptions;

        /// <summary>
        /// Allows manipulation of current time. By default it will be the machines current time
        /// </summary>
        public DateTimeOffset CurrentTime { get; set; } = DateTimeOffset.UtcNow;
        /// <summary>
        /// Allows a test to perform changes to the <see cref="IServiceCollection"/>
        /// This must be used before creating the HttpClient
        /// </summary>
        public Action<WebHostBuilderContext, IServiceCollection>? ConfigureServicesForSingleTest { get; set; }
        /// <summary>
        /// Allows a test to perform changes to teh <see cref="IApplicationBuilder"/>
        /// This must be used before creating the HttpClient
        /// </summary>
        public Action<WebHostBuilderContext, IConfigurationBuilder>? ConfigureAppConfigurationForSingleTest { get; set; }
        /// <summary>
        /// If logic is using IO abstractions, this allows tests to manipulate files
        /// </summary>
        public MockFileSystem FileSystem { get; } = new MockFileSystem();
        /// <summary>
        /// Gets the json serializer options that have been configured
        /// </summary>
        public JsonSerializerOptions JsonSerializerOptions
        {
            get
            {
                return _jsonSerializerOptions ??= Services.GetRequiredService<IOptions<JsonOptions>>().Value.JsonSerializerOptions;
            }
        }

        /// <summary>
        /// Initializes the test context
        /// </summary>
        /// <param name="output"></param>
        protected BaseTestContext(ITestOutputHelper output)
        {
            _loggerProvider = new TestOutputLoggerProvider(output);
            // we want to inspect the cookies, so we don't use the built in system
            ClientOptions.HandleCookies = false;
            ClientOptions.AllowAutoRedirect = false;
        }
        /// <summary>
        /// HttpClient that can be used to make requests against your service
        /// </summary>
        public HttpClient HttpClient
        {
            get
            {
                // make sure that we only have one HttpClient for each test
                return _httpClient ??= CreateClient();
            }
        }

        /// <summary>
        /// Allows applications tests to set up services for all tests
        /// </summary>
        /// <param name="context"></param>
        /// <param name="services"></param>
        [ExcludeFromCodeCoverage]
        protected virtual void ConfigureServicesForAllTests(WebHostBuilderContext context, IServiceCollection services)
        {
        }
        /// <summary>
        /// Allows application tests to set configuration for all tests
        /// </summary>
        /// <param name="context"></param>
        /// <param name="configurationBuilder"></param>
        [ExcludeFromCodeCoverage]
        protected virtual void ConfigureAppConfigurationForAllTests(WebHostBuilderContext context, IConfigurationBuilder configurationBuilder)
        {
        }
        /// <inheritdoc/>
        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder
                .UseDefaultServiceProvider(p => p.ValidateScopes = true)
                .ConfigureServices((ctx, services) =>
                {
                    // allow common test infrastructure for each service to configure services
                    ConfigureServicesForAllTests(ctx, services);

                    // allow individual tests to configure services
                    ConfigureServicesForSingleTest?.Invoke(ctx, services);
                })
                // don't use Configure() for setting up middleware here
                // it will prevent middleware in the Startup class from running
                .ConfigureAppConfiguration((ctx ,configurationBuilder) =>
                {
                    ConfigureAppConfigurationForAllTests(ctx, configurationBuilder);
                    ConfigureAppConfigurationForSingleTest?.Invoke(ctx, configurationBuilder);
                })
                .ConfigureLogging(l =>
                {
                    l.ClearProviders();
                    l.AddFilter("Microsoft", LogLevel.Warning);
                    l.AddProvider(_loggerProvider);
                });
        }

        /// <summary>
        /// Verifies that a message has been logged
        /// </summary>
        /// <param name="logLevel">The log leve</param>
        /// <param name="message">Part of the log message. Compared using Contains</param>
        /// <param name="category">The category</param>
        /// <param name="count">The number of times the message should occur. If null, just makes sure one entry exists</param>
        public void VerifyLog(LogLevel logLevel, string? message = null, string? category = null, int? count = null)
            => _loggerProvider.VerifyLog(logLevel, message, category, count);

        /// <inheritdoc/>
        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _loggerProvider?.Dispose();
            }
        }
    }
}
