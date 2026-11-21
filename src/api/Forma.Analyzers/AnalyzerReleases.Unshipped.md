; Unshipped analyzer release
; https://github.com/dotnet/roslyn-analyzers/blob/main/src/Microsoft.CodeAnalysis.Analyzers/ReleaseTrackingAnalyzers.Help.md

### New Rules

Rule ID | Category | Severity | Notes
--------|----------|----------|-------
FORMA001 | Forma.Repositories | Error | Filter queries with ApplyFilter, not with Where or a predicate
FORMA002 | Forma.Repositories | Error | Repository queries go through ApplyFilter
