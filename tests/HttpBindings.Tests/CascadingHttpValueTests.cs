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
    }

    [Fact]
    public void CascadingHttpValue_RendersData()
    {
        var cut = _context.RenderComponent<CascadingHttpValue<SimpleData>>(
            ComponentParameter.CreateParameter("Context", "data"),
            ComponentParameter.CreateParameter("Url", "/test/1"),
            ComponentParameter.CreateParameter("ChildContent", Fragments.Simple));

        cut.WaitForElement("#data", TimeSpan.FromSeconds(2));

        cut.Find("h1").MarkupMatches(@"<h1>1</h1>");
        cut.Find("h2").MarkupMatches(@"<h2>George</h2>");
        cut.Find("h3").MarkupMatches(@"<h3>A curious little monkey</h3>");
    }

    [Fact]
    public void CascadingHttpValue_RendersRouteTemplate()
    {
        var cut = _context.RenderComponent<CascadingHttpValue<SimpleData>>(
            ComponentParameter.CreateParameter("Context", "data"),
            ComponentParameter.CreateParameter("Url", "/test/{token}"),
            ComponentParameter.CreateParameter("ChildContent", Fragments.Simple),
            ComponentParameter.CreateParameter("token", "1"));

        cut.WaitForElement("#data", TimeSpan.FromSeconds(2));

        cut.Find("h1").MarkupMatches(@"<h1>1</h1>");
        cut.Find("h2").MarkupMatches(@"<h2>George</h2>");
        cut.Find("h3").MarkupMatches(@"<h3>A curious little monkey</h3>");
    }

    [Fact]
    public void CascadingHttpValue_UpdatesFromNewParameter()
    {
        var cut = _context.RenderComponent<CascadingHttpValue<SimpleData>>(
            ComponentParameter.CreateParameter("Context", "data"),
            ComponentParameter.CreateParameter("Url", "/test/{token}"),
            ComponentParameter.CreateParameter("ChildContent", Fragments.Simple),
            ComponentParameter.CreateParameter("token", "1"));

        cut.WaitForElement("#data", TimeSpan.FromSeconds(2));

        cut.SetParametersAndRender(ComponentParameter.CreateParameter("token", "2"));

        cut.WaitForState(() => cut.Find("h1").TextContent != "1");

        cut.Find("h1").MarkupMatches(@"<h1>2</h1>");
    }

    [Fact]
    public void CascadingHttpValue_ErrorResultsInErrorTemplate()
    {
        var cut = _context.RenderComponent<CascadingHttpValue<SimpleData>>(
            ComponentParameter.CreateParameter("Context", "data"),
            ComponentParameter.CreateParameter("Url", "/test/errors/404"),
            ComponentParameter.CreateParameter("ChildContent", Fragments.Simple),
            ComponentParameter.CreateParameter("ErrorTemplate", Fragments.ErrorBasic));

        cut.WaitForElement("#error", TimeSpan.FromSeconds(2));
        var err = cut.Find("#error");

        Assert.Equal(HttpValueErrorState.HttpError, cut.Instance.ErrorState);
        Assert.Equal("Error State", err.TextContent);
    }

    [Fact]
    public void CascadingHttpValue_Http404ErrorResultsIn404Template()
    {
        var cut = _context.RenderComponent<CascadingHttpValue<SimpleData>>(
            ComponentParameter.CreateParameter("Context", "data"),
            ComponentParameter.CreateParameter("Url", "/test/errors/404"),
            ComponentParameter.CreateParameter("ChildContent", Fragments.Simple),
            ComponentParameter.CreateParameter("ErrorTemplate", Fragments.Error404));

        cut.WaitForElement("#error", TimeSpan.FromSeconds(2));
        var err = cut.Find("#error");

        Assert.Equal(HttpValueErrorState.HttpError, cut.Instance.ErrorState);
        Assert.Equal("Error 404", err.TextContent);
    }

    [Fact]
    public void CascadingHttpValue_HttpTimeoutResultsInTimeoutTemplate()
    {
        var cut = _context.RenderComponent<CascadingHttpValue<SimpleData>>(
            ComponentParameter.CreateParameter("Context", "data"),
            ComponentParameter.CreateParameter("Url", "/test/error/timeout"),
            ComponentParameter.CreateParameter("TimeoutMilliseconds", 50),
            ComponentParameter.CreateParameter("ChildContent", Fragments.Simple),
            ComponentParameter.CreateParameter("ErrorTemplate", Fragments.Timeout));

        cut.WaitForElement("#error", TimeSpan.FromSeconds(2));
        var err = cut.Find("#error");

        Assert.Equal(HttpValueErrorState.Timeout, cut.Instance.ErrorState);
        Assert.Equal("Timeout", err.TextContent);
    }

    [Fact]
    public void CascadingHttpValue_ShowsLoadingContent()
    {
        var cut = _context.RenderComponent<CascadingHttpValue<SimpleData>>(
            ComponentParameter.CreateParameter("Context", "data"),
            ComponentParameter.CreateParameter("Url", "/test/error/timeout"),
            ComponentParameter.CreateParameter("TimeoutMilliseconds", 50),
            ComponentParameter.CreateParameter("ChildContent", Fragments.Simple),
            ComponentParameter.CreateParameter("LoadingContent", Fragments.Loading));

        cut.WaitForElement("#loading", TimeSpan.FromSeconds(2));
        var err = cut.Find("#loading");

        Assert.Equal("Loading", err.TextContent);
    }

}