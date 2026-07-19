using System;
using System.IO;
using System.Linq;
using System.Xml.Linq;
using Paramore.Darker.Validation;
using Shouldly;
using Xunit;

namespace Paramore.Darker.Validation.Tests;

public class CoreAssemblyDependencyTests
{
    [Fact]
    public void When_inspecting_core_assembly_should_have_no_provider_dependencies()
    {
        // Arrange — locate the core assembly via its published type QueryValidationError.
        // These are the assembly names the IL-reference check forbids (covers both netstandard2.0
        // and net8.0/net9.0, where DataAnnotations types live in System.ComponentModel.Annotations).
        var coreAssembly = typeof(QueryValidationError).Assembly;
        string[] forbiddenAssemblyNames =
        [
            "FluentValidation",
            "System.ComponentModel.DataAnnotations",
            "System.ComponentModel.Annotations"
        ];

        // These fragments are matched against Include= attributes in the csproj to catch
        // a declared-but-unused reference that the compiler would otherwise strip from IL.
        string[] forbiddenReferenceFragments = ["FluentValidation", "DataAnnotations"];

        // Act — collect all assembly names recorded in the compiled IL metadata
        var referencedNames = coreAssembly.GetReferencedAssemblies()
            .Select(a => a.Name ?? string.Empty)
            .ToArray();

        // Assert (IL check) — no forbidden provider assembly appears in the IL references.
        // Catches transitive leaks and any type usage, but has a blind spot: a declared-but-unused
        // package reference is omitted by the compiler and therefore invisible here.
        foreach (var forbidden in forbiddenAssemblyNames)
        {
            referencedNames.ShouldNotContain(forbidden);
        }

        // Arrange (csproj check) — locate the core project file relative to the test output directory.
        // AppContext.BaseDirectory = …/test/Paramore.Darker.Validation.Tests/bin/<config>/<tfm>/
        // Walking up five levels reaches the repository root.
        var repoRoot = Path.GetFullPath(
            Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", ".."));
        var csprojPath = Path.Combine(
            repoRoot,
            "src",
            "Paramore.Darker.Validation",
            "Paramore.Darker.Validation.csproj");

        // Act (csproj check) — parse the project file and extract all PackageReference /
        // ProjectReference Include values
        File.Exists(csprojPath).ShouldBeTrue($"Core csproj not found at '{csprojPath}'");
        var doc = XDocument.Load(csprojPath);
        var referenceIncludes = doc.Descendants()
            .Where(e => e.Name.LocalName is "PackageReference" or "ProjectReference")
            .Select(e => e.Attribute("Include")?.Value ?? string.Empty)
            .ToArray();

        // Assert (csproj check) — no declared reference contains a forbidden fragment.
        // Complements the IL check: catches a declared-but-unused package reference that the
        // compiler would otherwise strip from IL metadata (closing the trimming blind spot —
        // see review-tasks-round2.md Finding 2 / FR10).
        foreach (var fragment in forbiddenReferenceFragments)
        {
            var offenders = referenceIncludes
                .Where(include => include.Contains(fragment, StringComparison.OrdinalIgnoreCase))
                .ToArray();

            offenders.ShouldBeEmpty(
                $"Core csproj must not declare a reference containing '{fragment}' (FR10: dependency-free core). Found: {string.Join(", ", offenders)}");
        }
    }
}
