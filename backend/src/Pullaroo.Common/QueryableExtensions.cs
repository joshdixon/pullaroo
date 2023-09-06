using Microsoft.EntityFrameworkCore;

using MongoDB.Driver;
using MongoDB.Driver.Linq;

namespace Pullaroo.Common;

public static class QueryableExtensions
{
    public static async Task<List<T>> ToListAsync<T>(this IQueryable<T> source, CancellationToken cancelToken = default)
    {
        if (source is IMongoQueryable<T> mongoQueryable)
            return await IAsyncCursorSourceExtensions.ToListAsync(mongoQueryable);
        
        return await EntityFrameworkQueryableExtensions.ToListAsync(source, cancelToken);
    }
}
