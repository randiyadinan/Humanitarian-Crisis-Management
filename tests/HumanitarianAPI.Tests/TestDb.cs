using HumanitarianAPI.Data;
using Microsoft.EntityFrameworkCore;

namespace HumanitarianAPI.Tests;

public static class TestDb
{
    public static ApplicationDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase($"member4-tests-{Guid.NewGuid()}")
            .Options;

        return new ApplicationDbContext(options);
    }
}
