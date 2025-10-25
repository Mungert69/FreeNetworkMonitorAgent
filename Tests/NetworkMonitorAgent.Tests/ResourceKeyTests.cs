using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Xml.Linq;
using Xunit;

namespace NetworkMonitorAgent.Tests;

public class ResourceKeyTests
{
    private static readonly XNamespace XNamespace = "http://schemas.microsoft.com/winfx/2009/xaml";

    private static readonly string SolutionRoot = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", ".."));

    public static IEnumerable<object[]> PageXamlFiles =>
        new[]
        {
            "AppShell.xaml",
            "ChatPage.xaml",
            "ConfigPage.xaml",
            "DataViewPage.xaml",
            "DetailsPage.xaml",
            "ExitPage.xaml",
            "MainPage.xaml",
            "NetworkMonitorPage.xaml",
            "ScanPage.xaml",
            "SetupGuidePage.xaml",
            Path.Combine("Views", "CustomPopupView.xaml"),
            Path.Combine("Views", "ProcessorStatesView.xaml"),
            Path.Combine("Views", "ShowDetailsPopup.xaml"),
        }.Select(path => new object[] { path });

    [Theory]
    [MemberData(nameof(PageXamlFiles))]
    public void Page_ReferencesExistingResources(string relativePath)
    {
        var keys = LoadDefinedResourceKeys();
        var fullPath = Path.Combine(SolutionRoot, relativePath);
        var referencedKeys = ExtractReferencedResourceKeys(fullPath, out var localKeys);

        AssertMissingKeys(referencedKeys, keys, localKeys, relativePath.Replace(Path.DirectorySeparatorChar, '/'));
    }

    private static HashSet<string> LoadDefinedResourceKeys()
    {
        var keys = new HashSet<string>(StringComparer.Ordinal);

        foreach (var path in EnumerateResourceDictionaries())
        {
            if (!File.Exists(path))
            {
                continue;
            }

            var document = XDocument.Load(path);
            foreach (var element in document.Descendants())
            {
                var key = element.Attribute(XNamespace + "Key")?.Value;
                if (!string.IsNullOrWhiteSpace(key))
                {
                    keys.Add(key);
                }
            }
        }

        return keys;
    }

    private static IEnumerable<string> EnumerateResourceDictionaries()
    {
        yield return Path.Combine(SolutionRoot, "App.xaml");

        var resourcesDirectory = Path.Combine(SolutionRoot, "Resources");
        if (Directory.Exists(resourcesDirectory))
        {
            foreach (var path in Directory.EnumerateFiles(resourcesDirectory, "*.xaml", SearchOption.AllDirectories))
            {
                yield return path;
            }
        }
    }

    private static HashSet<string> ExtractReferencedResourceKeys(string xamlPath, out HashSet<string> localKeys)
    {
        localKeys = new HashSet<string>(StringComparer.Ordinal);
        var keys = new HashSet<string>(StringComparer.Ordinal);

        var xaml = File.ReadAllText(xamlPath);
        var document = XDocument.Load(xamlPath);

        foreach (var element in document.Descendants())
        {
            var key = element.Attribute(XNamespace + "Key")?.Value;
            if (!string.IsNullOrWhiteSpace(key))
            {
                localKeys.Add(key);
            }
        }

        var regex = new Regex(@"\b(?:StaticResource|DynamicResource)\s+(?<key>[\w\.]+)", RegexOptions.Compiled);

        foreach (Match match in regex.Matches(xaml))
        {
            var key = match.Groups["key"].Value;
            if (!string.IsNullOrWhiteSpace(key))
            {
                keys.Add(key);
            }
        }

        return keys;
    }

    private static void AssertMissingKeys(IEnumerable<string> referencedKeys, HashSet<string> definedKeys, HashSet<string> localKeys, string fileName)
    {
        var missing = referencedKeys
            .Where(key => !definedKeys.Contains(key) && !localKeys.Contains(key))
            .OrderBy(key => key, StringComparer.Ordinal)
            .ToList();

        Assert.True(
            missing.Count == 0,
            $"The following resource keys referenced in {fileName} are not defined: {string.Join(", ", missing)}");
    }
}
