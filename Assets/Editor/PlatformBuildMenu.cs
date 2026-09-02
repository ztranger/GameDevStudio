using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace GameDevStudio.EditorTools
{
    public static class PlatformBuildMenu
    {
        [MenuItem("Hpg/Build WebGL")]
        public static void BuildWebGl()
        {
            Build(BuildTarget.WebGL, "Builds/WebGL");
        }

        [MenuItem("Hpg/Build Android APK")]
        public static void BuildAndroid()
        {
            Build(BuildTarget.Android, "Builds/Android/GameDevStudio.apk");
        }

        static void Build(BuildTarget target, string location)
        {
            string dir = Path.GetDirectoryName(location);
            if (!string.IsNullOrEmpty(dir))
            {
                Directory.CreateDirectory(dir);
            }

            string[] scenes = EditorBuildSettings.scenes.Where(scene => scene.enabled).Select(scene => scene.path).ToArray();
            var options = new BuildPlayerOptions
            {
                scenes = scenes,
                locationPathName = location,
                target = target,
                options = BuildOptions.CompressWithLz4HC
            };

            BuildReport report = BuildPipeline.BuildPlayer(options);
            if (report.summary.result == BuildResult.Succeeded)
            {
                Debug.Log("Сборка готова: " + location + "  (" + report.summary.totalSize + " байт)");
                EditorUtility.RevealInFinder(location);
            }
            else
            {
                Debug.LogError("Сборка не удалась: " + report.summary.result);
            }
        }
    }
}
