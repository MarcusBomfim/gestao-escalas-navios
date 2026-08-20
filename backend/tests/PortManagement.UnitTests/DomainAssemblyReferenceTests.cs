using PortManagement.Domain;

namespace PortManagement.UnitTests;

public sealed class DomainAssemblyReferenceTests
{
    [Fact]
    public void MarkerPointsToTheDomainAssembly()
    {
        var assemblyName = typeof(DomainAssemblyReference).Assembly.GetName().Name;

        Assert.Equal("PortManagement.Domain", assemblyName);
    }
}

