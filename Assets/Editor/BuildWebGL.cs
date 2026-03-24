using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build.Reporting;

public static class BuildWebGL
{
    private const string DefaultOutputPath = "Builds/WebGL";

    public static void PerformBuild()
    {
        string outputPath = GetOutputPath();
        string[] enabledScenes = EditorBuildSettings.scenes
            .Where(scene => scene.enabled)
            .Select(scene => scene.path)
            .ToArray();

        if (enabledScenes.Length == 0)
        {
            throw new InvalidOperationException("No enabled scenes found in EditorBuildSettings.");
        }

        Directory.CreateDirectory(outputPath);

        BuildPlayerOptions buildOptions = new BuildPlayerOptions
        {
            scenes = enabledScenes,
            locationPathName = outputPath,
            target = BuildTarget.WebGL,
            options = BuildOptions.None,
        };

        BuildReport report = BuildPipeline.BuildPlayer(buildOptions);

        if (report.summary.result != BuildResult.Succeeded)
        {
            throw new InvalidOperationException(
                $"WebGL build failed with result {report.summary.result}."
            );
        }

        Console.WriteLine($"WebGL build succeeded: {Path.GetFullPath(outputPath)}");
    }

    private static string GetOutputPath()
    {
        string customPath = Environment.GetEnvironmentVariable("UNITY_WEBGL_BUILD_PATH");
        return string.IsNullOrWhiteSpace(customPath) ? DefaultOutputPath : customPath.Trim();
    }
}
