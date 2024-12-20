using System.Collections.Specialized;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using Codice.Utils;
using UnityEditor;
using UnityEngine;

public static class EUnrealToUnityImporterServer
{
    private static Socket _socket;

    [MenuItem("Unreal to Unity Importer/Start Server")]
    static void StartImporterServer()
    {
        StopImporterServer();
        
        IPAddress ipAddress = IPAddress.Parse("127.0.0.1");
        IPEndPoint localEndPoint = new IPEndPoint(ipAddress, 55720);

        _socket = new Socket(ipAddress.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
        _socket.Blocking = false;
        _socket.NoDelay = true;
        _socket.Bind(localEndPoint);
        _socket.Listen(100);
        StartAccept();
    }

    static void StartAccept()
    {
        if (_socket != null)
        {
            SocketAsyncEventArgs acceptEvent = new SocketAsyncEventArgs();
            acceptEvent.Completed += Accept;
        
            _socket.AcceptAsync(acceptEvent);
        }
    }
    
    static void Accept(object data, SocketAsyncEventArgs e)
    {
        byte[] receivedData = new byte[1024];
        int receivedSize = e.AcceptSocket.Receive(receivedData);
        e.AcceptSocket.Shutdown(SocketShutdown.Both);
        e.AcceptSocket.Close();

        string receivedString = Encoding.ASCII.GetString(receivedData).Substring(0, receivedSize);

        string header = "unrealToUnityImporter?";
        
        int headerStartIndex = receivedString.IndexOf(header);

        if (headerStartIndex >= 0)
        {
            int headerEndIndex = headerStartIndex + header.Length;
            
            string parameters = receivedString.Substring(headerEndIndex);
            int spaceIndex = parameters.IndexOf(" ");

            if (spaceIndex >= 0)
            {
                parameters = parameters.Substring(0, spaceIndex);
            }
            
            NameValueCollection parsedParameters = HttpUtility.ParseQueryString(parameters);
            
            string meshImportDescriptorPathKey = "ImportDescriptorPath";
            string meshImportDescriptorPath = parsedParameters[meshImportDescriptorPathKey];

            if (!string.IsNullOrEmpty(meshImportDescriptorPath))
            {
                EditorApplication.delayCall += () => EUnrealToUnityImporter.ImportMeshesFromDescriptor(meshImportDescriptorPath);
            }
        }

        StartAccept();
    }
    
    [MenuItem("Unreal to Unity Importer/Stop Server")]
    static void StopImporterServer()
    {
        if (_socket != null)
        {
            if (_socket.Connected)
            {
                _socket.Disconnect(false);   
            }
            
            _socket.Close();
            _socket = null;
        }
    }

    [MenuItem("Unreal to Unity Importer/Select importer attributes")]
    public static void SetImporterAttributes()
    {
        string importerAttributesPath = AssetDatabase.GUIDToAssetPath(EditorPrefs.GetString(EUnrealToUnityImporter.UnrealToUnityImporterAttributeAssetGUIDPrefsKey));
        string openDirectory = Path.Combine(Application.dataPath, importerAttributesPath);
        string selectedAsset = EditorUtility.OpenFilePanel("Set importer attributes", openDirectory, "asset");

        if (string.IsNullOrEmpty(selectedAsset))
        {
            return;
        }
        
        string relativeToAssetsPath = Path.GetRelativePath(Directory.GetParent(Application.dataPath)?.ToString(), selectedAsset);
        GUID importerAttributesGUID = AssetDatabase.GUIDFromAssetPath(relativeToAssetsPath);

        if (!importerAttributesGUID.Empty())
        {
            string importerAttributesAssetPath = AssetDatabase.GUIDToAssetPath(importerAttributesGUID);
            UnrealToUnityImporterAttributes importerAttributes = AssetDatabase.LoadAssetAtPath<UnrealToUnityImporterAttributes>(importerAttributesAssetPath);

            if (importerAttributes != null)
            {
                EditorPrefs.SetString(EUnrealToUnityImporter.UnrealToUnityImporterAttributeAssetGUIDPrefsKey, importerAttributesGUID.ToString());
                return;
            }
        }
        
        Debug.LogError("Selected asset isn't a UnrealToUnityImporterAttributes or the asset couldn't be loaded");
    }
}
