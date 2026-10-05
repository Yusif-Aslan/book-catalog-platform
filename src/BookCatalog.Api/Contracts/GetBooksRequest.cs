using System.ComponentModel.DataAnnotations;

namespace BookCatalog.Api.Contracts;

public sealed class GetBooksRequest
{
    public const int MaxPage = 1_000_000;
    public const int MaxPageSize = 100;

    [Range(1, MaxPage)]
    public int Page { get; init; } = 1;

    [Range(1, MaxPageSize)]
    public int PageSize { get; init; } = 20;
}