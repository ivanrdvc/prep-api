using Microsoft.EntityFrameworkCore;

using PrepApi.Authorization;
using PrepApi.Data;

namespace PrepApi.Tests.Unit.TestHelpers;

public class FakeDb(IUserContext userContext) : IDbContextFactory<PrepDb>
{
    public PrepDb CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<PrepDb>()
            .UseInMemoryDatabase($"InMemoryTestDb-{DateTime.Now.ToFileTimeUtc()}")
            .Options;

        return new PrepDb(options, userContext);
    }
}