namespace PortManagement.IntegrationTests;

public sealed class ApiBootstrapTests
{
    [Fact]
    public void ProgramEntryPointIsPublicForTheIntegrationTestHost()
    {
        Assert.True(typeof(Program).IsPublic);
    }
}

