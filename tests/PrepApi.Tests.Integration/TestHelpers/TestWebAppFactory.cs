using System.Security.Claims;
using System.Text.Encodings.Web;

using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

using PrepApi.Data;

using Testcontainers.PostgreSql;

namespace PrepApi.Tests.Integration.TestHelpers;

public class TestWebAppFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    private readonly PostgreSqlContainer _dbContainer = new PostgreSqlBuilder()
        .WithImage("postgres:17")
        .WithDatabase("test_db")
        .WithUsername("test_user")
        .WithPassword("test_password")
        .Build();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureServices(services =>
        {
            var descriptor = services.SingleOrDefault(d => d.ServiceType == typeof(DbContextOptions<PrepDb>));

            if (descriptor != null) services.Remove(descriptor);

            services.AddDbContext<PrepDb>(options => options.UseNpgsql(_dbContainer.GetConnectionString()));

            services.AddAuthentication(options =>
                {
                    options.DefaultAuthenticateScheme = TestAuthenticationHandler.TestScheme;
                    options.DefaultChallengeScheme = TestAuthenticationHandler.TestScheme;
                })
                .AddScheme<AuthenticationSchemeOptions, TestAuthenticationHandler>(
                    TestAuthenticationHandler.TestScheme, _ => { });
        });
    }

    protected override void ConfigureClient(HttpClient client)
    {
        client.DefaultRequestHeaders.Add(
            TestAuthenticationHandler.AuthenticationHeaderName,
            TestConstants.TestUserExternalId);

        base.ConfigureClient(client);
    }

    public Task<PrepDb> CreateScopedDbContextAsync()
    {
        var scope = Services.CreateAsyncScope();
        return Task.FromResult(scope.ServiceProvider.GetRequiredService<PrepDb>());
    }

    public async Task InitializeAsync()
    {
        await _dbContainer.StartAsync();

        using var scope = Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<PrepDb>();
        await dbContext.Database.EnsureCreatedAsync();

        await dbContext.SeedUserAsync(
            userId: TestConstants.TestUserId,
            externalId: TestConstants.TestUserExternalId,
            email: TestConstants.TestUserEmail);
    }

    public HttpClient CreateUnauthenticatedClient()
    {
        var client = base.CreateClient();
        client.DefaultRequestHeaders.Remove(TestAuthenticationHandler.AuthenticationHeaderName);

        return client;
    }

    public new async Task DisposeAsync()
    {
        await _dbContainer.StopAsync();
    }
}

public class TestAuthenticationHandler(
    IOptionsMonitor<AuthenticationSchemeOptions> options,
    ILoggerFactory logger,
    UrlEncoder encoder)
    : AuthenticationHandler<AuthenticationSchemeOptions>(options, logger, encoder)
{
    public const string TestScheme = "TestScheme";
    public const string AuthenticationHeaderName = "X-Test-User-Id";

    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        if (!Request.Headers.TryGetValue(AuthenticationHeaderName, out var userIdValues))
        {
            return Task.FromResult(AuthenticateResult.NoResult());
        }

        var externalUserId = userIdValues.FirstOrDefault() ?? TestConstants.TestUserExternalId;
        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, externalUserId)
        };
        var identity = new ClaimsIdentity(claims, Scheme.Name);
        var principal = new ClaimsPrincipal(identity);
        var ticket = new AuthenticationTicket(principal, Scheme.Name);

        return Task.FromResult(AuthenticateResult.Success(ticket));
    }
}