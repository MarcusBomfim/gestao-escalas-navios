using PortManagement.Domain.Common;
using PortManagement.Domain.Vessels;

namespace PortManagement.UnitTests;

public sealed class ImoNumberTests
{
    [Theory]
    [InlineData("IMO9074729")]
    [InlineData("9074729")]
    [InlineData("imo 9074729")]
    public void ParseAcceptsAndNormalizesAValidNumber(string value)
    {
        var imoNumber = ImoNumber.Parse(value);

        Assert.Equal("IMO9074729", imoNumber.Value);
    }

    [Theory]
    [InlineData("IMO9074728")]
    [InlineData("IMO123")]
    [InlineData("INVALID")]
    public void ParseRejectsAnInvalidNumber(string value)
    {
        var exception = Assert.Throws<DomainException>(() => ImoNumber.Parse(value));

        Assert.False(string.IsNullOrWhiteSpace(exception.Message));
    }
}
