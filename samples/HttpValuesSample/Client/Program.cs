using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using QuikBlazor.HttpValuesSample;
using QuikBlazor.HttpValues;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

if (builder.HostEnvironment.IsDevelopment())
{
    builder.Logging.SetMinimumLevel(LogLevel.Trace);
}

builder.Services
    .AddHttpValues()
    .AddHttpClient();

await builder.Build().RunAsync();

public record class Cake(int Id, string Name, string Origin, string Description);

public record class CakeChoice(int Id)
{
    public override string ToString()
    {
        return Id.ToString();
    }
}

public record GQL<T>(T Data);

public record class CakeByIdResponse(Cake CakeById);

public record CakeQuery(int CakeId);

public class NewCakeRequest
{
    public string? Name { get; set; }

    public string? Origin { get; set; }

    public string? Description { get; set; }
}
