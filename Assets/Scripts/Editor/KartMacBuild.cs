using System;
using System.IO;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace BendStarKart.Editor
{
    public static class KartMacBuild
    {
        private const string RaceScenePath = "Assets/Scenes/StarloopRace.unity";

        [MenuItem("Tools/Kart/Build macOS Universal")]
        public static void BuildMacUniversal()
        {
            // 正式赛道由生成菜单创建；缺少场景时给出明确的准备步骤。
            if (AssetDatabase.LoadAssetAtPath<SceneAsset>(RaceScenePath) == null)
                throw new InvalidOperationException(
                    "请先执行 Tools/Kart/Generate Race Track，再构建 macOS 应用。");

            // Unity 6 的 macOS 架构由 OSXStandalone 设置控制，x64ARM64 才是 Universal。
            UnityEditor.OSXStandalone.UserBuildSettings.architecture = OSArchitecture.x64ARM64;
            PlayerSettings.productName = "BendStarKart";
            PlayerSettings.SetApplicationIdentifier(NamedBuildTarget.Standalone,
                "com.kelvinxie.bendstarkart");

            // 输出固定在 Unity 项目根目录，避免编辑器的工作目录影响结果。
            string outputPath = Path.GetFullPath(Path.Combine(
                Application.dataPath, "..", "Builds", "macOS", "BendStarKart.app"));
            Directory.CreateDirectory(Path.GetDirectoryName(outputPath));
            var options = new BuildPlayerOptions
            {
                scenes = new[] { RaceScenePath },
                locationPathName = outputPath,
                target = BuildTarget.StandaloneOSX,
                options = BuildOptions.None
            };

            BuildReport report = BuildPipeline.BuildPlayer(options);
            if (report.summary.result != BuildResult.Succeeded)
                throw new InvalidOperationException(
                    "弯星卡丁 macOS Universal 构建失败，请查看 Unity Console 和 Editor.log。");

            Debug.Log("弯星卡丁 macOS Universal 构建完成：" + outputPath);
        }
    }
}
