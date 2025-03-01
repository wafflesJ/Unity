using System.Diagnostics;
using UnityEditor;
using UnityEngine;

public class GitCommitWindow : EditorWindow
{
    private string commitMessage = "Update";

    [MenuItem("Window/Git Commit & Push")]
    public static void ShowWindow()
    {
        GitCommitWindow window = GetWindow<GitCommitWindow>("Git Sync",true);
        window.minSize = new Vector2(300, 150);
    }

    private void OnGUI()
    {
        GUILayout.Label("Git Version Control", EditorStyles.boldLabel);
        GUILayout.Space(5);

        EditorGUILayout.HelpBox("Enter a commit message and push changes to GitHub.", MessageType.Info);

        commitMessage = EditorGUILayout.TextField("Commit Message", commitMessage);
        GUILayout.Space(10);

        if (GUILayout.Button("Commit & Push"))
        {
            RunGitCommand("add .");
            RunGitCommand($"commit -m \"{commitMessage}\"");
            RunGitCommand("push origin main");
            UnityEngine.Debug.Log("Committed & Pushed to GitHub.");
        }

        GUILayout.Space(10);
        if (GUILayout.Button("Pull from GitHub"))
        {
            RunGitCommand("pull origin main");
            UnityEngine.Debug.Log("Pulled latest changes from GitHub.");
        }
    }

    private static void RunGitCommand(string command)
    {
        ProcessStartInfo startInfo = new ProcessStartInfo
        {
            FileName = "git",
            Arguments = command,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
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
            using (var errorReader = process.StandardError)
            {
                string error = errorReader.ReadToEnd();
                if (!string.IsNullOrEmpty(error))
                {
                    UnityEngine.Debug.LogError(error);
                }
            }
        }
    }
}
