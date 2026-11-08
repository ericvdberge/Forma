using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;
using Forma.Api.Abstractions;
using Forma.Api.Authentication;
using Forma.Application;
using Forma.Infrastructure;
using Forma.Infrastructure.Persistence;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOpenApi();
builder.Services.ConfigureHttpJsonOptions(options =>
    options.SerializerOptions.Converters.Add(new JsonStringEnumConverter(JsonNamingPolicy.CamelCase)));

builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddFormaAuthentication();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi().AllowAnonymous();
    app.MapScalarApiReference().AllowAnonymous();

    // Skip migrations/seeding when the build-time OpenAPI generator boots the app.
    if (Assembly.GetEntryAssembly()?.GetName().Name != "GetDocument.Insider")
    {
        await app.Services.InitializeDatabaseAsync();
    }
}
else
{
    app.UseHsts();
}

app.UseHttpsRedirection();

app.UseAuthentication();
app.UseAuthorization();
app.UseRateLimiter();

// Every endpoint requires a signed-in user unless it calls AllowAnonymous() (see AddFormaAuthentication).
app.MapEndpoints();

app.Run();
