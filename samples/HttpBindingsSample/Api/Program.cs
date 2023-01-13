using HttpBindingsSampleApi.Data;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Mvc;

var builder = WebApplication.CreateBuilder(args);

builder.Services
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


app.MapGet("/errors/{code}", (int code) => Results.StatusCode(code));
app.MapGet("/timeouts/{time}", async (int time) =>
{
    await Task.Delay(time);
    return Results.Ok();
});

app.Run();
