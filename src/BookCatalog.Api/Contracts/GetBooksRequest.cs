using System.ComponentModel.DataAnnotations;

namespace BookCatalog.Api.Contracts;

public sealed class GetBooksRequest : IValidatableObject
{
    public const int MaxPage = 1_000_000;
    public const int MaxPageSize = 100;

    [Range(1, MaxPage)]
    public int Page { get; init; } = 1;

    [Range(1, MaxPageSize)]
    public int PageSize { get; init; } = 20;

    [StringLength(200)]
    public string? Title { get; init; }

    [StringLength(100)]
    public string? Author { get; init; }

    [StringLength(50)]
    public string? Genre { get; init; }

    public int? PublishedFrom { get; init; }

    public int? PublishedTo { get; init; }

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (PublishedFrom > PublishedTo)
        {
            yield return new ValidationResult(
                "PublishedFrom must be less than or equal to PublishedTo.",
                [nameof(PublishedFrom), nameof(PublishedTo)]);
        }
    }
}