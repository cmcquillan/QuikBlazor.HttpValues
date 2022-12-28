using HttpBindings.Tests.Api.Responses;

namespace HttpBindings.Tests.Api;

public class Program
{
    private static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);
        var app = builder.Build();

        app.MapGet("/test/simple", () => new SimpleData(1, "George", "A curious little monkey"));

        app.Run();
    }
}