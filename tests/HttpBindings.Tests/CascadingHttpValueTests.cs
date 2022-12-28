using HttpBindings.Tests.Api;
using HttpBindings.Tests.TestComponents;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;

namespace HttpBindings.Tests;

public class CascadingHttpValueTests : IClassFixture<WebApplicationFactory<Program>>, IDisposable
{
    private readonly WebApplicationFactory<Program> _factory;
    private readonly TestContext _context;

    public CascadingHttpValueTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory;
        _context = new TestContext();

        var client = factory.CreateDefaultClient();
        _context.Services.AddSingleton<IHttpClientFactory>(new TestHttpClientFactory(client));
    }

    public void Dispose()
    {
        _context.Dispose();
        _factory.Dispose();
    }

    [Fact]
    public void SimpleRequest_RendersData()
    {
        var cut = _context.RenderComponent<SimpleRequest>();
        cut.WaitForElement("#data", TimeSpan.FromSeconds(2));

        cut.Find("h1").MarkupMatches(@"<h1>1</h1>");
        cut.Find("h2").MarkupMatches(@"<h2>George</h2>");
        cut.Find("h3").MarkupMatches(@"<h3>A curious little monkey</h3>");
    }
}