#region Licence
/* The MIT License (MIT)
Copyright © 2026 Ian Cooper <ian_hammond_cooper@yahoo.co.uk>

Permission is hereby granted, free of charge, to any person obtaining a copy
of this software and associated documentation files (the "Software"), to deal
in the Software without restriction, including without limitation the rights
to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
copies of the Software, and to permit persons to whom the Software is
furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in
all copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
THE SOFTWARE. */
#endregion

using System;
using System.IO;
using System.Linq;
using System.Xml.Linq;
using Shouldly;
using Xunit;

namespace Paramore.Darker.Caching.Tests;

public class CachingAssemblyDependencyTests
{
    [Fact]
    public void When_inspecting_caching_assembly_should_have_no_otel_or_metrics_dependency()
    {
        // Arrange — locate the caching assembly via its key public type.
        // The caching package must not reference any OpenTelemetry assembly; it records
        // cache outcomes purely as a core Activity span attribute (ADR 0021, FR7, FR10).
        var cachingAssembly = typeof(CacheableQueryDecoratorAsync<,>).Assembly;

        // Act — collect all assembly names recorded in the compiled IL metadata.
        var referencedNames = cachingAssembly.GetReferencedAssemblies()
            .Select(a => a.Name ?? string.Empty)
            .ToArray();

        // Assert (no OpenTelemetry) — no referenced assembly name starts with "OpenTelemetry".
        // This is the load-bearing guard: the caching package must not bring in any OTel
        // assembly. The metrics-from-traces subsystem (ADR 0018) lives in
        // Paramore.Darker.Extensions.Diagnostics, not in the caching package.
        var otelAssemblies = referencedNames
            .Where(name => name.StartsWith("OpenTelemetry", StringComparison.OrdinalIgnoreCase))
            .ToArray();

        otelAssemblies.ShouldBeEmpty(
            $"Paramore.Darker.Caching must not reference any OpenTelemetry assembly (ADR 0021). " +
            $"Found: {string.Join(", ", otelAssemblies)}");

        // Assert (positive controls) — the guard is inspecting the right assembly.
        // These confirm the assembly is really the caching package, not a framework assembly.
        // Note: HybridCache types are compiled into Microsoft.Extensions.Caching.Abstractions
        // (not the NuGet package name "Microsoft.Extensions.Caching.Hybrid") in the IL metadata.
        referencedNames.ShouldContain(
            "Paramore.Darker",
            "Expected Paramore.Darker.Caching to reference the Darker core assembly.");

        referencedNames.ShouldContain(
            "Microsoft.Extensions.Caching.Abstractions",
            "Expected Paramore.Darker.Caching to reference Microsoft.Extensions.Caching.Abstractions " +
            "(where HybridCache types live in compiled IL).");

        // Arrange (csproj check) — locate the caching project file relative to the test output.
        // AppContext.BaseDirectory = …/test/Paramore.Darker.Caching.Tests/bin/<config>/<tfm>/
        // Walking up five levels reaches the repository root.
        var repoRoot = Path.GetFullPath(
            Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", ".."));
        var csprojPath = Path.Combine(
            repoRoot,
            "src",
            "Paramore.Darker.Caching",
            "Paramore.Darker.Caching.csproj");

        // Act (csproj check) — parse the project file and extract all PackageReference /
        // ProjectReference Include values.
        File.Exists(csprojPath).ShouldBeTrue($"Caching csproj not found at '{csprojPath}'");
        var doc = XDocument.Load(csprojPath);
        var referenceIncludes = doc.Descendants()
            .Where(e => e.Name.LocalName is "PackageReference" or "ProjectReference")
            .Select(e => e.Attribute("Include")?.Value ?? string.Empty)
            .ToArray();

        // Assert (csproj positive control) — the csproj declares the expected caching package.
        // Microsoft.Extensions.Caching.Hybrid is the NuGet package name (even though its types
        // compile into Microsoft.Extensions.Caching.Abstractions in the IL).
        referenceIncludes.ShouldContain(
            "Microsoft.Extensions.Caching.Hybrid",
            "Expected Paramore.Darker.Caching.csproj to declare a PackageReference to Microsoft.Extensions.Caching.Hybrid.");

        // Assert (csproj check) — no declared reference contains "OpenTelemetry".
        // Complements the IL check: catches a declared-but-unused package reference that the
        // compiler would otherwise strip from IL metadata.
        var otelReferences = referenceIncludes
            .Where(include => include.StartsWith("OpenTelemetry", StringComparison.OrdinalIgnoreCase))
            .ToArray();

        otelReferences.ShouldBeEmpty(
            $"Paramore.Darker.Caching csproj must not declare an OpenTelemetry reference (ADR 0021). " +
            $"Found: {string.Join(", ", otelReferences)}");
    }
}
