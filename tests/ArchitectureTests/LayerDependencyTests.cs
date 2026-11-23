using NetArchTest.Rules;
using Orders.Domain;

namespace ArchitectureTests;

/// <summary>
/// Clean Architecture dependency rule, enforced by the build: inner layers never reference outer layers
/// or infrastructure frameworks.
/// </summary>
public class LayerDependencyTests
{
    private const string Application = "Orders.Application";
    private const string Infrastructure = "Orders.Infrastructure";
    private const string Api = "Orders.Api";

    private static readonly System.Reflection.Assembly DomainAssembly = typeof(Order).Assembly;
    private static readonly System.Reflection.Assembly ApplicationAssembly = typeof(Orders.Application.Result).Assembly;
    private static readonly System.Reflection.Assembly InfrastructureAssembly = typeof(Orders.Infrastructure.DependencyInjection).Assembly;

    [Fact]
    public void Domain_depends_on_nothing_else() =>
        AssertNoDependency(DomainAssembly, Application, Infrastructure, Api, "Microsoft.EntityFrameworkCore", "Dapper", "RabbitMQ", "Ecommerce");

    [Fact]
    public void Application_does_not_depend_on_infrastructure_or_frameworks() =>
        AssertNoDependency(ApplicationAssembly, Infrastructure, Api, "Microsoft.EntityFrameworkCore", "Dapper", "RabbitMQ", "Ecommerce.Messaging", "Microsoft.AspNetCore");

    [Fact]
    public void Infrastructure_does_not_depend_on_the_api() =>
        AssertNoDependency(InfrastructureAssembly, Api);

    [Fact]
    public void Domain_classes_are_sealed()
    {
        var result = Types.InAssembly(DomainAssembly).That().AreClasses().And().AreNotAbstract().Should().BeSealed().GetResult();

        Assert.True(result.IsSuccessful, Describe(result));
    }

    private static void AssertNoDependency(System.Reflection.Assembly assembly, params string[] forbidden)
    {
        var result = Types.InAssembly(assembly).ShouldNot().HaveDependencyOnAny(forbidden).GetResult();

        Assert.True(result.IsSuccessful, Describe(result));
    }

    private static string Describe(NetArchTest.Rules.TestResult result) =>
        "Violations: " + string.Join(", ", result.FailingTypeNames ?? []);
}
