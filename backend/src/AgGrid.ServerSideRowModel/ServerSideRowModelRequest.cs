// ReSharper disable InconsistentNaming

using System.Text.Json;

namespace AgGrid.ServerSideRowModel;

public record ServerSideRowModelRequest
{
    public int? StartRow { get; init; }
    public int? EndRow { get; init; }
    public IReadOnlyCollection<ColumnV0> RowGroupCols { get; init; } = Array.Empty<ColumnV0>();
    public IReadOnlyCollection<ColumnV0> ValueCols { get; init; } = Array.Empty<ColumnV0>();
    public IReadOnlyCollection<ColumnV0> PivotCols { get; init; } = Array.Empty<ColumnV0>();
    public bool PivotMode { get; init; }
    public IReadOnlyCollection<string> GroupKeys { get; init; } = Array.Empty<string>();
    public IDictionary<string, FilterModel> FilterModel { get; init; } = new Dictionary<string, FilterModel>();
    public IReadOnlyCollection<SortModel> SortModel { get; init; } = Array.Empty<SortModel>();

    public static T FromJson<T>(string? json) where T : ServerSideRowModelRequest, new()
    {
        if (json is null)
            return new T();

        return JsonSerializer.Deserialize<T>(json,
                new JsonSerializerOptions()
                {
                    // Ignore case
                    PropertyNameCaseInsensitive = true
                })
            ?? new T();
    }
}

public record SortModel
{
    public string ColId { get; init; } = string.Empty;
    public string Sort { get; init; } = string.Empty;
}

public static class SortModelSortDirection
{
    public static HashSet<string> All { get; } = new() { Ascending, Descending };

    public const string Ascending = "asc";
    public const string Descending = "desc";
}

public record ColumnV0
{
    public string Id { get; init; } = string.Empty;
    public string DisplayName { get; init; } = string.Empty;
    public string? Field { get; init; }
    public string? AggFunc { get; init; }
}

public record FilterModel
{
    public string FilterType { get; set; }
    public string Type { get; set; }

    public object Filter { get; set; }
    public double FilterTo { get; set; }
    public string DateFrom { get; set; }
    public string DateTo { get; set; }
    public IEnumerable<string> Values { get; set; }

    public string Operator { get; set; }
    public FilterModel Condition1 { get; set; }
    public FilterModel Condition2 { get; set; }
}

public static class FilterModelFilterType
{
    public static HashSet<string> All { get; } = new() { Text, Number, Date, Boolean, Set };

    public const string Text = "text";
    public const string Number = "number";
    public const string Date = "date";
    public const string Boolean = "boolean";
    public const string Set = "set";
}

public static class FilterModelType
{
    public static HashSet<string> All { get; } = new()
    {
        Equals, NotEqual, Contains, NotContains,
        StartsWith, EndsWith, LessThan, LessThanOrEqual,
        GreaterThan, GreaterThanOrEqual, InRange,
        Null, NotNull
    };

    new public const string Equals = "equals";
    public const string NotEqual = "notEqual";

    public const string Contains = "contains";
    public const string NotContains = "notContains";

    public const string StartsWith = "startsWith";
    public const string EndsWith = "endsWith";

    public const string LessThan = "lessThan";
    public const string LessThanOrEqual = "lessThanOrEqual";

    public const string GreaterThan = "greaterThan";
    public const string GreaterThanOrEqual = "greaterThanOrEqual";

    public const string InRange = "inRange";

    public const string Null = "null";
    public const string NotNull = "notNull";
}

public static class FilterModelOperator
{
    public static HashSet<string> All { get; } = new() { And, Or };

    public const string And = "AND";
    public const string Or = "OR";
}