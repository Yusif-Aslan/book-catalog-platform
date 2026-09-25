using System.ComponentModel.DataAnnotations;

namespace BookCatalog.Api.Validation;

public sealed class IsbnAttribute : ValidationAttribute
{
    public IsbnAttribute()
        : base("The {0} field must be a valid ISBN-10 or ISBN-13.")
    {
    }

    public override bool IsValid(object? value)
    {
        if (value is null)
            return true;

        if (value is not string raw)
            return false;

        var isbn = raw.Replace("-", "").Replace(" ", "");

        return isbn.Length switch
        {
            10 => IsValidIsbn10(isbn),
            13 => IsValidIsbn13(isbn),
            _ => false
        };
    }

    private static bool IsValidIsbn10(string isbn)
    {
        var sum = 0;
        for (var i = 0; i < 10; i++)
        {
            int digit;
            if (i == 9 && (isbn[i] == 'X' || isbn[i] == 'x'))
                digit = 10;
            else if (char.IsAsciiDigit(isbn[i]))
                digit = isbn[i] - '0';
            else
                return false;

            sum += digit * (10 - i);
        }
        return sum % 11 == 0;
    }

    private static bool IsValidIsbn13(string isbn)
    {
        var sum = 0;
        for (var i = 0; i < 13; i++)
        {
            if (!char.IsAsciiDigit(isbn[i]))
                return false;

            var digit = isbn[i] - '0';
            sum += i % 2 == 0 ? digit : digit * 3;
        }
        return sum % 10 == 0;
    }
}