using System.Collections.Generic;
using Microsoft.Extensions.Configuration;

namespace EvCharge.Api.Tests.TestSupport
{
    public static class TestConfig
    {
        public static IConfiguration Build(string connectionString, string databaseName)
        {
            var dict = new Dictionary<string, string?>
            {
                ["Mongo:ConnectionString"] = connectionString,
                ["Mongo:Database"] = databaseName
            };

            return new ConfigurationBuilder()
                .AddInMemoryCollection(dict!)
                .Build();
        }
    }
}
