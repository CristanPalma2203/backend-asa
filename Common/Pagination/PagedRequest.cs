namespace backend_asa.Common.Pagination;

public record PagedRequest(int Page = 1, int PageSize = 10)
{
    public const int MaxPageSize = 50;

    public int Page { get; init; } = Page < 1 ? 1 : Page;

    public int PageSize { get; init; } = PageSize switch
    {
        < 1 => 10,
        > MaxPageSize => MaxPageSize,
        _ => PageSize
    };
}
