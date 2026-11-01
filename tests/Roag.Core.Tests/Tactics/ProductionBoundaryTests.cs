using Roag.Core.World;
using Xunit;

namespace Roag.Core.Tests.Tactics;

public sealed class ProductionBoundaryTests
{
    [Fact]
    public void ProductionAssembliesExcludeGeneratedRulesAndTheirReferenceRuntime()
    {
        var core = typeof(WorldSession).Assembly;
        Assert.DoesNotContain(core.GetTypes(), type => type.Namespace?.StartsWith("Roag.Core.Rules", StringComparison.Ordinal) == true);
        Assert.DoesNotContain(core.GetReferencedAssemblies(), assembly => assembly.Name == "Roag.Reference");
        var client = System.Reflection.Assembly.Load("Roag.Client");
        Assert.DoesNotContain(client.GetReferencedAssemblies(), assembly => assembly.Name == "Roag.Reference");
    }
}
