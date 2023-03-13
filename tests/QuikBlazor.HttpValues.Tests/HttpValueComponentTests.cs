using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using QuikBlazor.HttpValues.Tests.Api;
using QuikBlazor.HttpValues.Tests.TestComponents;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QuikBlazor.HttpValues.Tests;

public class HttpValueComponentTests : IClassFixture<WebApplicationFactory<Program>>, IDisposable
{
    private readonly WebApplicationFactory<Program> _factory;
    private readonly TestContext _context;

    public HttpValueComponentTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory;
        _context = new TestContext();

        var client = factory.CreateDefaultClient();
        _context.Services
            .AddHttpValues()
            .AddSingleton<IHttpClientFactory>(new TestHttpClientFactory(client));
    }

    public class TestComponent : HttpValueComponent
    {
        [HttpGetResult("/test/1")]
        public SimpleData? data { get; set; }

        protected override void BuildRenderTree(RenderTreeBuilder builder)
        {
            if (data is not null)
            {
                builder.AddContent(0, Fragments.Simple, data);
            }
        }
    }

    [Fact]
    public void HttpGetResultAttribute_PopulatesSimpleData()
    {
        var cut = _context.RenderComponent<TestComponent>();

        cut.WaitForAssertion(() =>
        {
            cut.Find("h1").MarkupMatches(@"<h1>1</h1>");
            cut.Find("h2").MarkupMatches(@"<h2>George</h2>");
            cut.Find("h3").MarkupMatches(@"<h3>A curious little monkey</h3>");
        }, TimeSpan.FromSeconds(2));
    }


    public class TestComponent_Param : HttpValueComponent
    {
        [Parameter]
        public int Token { get; set; }

        [HttpGetResult("/test/{Token}")]
        public SimpleData? data { get; set; }

        protected override void BuildRenderTree(RenderTreeBuilder builder)
        {
            if (data is not null)
            {
                builder.AddContent(0, Fragments.Simple, data);
            }
        }
    }

    [Fact]
    public void HttpGetResultAttribute_PopulatesParameterizedData()
    {
        var cut = _context.RenderComponent<TestComponent_Param>(
            ComponentParameter.CreateParameter("Token", 1));

        cut.WaitForAssertion(() =>
        {
            cut.Find("h1").MarkupMatches(@"<h1>1</h1>");
            cut.Find("h2").MarkupMatches(@"<h2>George</h2>");
            cut.Find("h3").MarkupMatches(@"<h3>A curious little monkey</h3>");
        }, TimeSpan.FromSeconds(2));
    }

    public void Dispose()
    {
        _context.Dispose();
    }
}
