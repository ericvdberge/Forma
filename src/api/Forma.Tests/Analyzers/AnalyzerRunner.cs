using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.EntityFrameworkCore;

namespace Forma.Tests.Analyzers;

/// <summary>Compiles a snippet against the .NET and EF Core assemblies and returns what an analyzer reports on it.</summary>
public static class AnalyzerRunner
{
    private static readonly ImmutableArray<MetadataReference> References =
    [
        .. ((string)AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES")!)
            .Split(Path.PathSeparator)
            .Select(path => MetadataReference.CreateFromFile(path)),
        MetadataReference.CreateFromFile(typeof(DbContext).Assembly.Location),
    ];

    /// <summary>The analyzer's diagnostics as <c>(id, the code they point at)</c>, in source order.</summary>
    public static async Task<IReadOnlyList<(string Id, string Code)>> RunAsync(DiagnosticAnalyzer analyzer, string source)
    {
        var tree = CSharpSyntaxTree.ParseText(source);
        var compilation = CSharpCompilation.Create(
            "Snippet",
            [tree],
            References,
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary, nullableContextOptions: NullableContextOptions.Enable));

        AssertCompiles(compilation);

        var diagnostics = await compilation
            .WithAnalyzers([analyzer])
            .GetAnalyzerDiagnosticsAsync();

        return diagnostics
            .OrderBy(d => d.Location.SourceSpan.Start)
            .Select(d => (d.Id, tree.GetText().ToString(d.Location.SourceSpan)))
            .ToList();
    }

    private static void AssertCompiles(Compilation compilation)
    {
        var errors = compilation.GetDiagnostics().Where(d => d.Severity == DiagnosticSeverity.Error).ToList();

        Assert.True(errors.Count == 0, string.Join(Environment.NewLine, errors));
    }
}
