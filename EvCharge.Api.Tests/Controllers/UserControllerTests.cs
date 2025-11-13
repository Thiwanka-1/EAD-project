using System.Threading.Tasks;
using EvCharge.Api.Controllers;
using EvCharge.Api.Repositories;
using EvCharge.Api.Tests.TestSupport;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Xunit;

namespace EvCharge.Api.Tests.Controllers
{
    [Collection("mongo")]
    public class UsersControllerTests
    {
        private readonly IConfiguration _cfg;

        public UsersControllerTests(TestMongoHost mongo)
        {
            _cfg = TestConfig.Build(mongo.ConnectionString, "evcharge-tests-users");
        }

        [Fact]
        public async Task Create_Then_GetById_ShouldReturnUser()
        {
            if (TestMongoHost.SkipAll) return;

            var repo = new UserRepository(_cfg);
            var _ = new UsersController(_cfg);

            var user = new EvCharge.Api.Domain.User
            {
                Username = "op1",
                PasswordHash = "pass",
                Role = "Operator",
                IsActive = true
            };

            await repo.CreateAsync(user);
            var got = await repo.GetByIdAsync(user.Id);

            got.Should().NotBeNull();
            got!.Username.Should().Be("op1");
        }

        [Fact]
        public async Task Update_WithoutPassword_DoesNotOverwriteHash()
        {
            if (TestMongoHost.SkipAll) return;

            var repo = new UserRepository(_cfg);
            var _ = new UsersController(_cfg);

            var user = new EvCharge.Api.Domain.User
            {
                Username = "back1",
                PasswordHash = "initial",
                Role = "Backoffice",
                IsActive = true
            };
            await repo.CreateAsync(user);

            var updated = new EvCharge.Api.Domain.User
            {
                Username = "back1-new",
                PasswordHash = "", // simulate "no change"
                Role = "Backoffice",
                IsActive = true
            };

            await repo.UpdateAsync(user.Id, updated);

            var got = await repo.GetByIdAsync(user.Id);
            got.Should().NotBeNull();
            got!.Username.Should().Be("back1-new");
            got.PasswordHash.Should().Be("initial"); // unchanged
        }
    }
}
