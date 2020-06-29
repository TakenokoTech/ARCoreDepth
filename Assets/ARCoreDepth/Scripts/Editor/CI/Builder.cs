using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace ARCoreDepth.Scripts.Editor.CI
{
    public static class Builder
    {
        // ビルド対象シーンリスト
        private static readonly string[] SceneList =
        {
            "./Assets/ARCoreDepth/MainScene.unity"
        };

        // オプション
        private static readonly BuildOptions[] Options =
        {
            // 開発向け
            BuildOptions.Development,
            // 
            BuildOptions.ShowBuiltPlayer
        };
        
        [MenuItem("Project/Build Android")]
        public static void BuildAndroid()
        {
            Build(SceneList, "Android", BuildTarget.Android, "CrossPlatformMR.apk");
        }

        /**
         * ビルド
         */
        private static void Build(string[] sceneList, string outputDir, BuildTarget buildTarget, string fileName)
        {
            var option = Options.Aggregate(BuildOptions.None, (c, o) => c | o);
            var locationPathName = Directory.GetCurrentDirectory() + $"/App/{outputDir}/{fileName}";

            DeleteDir(Directory.GetCurrentDirectory() + $"/App/{outputDir}");
            var report = BuildPipeline.BuildPlayer(sceneList, locationPathName, buildTarget, option);

            if (report.summary.result == BuildResult.Failed)
                Debug.LogError("[Build] Error. " + report.summary.outputPath);
            else
                Debug.Log("[Build] Success. " + report.summary.outputPath);
        }

        /**
         * ディレクトリの再起削除
         */
        private static void DeleteDir(string targetDirectoryPath)
        {
            if (!Directory.Exists(targetDirectoryPath)) return;
            foreach (var filePath in Directory.GetFiles(targetDirectoryPath))
            {
                File.SetAttributes(filePath, FileAttributes.Normal);
                File.Delete(filePath);
            }

            foreach (var directoryPath in Directory.GetDirectories(targetDirectoryPath))
            {
                DeleteDir(directoryPath);
            }

            Directory.Delete(targetDirectoryPath, false);
        }
    }
}