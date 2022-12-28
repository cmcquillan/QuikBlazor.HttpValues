using HttpBindings.Tests.Api.Responses;

namespace HttpBindings.Tests.Api;

public class Program
{
    private static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);
        var app = builder.Build();

        app.MapGet("/test/{id}", (int id) => new SimpleData(id, "George", "A curious little monkey"));
        app.MapGet("/test/debounce/{id}", async (int id) => { await Task.Delay(1000); return new SimpleData(id, "George", "A curious little monkey"); });
        app.MapGet("/test/error/404", () => Results.NotFound());
        app.MapGet("/test/error/timeout", async (CancellationToken ct) => await Task.Delay(1000, ct));

        app.Run();
    }
}