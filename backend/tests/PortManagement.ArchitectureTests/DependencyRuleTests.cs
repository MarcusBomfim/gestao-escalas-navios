using System.Reflection;
using PortManagement.Application;
using PortManagement.Domain;
using PortManagement.Infrastructure;

namespace PortManagement.ArchitectureTests;

public sealed class DependencyRuleTests
{
    [Fact]
    public void DomainDoesNotReferenceOuterLayers()
    {
        var references = GetProjectReferences(typeof(DomainAssemblyReference).Assembly);

        Assert.Empty(references);
    }

    [Fact]
    public void ApplicationDoesNotReferenceOuterLayers()
    {
        var references = GetProjectReferences(typeof(ApplicationAssemblyReference).Assembly);

        Assert.DoesNotContain("PortManagement.Infrastructure", references);
        Assert.DoesNotContain("PortManagement.Api", references);
    }

    [Fact]
    public void InfrastructureDoesNotReferenceTheApiLayer()
    {
        var references = GetProjectReferences(typeof(InfrastructureAssemblyReference).Assembly);

        Assert.DoesNotContain("PortManagement.Api", references);
    }

    private static string[] GetProjectReferences(Assembly assembly)
    {
        return assembly
            .GetReferencedAssemblies()
            .Select(reference => reference.Name)
            .OfType<string>()
            .Where(name => name.StartsWith("PortManagement.", StringComparison.Ordinal))
            .Order(StringComparer.Ordinal)
            .ToArray();
    }
}
