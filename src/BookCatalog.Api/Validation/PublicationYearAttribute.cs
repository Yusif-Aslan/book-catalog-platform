using System.ComponentModel.DataAnnotations;

namespace BookCatalog.Api.Validation;

public sealed class PublicationYearAttribute : ValidationAttribute
{
    public const int MinYear = 1450;

    public PublicationYearAttribute()
        : base($"The {{0}} field must be between {MinYear} and the current year.")
    {
    }

    public override bool IsValid(object? value) =>
        value is int year && year >= MinYear && year <= DateTime.UtcNow.Year;
}