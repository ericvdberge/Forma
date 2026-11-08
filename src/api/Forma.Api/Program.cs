using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;
using Forma.Api.Abstractions;
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

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference();

    // Skip migrations/seeding when the build-time OpenAPI generator boots the app.
    if (Assembly.GetEntryAssembly()?.GetName().Name != "GetDocument.Insider")
    {
        await app.Services.InitializeDatabaseAsync();
    }
}

app.UseHttpsRedirection();

app.MapEndpoints();

app.Run();
