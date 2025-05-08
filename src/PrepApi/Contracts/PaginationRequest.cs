using System.ComponentModel;
using System.Text.Json.Serialization;

namespace PrepApi.Contracts;

public record PaginationRequest
{
    [DefaultValue(10)] public int PageSize { get; init; } = 10;

    [DefaultValue(0)] public int PageIndex { get; init; } = 0;
}

public class PaginatedItems<T>(int pageIndex, int pageSize, long count, IEnumerable<T> data) where T : class
{
    public int PageIndex { get; } = pageIndex;

    public int PageSize { get; } = pageSize;

    public long Count { get; } = count;

    public IEnumerable<T> Data { get; } = data;
}

[JsonConverter(typeof(JsonStringEnumConverter<SortOrder>))]
public enum SortOrder
{
    Asc,
    Desc
}