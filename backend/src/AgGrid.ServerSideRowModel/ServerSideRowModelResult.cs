namespace AgGrid.ServerSideRowModel;

public record ServerSideRowModelResult<T>
{
    public IReadOnlyCollection<T> Data { get; init; }
    public int? LastRow { get; init; }
}
