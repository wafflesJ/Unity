using System;
using System.Collections.Generic;
using System.Text;
using UnityEditor;
using UnityEngine;
using NativeWebSocket;

[InitializeOnLoad]
public class WebSocketManager : MonoBehaviour
{
    private static WebSocket websocket;
    private static bool isConnected = false;

    // Dictionary to track last known transforms
    private static Dictionary<string, TransformUpdate> lastKnownTransforms = new Dictionary<string, TransformUpdate>();

    static WebSocketManager()
    {
        // Listen for changes in the scene hierarchy
        EditorApplication.hierarchyChanged += OnHierarchyChanged;
        EditorApplication.update += EditorUpdate; // Listen to Editor updates for real-time changes
    }

    // Connect to the WebSocket server
    [MenuItem("Tools/Connect to WebSocket Server")]
    public static void ConnectToWebSocket()
    {
        if (!isConnected)
        {
            Debug.Log("Connecting to WebSocket server...");
            websocket = new WebSocket("ws://localhost:8080");
            websocket.OnMessage += (bytes) =>
            {
                string message = Encoding.UTF8.GetString(bytes);
                Debug.Log("Received message: " + message);
                ApplyUpdate(message);
            };

            websocket.OnOpen += () =>
            {
                isConnected = true;
                Debug.Log("WebSocket connected.");
            };

            websocket.OnError += (e) =>
            {
                Debug.LogError("WebSocket error: " + e);
            };

            websocket.OnClose += (e) =>
            {
                isConnected = false;
                Debug.Log("WebSocket connection closed: " + e);
            };

            websocket.Connect();
        }
        else
        {
            Debug.LogWarning("Already connected to WebSocket.");
        }
    }

    // Disconnect from the WebSocket server
    [MenuItem("Tools/Disconnect from WebSocket Server")]
    public static void DisconnectFromWebSocket()
    {
        if (isConnected)
        {
            Debug.Log("Disconnecting from WebSocket...");
            websocket.Close();
            isConnected = false;
        }
        else
        {
            Debug.LogWarning("Not connected to any WebSocket.");
        }
    }

    // Send updates to the server
    public static void SendUpdate(string data)
    {
        if (websocket != null && websocket.State == WebSocketState.Open)
        {
            Debug.Log("Sending update: " + data);
            websocket.SendText(data);
        }
        else
        {
            Debug.LogWarning("WebSocket is not open. Cannot send data.");
        }
    }

    // Apply changes when receiving a message
    static void ApplyUpdate(string data)
    {
        TransformUpdate update = JsonUtility.FromJson<TransformUpdate>(data);
        GameObject obj = GameObject.Find(update.objectName);
        if (obj != null)
        {
            // Use Undo and SerializedObject to ensure that the transform updates are correctly tracked in the editor
            Undo.RecordObject(obj.transform, "Sync Transform");
            obj.transform.position = update.position;
            obj.transform.rotation = Quaternion.Euler(update.rotation);
            EditorUtility.SetDirty(obj);
            Debug.Log($"Applied update to {obj.name}: Position = {update.position}, Rotation = {update.rotation}");
        }
        else
        {
            Debug.LogWarning("Object not found: " + update.objectName);
        }
    }

    // Listen for changes in the hierarchy
    private static void OnHierarchyChanged()
    {
        foreach (var obj in Selection.gameObjects)
        {
            if (obj != null)
            {
                Debug.Log($"Object changed: {obj.name}");
                SendTransformUpdate(obj);
            }
        }
    }

    // Send a transform update when an object moves or changes
    private static void SendTransformUpdate(GameObject obj)
    {
        if (obj != null)
        {
            // Create the current transform update
            TransformUpdate update = new TransformUpdate
            {
                objectName = obj.name,
                position = obj.transform.position,
                rotation = obj.transform.rotation.eulerAngles
            };

            // Check if the transform has actually changed compared to the last known values
            if (HasTransformChanged(update))
            {
                // Save the new transform as the last known
                lastKnownTransforms[obj.name] = update;

                string json = JsonUtility.ToJson(update);
                Debug.Log($"Sending transform update: {obj.name}, Position = {obj.transform.position}, Rotation = {obj.transform.rotation.eulerAngles}");
                SendUpdate(json);
            }
            else
            {
                Debug.Log($"No changes detected for {obj.name}, not sending update.");
            }
        }
    }

    // Check if the transform has changed compared to the last known values
    private static bool HasTransformChanged(TransformUpdate update)
    {
        if (lastKnownTransforms.ContainsKey(update.objectName))
        {
            TransformUpdate lastUpdate = lastKnownTransforms[update.objectName];
            return !lastUpdate.position.Equals(update.position) || !lastUpdate.rotation.Equals(update.rotation);
        }
        return true; // If there is no last known transform, treat it as changed
    }

    // Editor update method to track changes in the editor (not runtime)
    private static void EditorUpdate()
    {
        if(isConnected)      websocket.DispatchMessageQueue();

        // Update the scene in the editor as objects move or change
        foreach (var obj in Selection.gameObjects)
        {
            if (obj != null)
            {
                SendTransformUpdate(obj);
            }
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
