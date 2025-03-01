using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using UnityEditor;
using UnityEngine;
using NativeWebSocket;

public class WebSocketGitEditor : EditorWindow
{
    private static WebSocket websocket;
    private static bool isConnected = false;
    private string commitMessage = "Update";
    private static Dictionary<string, TransformUpdate> lastKnownTransforms = new Dictionary<string, TransformUpdate>();
    
    [MenuItem("Window/WebSocket & Git Sync")]
    public static void ShowWindow()
    {
        GetWindow<WebSocketGitEditor>("WebSocket & Git Sync");
    }

    private void OnGUI()
    {
        GUILayout.Label("WebSocket Sync", EditorStyles.boldLabel);
        if (!isConnected)
        {
            if (GUILayout.Button("Connect")) ConnectToWebSocket();
        }
        else
        {
            if (GUILayout.Button("Disconnect")) DisconnectFromWebSocket();
        }
        
        GUILayout.Space(10);
        GUILayout.Label("Git Version Control", EditorStyles.boldLabel);
        commitMessage = EditorGUILayout.TextField("Commit Message", commitMessage);
        
        if (GUILayout.Button("Sync Files"))
        {
            RunGitCommand("add .");
            RunGitCommand($"commit -m \"{commitMessage}\"");
            RunGitCommand("pull origin main --rebase");
            RunGitCommand("push origin main");
            AssetDatabase.Refresh();

        }
        
        //if (GUILayout.Button("Pull from GitHub")) 
    }

    private static void ConnectToWebSocket()
    {
        if (!isConnected)
        {
            websocket = new WebSocket("ws://localhost:8080");

            websocket.OnOpen += () => { isConnected = true; };            
            websocket.OnMessage += (bytes) => ApplyUpdate(Encoding.UTF8.GetString(bytes));
            websocket.OnClose += (e) => isConnected = false;

            websocket.Connect();
        }
    }

    private static void DisconnectFromWebSocket()
    {
        if (isConnected)
        {
            isConnected = false;
            websocket.Close();
        }
    }

    private static void ApplyUpdate(string data)
    {
        TransformUpdate update = JsonUtility.FromJson<TransformUpdate>(data);
        GameObject obj = GameObject.Find(update.objectName);
        if (obj != null)
        {
            Undo.RecordObject(obj.transform, "Sync Transform");
            obj.transform.position = update.position;
            obj.transform.rotation = Quaternion.Euler(update.rotation);
            EditorUtility.SetDirty(obj);
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
            process.StandardOutput.ReadToEnd();
            string error = process.StandardError.ReadToEnd();
            //if (!string.IsNullOrEmpty(error)) UnityEngine.Debug.LogError(error);
        }
    }
}

[Serializable]
public class TransformUpdate
{
    public string objectName;
    public Vector3 position;
    public Vector3 rotation;
}
