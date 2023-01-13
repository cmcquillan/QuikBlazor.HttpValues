using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using HttpBindingsSample;
using HttpBindings;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

builder.Services
    .AddHttpBindings()
    .AddHttpClient();

await builder.Build().RunAsync();

public record class Cake(int Id, string Name, string Origin, string Description);

public record GQL<T>(T Data);

public record class CakeByIdResponse(Cake CakeById);

public record CakeQuery(int CakeId);
