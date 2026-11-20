using System.Reflection;
using FluentValidation;
using Forma.Api.Abstractions;
using Forma.Application;
using Forma.Application.Documents;
using Forma.Application.Seeding;
using Forma.Infrastructure;
using Forma.Infrastructure.Persistence;
using Microsoft.OpenApi;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOpenApi(options =>
    // Numbers are read strictly (see PageDocumentJson), so describe them as plain numbers: otherwise the generated
    // TS types read `number | string`.
    options.AddSchemaTransformer((schema, _, _) =>
    {
        if (schema.Type is { } type
            && (type.HasFlag(JsonSchemaType.Integer) || type.HasFlag(JsonSchemaType.Number))
            && type.HasFlag(JsonSchemaType.String))
        {
            schema.Type = type & ~JsonSchemaType.String;
            schema.Pattern = null;
        }

        return Task.CompletedTask;
    }));
// camelCase string enums and strict reading (unknown properties, missing values) for every JSON body;
// page documents need exactly these rules, see PageDocumentJson.
builder.Services.ConfigureHttpJsonOptions(options => PageDocumentJson.Configure(options.SerializerOptions));
// Unreadable bodies and failed validation are answered with a 400 in problem+json format.
builder.Services.AddProblemDetails();

builder.Services.AddValidatorsFromAssemblyContaining<Program>();

builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference();
}

// Skip migrations/seeding when the build-time OpenAPI generator boots the app.
if (Assembly.GetEntryAssembly()?.GetName().Name != "GetDocument.Insider")
{
    // Sample data is always there in development; elsewhere only when Database:SeedSampleData is set.
    var seedSampleData = app.Environment.IsDevelopment() || app.Configuration.GetValue<bool>("Database:SeedSampleData");
    await app.Services.InitializeDatabaseAsync(seedSampleData);
    if (seedSampleData)
        await app.Services.SeedPageContentAsync();
}

app.UseHttpsRedirection();

app.MapEndpoints();

app.Run();

/// <summary>Entry point, public so the integration tests can host the API.</summary>
public partial class Program;
