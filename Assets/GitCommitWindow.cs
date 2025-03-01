using System.Diagnostics;
using UnityEditor;
using UnityEngine;

public class GitCommitWindow : EditorWindow
{
    private string commitMessage = "Update";

    [MenuItem("Tools/Git/Commit & Push")]
    public static void ShowWindow()
    {
        GetWindow<GitCommitWindow>("Git Commit");
    }

    private void OnGUI()
    {
        GUILayout.Label("Commit to Git", EditorStyles.boldLabel);
        commitMessage = EditorGUILayout.TextField("Commit Message", commitMessage);

        if (GUILayout.Button("Commit & Push"))
        {
            RunGitCommand("add .");  // Add all changes
            RunGitCommand($"commit -am \"{commitMessage}\"");
            RunGitCommand("push origin main");
            UnityEngine.Debug.Log("Committed & Pushed to GitHub.");
        }
    }

    private static void RunGitCommand(string command)
    {
        ProcessStartInfo startInfo = new ProcessStartInfo
        {
            FileName = "git",
            Arguments = command,
            RedirectStandardOutput = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        using (Process process = Process.Start(startInfo))
        {
            using (var reader = process.StandardOutput)
            {
                string result = reader.ReadToEnd();
                UnityEngine.Debug.Log(result);
            }
        }
    }
}
