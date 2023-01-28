using QuikBlazor.HttpValuesSampleApi.Data;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Mvc;

var builder = WebApplication.CreateBuilder(args);

builder.Services
    .AddSingleton<Random>()
    .AddGraphQLServer()
    .AddQueryType<CakeQuery>();

builder.Services.AddCors();

var app = builder.Build();

app.UseCors(policy =>
{
    policy.AllowAnyOrigin();
    policy.AllowAnyMethod();
    policy.AllowAnyHeader();
});

app.MapGraphQL();

var cakeApi = app.MapGroup("/cakes");

cakeApi.MapGet("/", ([FromQuery] string? search) => string.IsNullOrEmpty(search) ? Cakes.List : Cakes.List.Where(c => c.Name?.Contains(search, StringComparison.OrdinalIgnoreCase) ?? false));
cakeApi.MapGet("/{id}", (int id) => Cakes.List.Single(p => p.Id == id));
cakeApi.MapGet("/{id}.html", (int id) => Cakes.List.Where(p => p.Id == id).Select(c => Results.Text(@$"
            <div>
                <h2>{c.Name}</h2>
                <strong>{c.Origin}</strong>
            </div>
            <div>
                <p>
                    {c.Description}
                </p>
            </div>
", "text/html")).Single());
cakeApi.MapGet("/random", ([FromServices] Random rand) =>
{
    return new { Id = rand.Next(1, Cakes.List.Max(p => p.Id)) };
});

cakeApi.MapPost("/", async ([FromBodyAttribute] CakePost cake) =>
{
    await Task.Delay(1000);
    var id = Cakes.List.Max(p => p.Id) + 1;
    var cakeData = new Cake(id, cake.Name, cake.Origin, cake.Description);
    Cakes.List.Add(cakeData);
    return cakeData;
});


app.MapGet("/errors/{code}", (int code) => Results.StatusCode(code));
app.MapGet("/timeouts/{time}", async (int time) =>
{
    await Task.Delay(time);
    return Results.Ok();
});

app.Run();
