using PortManagement.Infrastructure.Security;

namespace PortManagement.IntegrationTests;

public sealed class PasswordRecoveryInfrastructureTests
{
    [Fact]
    public void ResetLinkUsesTheConfiguredOriginAndEscapesParameters()
    {
        var link = PasswordResetLinkBuilder.Build(
            "https://portal.example.com/",
            "10000000-0000-0000-0000-000000000001",
            "token/with+reserved=characters");

        Assert.Equal(
            "https://portal.example.com/redefinir-senha" +
            "?user=10000000-0000-0000-0000-000000000001" +
            "&token=token%2Fwith%2Breserved%3Dcharacters",
            link);
    }

    [Fact]
    public void RecoveryOptionsRejectAnUntrustedPublicUrlScheme()
    {
        var options = new PasswordRecoveryOptions
        {
            PublicWebUrl = "javascript:alert(1)"
        };

        var exception = Assert.Throws<InvalidOperationException>(options.Validate);

        Assert.Contains("HTTP ou HTTPS", exception.Message, StringComparison.Ordinal);
    }
}
