using QuikBlazor.HttpValues.Tests.Api;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using QuikBlazor.HttpValues.Tests.TestComponents;
using Microsoft.AspNetCore.Components;

namespace QuikBlazor.HttpValues.Tests;

public class HttpValueTests : IClassFixture<WebApplicationFactory<Program>>, IDisposable
{
    private readonly WebApplicationFactory<Program> _factory;
    private readonly TestContext _context;

    public HttpValueTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory;
        _context = new TestContext();

        var client = factory.CreateDefaultClient();
        _context.Services
            .AddHttpValues()
            .AddSingleton<IHttpClientFactory>(new TestHttpClientFactory(client));
    }

    public void Dispose()
    {
        _context.Dispose();
    }

    [Fact]
    public void HttpValue_ResolvesHttpData()
    {
        var cut = _context.RenderComponent<TestHttpValue>(
            ComponentParameter.CreateParameter("Url", "/test/1")
        );

        cut.WaitForAssertion(() =>
        {
            cut.Find("h1").MarkupMatches(@"<h1>1</h1>");
            cut.Find("h2").MarkupMatches(@"<h2>George</h2>");
            cut.Find("h3").MarkupMatches(@"<h3>A curious little monkey</h3>");
        }, TimeSpan.FromSeconds(2));
    }
}
