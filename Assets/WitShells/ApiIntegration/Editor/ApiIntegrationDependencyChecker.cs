using UnityEditor;
using UnityEngine;
using System.IO;
using System.Linq;

[InitializeOnLoad]
public static class ApiIntegrationDependencyChecker
{
    private const string ManifestPath = "Packages/manifest.json";
    private const string DesignPatternsKey = "com.witshells.designpatterns";
    private const string DesignPatternsUrl = "https://github.com/Sulaiman281/Reusable-Unity-Scripts-Packages.git?path=Assets/WitShells/DesignPatterns";

    static ApiIntegrationDependencyChecker()
    {
        TryAddDesignPatternsDependency();
    }

    public static void TryAddDesignPatternsDependency()
    {
        string manifestFullPath = Path.Combine(Directory.GetCurrentDirectory(), ManifestPath);
        if (!File.Exists(manifestFullPath))
            return;

        string json = File.ReadAllText(manifestFullPath);
        if (json.Contains(DesignPatternsKey))
            return; // Already present

        int depIndex = json.IndexOf("\"dependencies\":");
        if (depIndex < 0)
            return;

        int braceIndex = json.IndexOf('{', depIndex);
        if (braceIndex < 0)
            return;

        // Insert our dependency after the opening brace of dependencies
        int insertIndex = braceIndex + 1;
        string toInsert = $"\n    \"{DesignPatternsKey}\": \"{DesignPatternsUrl}\",";

        json = json.Insert(insertIndex, toInsert);

        File.WriteAllText(manifestFullPath, json);
        Debug.Log("[ApiIntegration] Added DesignPatterns dependency to manifest.json. Unity will reload packages.");
        AssetDatabase.Refresh();
    }
}