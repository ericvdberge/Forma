using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace Forma.Infrastructure.Persistence.Converters;

/// <summary>SQLite does not store <see cref="DateTimeKind"/>; values are written as UTC and read back as UTC.</summary>
internal sealed class UtcDateTimeConverter() : ValueConverter<DateTime, DateTime>(
    v => v.Kind == DateTimeKind.Utc ? v : v.ToUniversalTime(),
    v => DateTime.SpecifyKind(v, DateTimeKind.Utc));
