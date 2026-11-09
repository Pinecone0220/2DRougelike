using UnityEditor;
using UnityEditor.Build.Reporting;
public static class DemoBuild {
 public static void Run() {
  var r=BuildPipeline.BuildPlayer(new BuildPlayerOptions {
   scenes=new[]{"Assets/Scenes/Title.unity","Assets/Scenes/Stage 1.unity","Assets/Scenes/Stage 2.unity","Assets/Scenes/Stage 3.unity"},
   locationPathName="../DemoVideo/build/Demo.exe", target=BuildTarget.StandaloneWindows64, options=BuildOptions.None });
  EditorApplication.Exit(r.summary.result==BuildResult.Succeeded?0:1);
 }
}
