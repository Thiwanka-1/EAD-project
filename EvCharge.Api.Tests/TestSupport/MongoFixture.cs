namespace EvCharge.Api.Tests.TestSupport
{
    /// <summary>
    /// Simple helper that exposes the connection string from the shared TestMongoHost.
    /// </summary>
    public sealed class MongoFixture
    {
        public string ConnectionString { get; }

        public MongoFixture(TestMongoHost host)
        {
            ConnectionString = host.ConnectionString;
        }
    }
}
