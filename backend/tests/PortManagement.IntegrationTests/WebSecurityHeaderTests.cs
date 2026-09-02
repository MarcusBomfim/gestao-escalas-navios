using System.Text.RegularExpressions;

namespace PortManagement.IntegrationTests;

/// <summary>
/// No nginx, um bloco <c>location</c> que declara o próprio <c>add_header</c>
/// deixa de herdar os cabeçalhos declarados no bloco <c>server</c>. É um
/// comportamento silencioso: nada falha, os arquivos passam a ser servidos sem
/// proteção. Estes testes garantem que todo <c>location</c> inclua o arquivo
/// compartilhado de cabeçalhos.
/// </summary>
public sealed partial class WebSecurityHeaderTests
{
    private static readonly string NginxDirectory = LocateNginxDirectory();

    private static string ServerConfiguration =>
        File.ReadAllText(Path.Combine(NginxDirectory, "default.conf"));

    private static string HeaderTemplate =>
        File.ReadAllText(Path.Combine(NginxDirectory, "security-headers.conf.template"));

    [Fact]
    public void EveryLocationIncludesTheSharedSecurityHeaders()
    {
        var blocks = LocationBlocks(ServerConfiguration);

        Assert.NotEmpty(blocks);

        foreach (var block in blocks)
        {
            // O bloco que apenas nega acesso não serve conteúdo algum.
            if (block.Body.Contains("deny all", StringComparison.Ordinal))
            {
                continue;
            }

            Assert.Contains("security-headers.conf", block.Body, StringComparison.Ordinal);
        }
    }

    [Theory]
    [InlineData("X-Content-Type-Options")]
    [InlineData("Referrer-Policy")]
    [InlineData("X-Frame-Options")]
    [InlineData("Permissions-Policy")]
    [InlineData("Cross-Origin-Opener-Policy")]
    [InlineData("Content-Security-Policy")]
    public void SharedHeadersDeclareTheExpectedProtections(string header)
    {
        Assert.Contains(header, HeaderTemplate, StringComparison.Ordinal);
    }

    [Fact]
    public void EveryHeaderIsMarkedAlwaysSoItSurvivesErrorResponses()
    {
        var declarations = HeaderTemplate
            .Split('\n')
            .Select(line => line.Trim())
            .Where(line => line.StartsWith("add_header", StringComparison.Ordinal));

        Assert.NotEmpty(declarations);
        Assert.All(
            declarations,
            line => Assert.EndsWith("always;", line, StringComparison.Ordinal));
    }

    [Theory]
    [InlineData("default-src 'self'")]
    [InlineData("frame-ancestors 'none'")]
    [InlineData("object-src 'none'")]
    [InlineData("base-uri 'self'")]
    public void ContentSecurityPolicyLocksDownTheRiskyDirectives(string directive)
    {
        Assert.Contains(directive, HeaderTemplate, StringComparison.Ordinal);
    }

    [Fact]
    public void ContentSecurityPolicyDoesNotAllowInlineOrEvaluatedScript()
    {
        var policy = HeaderTemplate[HeaderTemplate.IndexOf(
            "Content-Security-Policy",
            StringComparison.Ordinal)..];
        var scriptSource = policy[policy.IndexOf("script-src", StringComparison.Ordinal)..];
        scriptSource = scriptSource[..scriptSource.IndexOf(';', StringComparison.Ordinal)];

        Assert.DoesNotContain("unsafe-inline", scriptSource, StringComparison.Ordinal);
        Assert.DoesNotContain("unsafe-eval", scriptSource, StringComparison.Ordinal);
    }

    [Fact]
    public void ServerVersionIsNotAdvertised()
    {
        Assert.Contains("server_tokens off", ServerConfiguration, StringComparison.Ordinal);
    }

    private static List<(string Selector, string Body)> LocationBlocks(string configuration)
    {
        var blocks = new List<(string Selector, string Body)>();

        foreach (Match match in LocationPattern().Matches(configuration))
        {
            blocks.Add((match.Groups["selector"].Value.Trim(), match.Groups["body"].Value));
        }

        return blocks;
    }

    [GeneratedRegex(
        @"location\s+(?<selector>[^{]+)\{(?<body>[^}]*)\}",
        RegexOptions.Singleline)]
    private static partial Regex LocationPattern();

    private static string LocateNginxDirectory()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);

        while (directory is not null)
        {
            var candidate = Path.Combine(directory.FullName, "infrastructure", "nginx");
            if (Directory.Exists(candidate))
            {
                return candidate;
            }

            directory = directory.Parent;
        }

        throw new DirectoryNotFoundException(
            "Não foi possível localizar infrastructure/nginx a partir do diretório de testes.");
    }
}
