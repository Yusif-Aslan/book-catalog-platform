using BookCatalog.Api.Validation;

namespace BookCatalog.UnitTests.Api.Validation;

public class IsbnAttributeTests
{
    private readonly IsbnAttribute _attribute = new();

    [Theory]
    [InlineData("9780132350884")]
    [InlineData("978-0-13-235088-4")]
    [InlineData("978 0 13 235088 4")]
    [InlineData("020161622X")]
    [InlineData("0-201-61622-x")]
    public void IsValid_WithValidIsbn_ReturnsTrue(string isbn)
    {
        Assert.True(_attribute.IsValid(isbn));
    }

    [Fact]
    public void IsValid_WithNull_ReturnsTrueBecauseIsbnIsOptional()
    {
        Assert.True(_attribute.IsValid(null));
    }

    [Theory]
    [InlineData("9780132350885")]
    [InlineData("0201616220")]
    public void IsValid_WithWrongCheckDigit_ReturnsFalse(string isbn)
    {
        Assert.False(_attribute.IsValid(isbn));
    }

    [Theory]
    [InlineData("978013235088X")]
    [InlineData("X201616220")]
    [InlineData("97801323508AB")]
    public void IsValid_WithInvalidCharacters_ReturnsFalse(string isbn)
    {
        Assert.False(_attribute.IsValid(isbn));
    }

    [Theory]
    [InlineData("")]
    [InlineData("12345")]
    [InlineData("97801323508841")]
    public void IsValid_WithWrongLength_ReturnsFalse(string isbn)
    {
        Assert.False(_attribute.IsValid(isbn));
    }

    [Fact]
    public void IsValid_WithNonStringValue_ReturnsFalse()
    {
        Assert.False(_attribute.IsValid(9780132350884L));
    }
}