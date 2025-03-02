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
    public static string WebSocketAddress = "ws://localhost:8080";
    [MenuItem("Window/Open Version Control & Realtime Sync")]
    public static void ShowWindow()
    {
        GetWindow<WebSocketGitEditor>("Version Control & Realtime Sync");
    }
    [MenuItem("Window/Initialize Version Control")]
    public static void Init()
    {
        GetWindow<InitializeWindow>("Initialize Version Control");
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
            websocket = new WebSocket(WebSocketAddress);

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

    public static void RunGitCommand(string command)
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
public class InitializeWindow : EditorWindow
{
    private static string tempText;
    private static string userName;
    private static string email;
    private static string personalAccessToken;
    private static string link;

    private void OnGUI()
    {
        GUILayout.Label("Web Socket Adress", EditorStyles.boldLabel);
        tempText = EditorGUILayout.TextField("ws://localhost:8080");
        if (GUILayout.Button("Set Address")) WebSocketGitEditor.WebSocketAddress = tempText;
        GUILayout.Space(10);
        GUILayout.Label("Git Config", EditorStyles.boldLabel);
        userName = EditorGUILayout.TextField("Username",userName);
        email = EditorGUILayout.TextField("Email",email);
        personalAccessToken = EditorGUILayout.TextField("Personal Access Token",personalAccessToken);
        link = EditorGUILayout.TextField("Repository URL",link);
        if (GUILayout.Button("Initialize Git")) {
            WebSocketGitEditor.RunGitCommand("init");
            WebSocketGitEditor.RunGitCommand("add .");
            WebSocketGitEditor.RunGitCommand("commit -m \"Initial commit\"");
            WebSocketGitEditor.RunGitCommand($"remote add origin {link}");
            WebSocketGitEditor.RunGitCommand("branch -M main");
            // Store GitHub credentials (replace with your username and PAT)
            WebSocketGitEditor.RunGitCommand($"config --global credential.helper store");
            WebSocketGitEditor.RunGitCommand($"config --global user.name \"{userName}\"");
            WebSocketGitEditor.RunGitCommand($"config --global user.email \"{email}\"");
            WebSocketGitEditor.RunGitCommand($"config --global user.password \"{personalAccessToken}\"");
            // Push with authentication
            WebSocketGitEditor.RunGitCommand("pull origin main --rebase");
            WebSocketGitEditor.RunGitCommand("push -u origin main");
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
