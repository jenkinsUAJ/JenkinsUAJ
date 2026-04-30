using System.IO;
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

    [MenuItem("Build/Build Windows Player With Readme")]
    public static void BuildWindowsPlayer()
    {
            // Define build options
            string path =GetArg("-buildPath");/* EditorUtility.SaveFolderPanel("Choose Location of Built Game", "", "");*/

        var buildOptions = new BuildPlayerOptions()
        {
            // Adjust scene list based on your project
            scenes = new string[] { "Assets/Scenes/FinalMenus/MainMenu.unity"},
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

