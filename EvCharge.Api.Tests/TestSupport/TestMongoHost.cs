using System;
using System.Threading.Tasks;
using Xunit;

namespace EvCharge.Api.Tests.TestSupport
{
    /// <summary>
    /// Test Mongo "host" that does NOT start Docker.
    /// - If env var TEST_MONGO_CONNECTIONSTRING is present, tests will run against it.
    /// - Otherwise, tests will be skipped (SkipAll = true).
    /// </summary>
    public sealed class TestMongoHost : IAsyncLifetime
    {
        public string ConnectionString { get; private set; } = string.Empty;

        /// <summary>When true, tests should Skip.</summary>
        public static bool SkipAll { get; private set; } = true;

        public Task InitializeAsync()
        {
            // Provide a real connection string to run tests without skipping, e.g.:
            // set TEST_MONGO_CONNECTIONSTRING=mongodb://localhost:27017
            var cs = Environment.GetEnvironmentVariable("TEST_MONGO_CONNECTIONSTRING");

            if (!string.IsNullOrWhiteSpace(cs))
            {
                ConnectionString = cs!;
                SkipAll = false;
            }
            else
            {
                // No Mongo available; skip tests gracefully
                ConnectionString = string.Empty;
                SkipAll = true;
            }

            return Task.CompletedTask;
        }

        public Task DisposeAsync() => Task.CompletedTask;
    }

    [CollectionDefinition("mongo")]
    public sealed class MongoCollection : ICollectionFixture<TestMongoHost> { }
}
