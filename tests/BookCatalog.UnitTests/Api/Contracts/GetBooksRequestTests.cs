using System.ComponentModel.DataAnnotations;
using BookCatalog.Api.Contracts;

namespace BookCatalog.UnitTests.Api.Contracts;

public class GetBooksRequestTests
{
    [Fact]
    public void Validate_WithDefaultValues_ReturnsNoErrors()
    {
        var results = Validate(new GetBooksRequest());

        Assert.Empty(results);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(GetBooksRequest.MaxPage + 1)]
    public void Validate_WithPageOutOfRange_ReturnsErrorForPage(int page)
    {
        var results = Validate(new GetBooksRequest { Page = page });

        AssertHasErrorFor(results, nameof(GetBooksRequest.Page));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(GetBooksRequest.MaxPageSize + 1)]
    public void Validate_WithPageSizeOutOfRange_ReturnsErrorForPageSize(int pageSize)
    {
        var results = Validate(new GetBooksRequest { PageSize = pageSize });

        AssertHasErrorFor(results, nameof(GetBooksRequest.PageSize));
    }

    [Fact]
    public void Validate_WithMaxPageSize_ReturnsNoErrors()
    {
        var results = Validate(new GetBooksRequest { PageSize = GetBooksRequest.MaxPageSize });

        Assert.Empty(results);
    }

    [Fact]
    public void Validate_WithTooLongTitle_ReturnsErrorForTitle()
    {
        var results = Validate(new GetBooksRequest { Title = new string('a', 201) });

        AssertHasErrorFor(results, nameof(GetBooksRequest.Title));
    }

    [Fact]
    public void Validate_WhenPublishedFromIsAfterPublishedTo_ReturnsErrorForBothFields()
    {
        var results = Validate(new GetBooksRequest { PublishedFrom = 2020, PublishedTo = 2000 });

        AssertHasErrorFor(results, nameof(GetBooksRequest.PublishedFrom));
        AssertHasErrorFor(results, nameof(GetBooksRequest.PublishedTo));
    }

    [Fact]
    public void Validate_WhenPublishedFromEqualsPublishedTo_ReturnsNoErrors()
    {
        var results = Validate(new GetBooksRequest { PublishedFrom = 2008, PublishedTo = 2008 });

        Assert.Empty(results);
    }

    [Fact]
    public void Validate_WithOnlyPublishedFrom_ReturnsNoErrors()
    {
        var results = Validate(new GetBooksRequest { PublishedFrom = 2000 });

        Assert.Empty(results);
    }

    private static List<ValidationResult> Validate(GetBooksRequest request)
    {
        var results = new List<ValidationResult>();
        Validator.TryValidateObject(request, new ValidationContext(request), results, validateAllProperties: true);
        return results;
    }

    private static void AssertHasErrorFor(List<ValidationResult> results, string memberName)
    {
        Assert.Contains(results, r => r.MemberNames.Contains(memberName));
    }
}