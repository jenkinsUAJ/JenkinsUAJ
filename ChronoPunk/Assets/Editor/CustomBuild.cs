using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

public class CustomBuild
{

    private static string GetArg(string name)
    {
        var args = System.Environment.GetCommandLineArgs();
        for (int i = 0; i < args.Length; i++)
        {
            if (args[i] == name && args.Length > i + 1)
            {
                return args[i + 1];
            }
        }
        return null;
    }

    private static string[] FillLevels()
    {
        return (from scene in EditorBuildSettings.scenes where scene.enabled select scene.path).ToArray();
    }

    [MenuItem("Build/Build Windows Player With Readme")]
    public static void BuildWindowsPlayer()
    {
        Debug.Log(EditorBuildSettingsScene.GetActiveSceneList(EditorBuildSettings.scenes));
            // Define build options
        string path =GetArg("-buildPath");/* EditorUtility.SaveFolderPanel("Choose Location of Built Game", "", "");*/

        if(path  == null)
        {
            path = "../Build";
        }


        var buildOptions = new BuildPlayerOptions()
        {
            // Adjust scene list based on your project
            scenes = FillLevels(),
            locationPathName = path + "/MyGame.exe",
            target = BuildTarget.StandaloneWindows64,
            options = BuildOptions.AutoRunPlayer
        };

        // Build the Player
        var buildReport = BuildPipeline.BuildPlayer(buildOptions);

        if (buildReport.summary.result != UnityEditor.Build.Reporting.BuildResult.Succeeded)
        {
            Debug.Log("Build failed!\n\n" + buildReport.SummarizeErrors());
            return;
        }

        // Post-process: Copy README file to the build folder
        //File.Copy("Assets/Documentation/README.txt", path + "/README.txt", true);
    }
}

