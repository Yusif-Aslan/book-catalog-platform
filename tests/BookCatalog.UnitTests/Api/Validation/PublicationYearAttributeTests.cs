using BookCatalog.Api.Validation;

namespace BookCatalog.UnitTests.Api.Validation;

public class PublicationYearAttributeTests
{
    private readonly PublicationYearAttribute _attribute = new();

    [Theory]
    [InlineData(PublicationYearAttribute.MinYear)]
    [InlineData(2000)]
    public void IsValid_WithYearInRange_ReturnsTrue(int year)
    {
        Assert.True(_attribute.IsValid(year));
    }

    [Fact]
    public void IsValid_WithCurrentYear_ReturnsTrue()
    {
        Assert.True(_attribute.IsValid(DateTime.UtcNow.Year));
    }

    [Fact]
    public void IsValid_WithYearBeforeMinYear_ReturnsFalse()
    {
        Assert.False(_attribute.IsValid(PublicationYearAttribute.MinYear - 1));
    }

    [Fact]
    public void IsValid_WithNextYear_ReturnsFalse()
    {
        Assert.False(_attribute.IsValid(DateTime.UtcNow.Year + 1));
    }

    [Fact]
    public void IsValid_WithNonIntegerValue_ReturnsFalse()
    {
        Assert.False(_attribute.IsValid("2000"));
    }
}