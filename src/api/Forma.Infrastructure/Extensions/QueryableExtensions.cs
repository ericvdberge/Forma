using System.Linq.Expressions;

namespace Forma.Infrastructure.Extensions;

public static class QueryableExtensions
{
    /// <summary>Applies <paramref name="predicate"/> only when <paramref name="condition"/> is true.</summary>
    public static IQueryable<T> WhereIf<T>(
        this IQueryable<T> query,
        bool condition,
        Expression<Func<T, bool>> predicate) =>
        condition ? query.Where(predicate) : query;
}
