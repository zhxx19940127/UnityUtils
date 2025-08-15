using UnityEngine;
using UnityEditor;
using System.IO;
using System.Collections.Generic;
using System.Text;

public class WebConfigGenerator : EditorWindow
{
    private Vector2 scrollPosition;
    
    // CORS Settings
    private bool enableCors = true;
    private string allowOrigin = "*";
    private List<string> allowedMethods = new List<string> { "GET", "POST", "PUT", "DELETE", "OPTIONS" };
    private string allowedHeaders = "Content-Type";
    
    // MIME Types
    private List<MimeType> mimeTypes = new List<MimeType>
    {
        new MimeType(".unityweb", "application/octet-stream"),
        new MimeType(".unity3d", "application/octet-stream"),
        new MimeType(".data", "application/octet-stream"),
        new MimeType(".mem", "application/octet-stream"),
        new MimeType(".memgz", "application/octet-stream"),
        new MimeType(".datagz", "application/octet-stream"),
        new MimeType(".unity3dgz", "application/octet-stream"),
        new MimeType(".jsgz", "application/x-javascript")
    };
    
    // Compression Settings
    private bool doStaticCompression = false;
    private bool doDynamicCompression = false;
    
    // Output Path
    private string outputPath = "Assets/";
    
    [MenuItem("Tools/Web.Config Generator")]
    public static void ShowWindow()
    {
        GetWindow<WebConfigGenerator>("Web.Config Generator");
    }
    
    private void OnGUI()
    {
        scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);
        
        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Web.Config Generator", EditorStyles.boldLabel);
        EditorGUILayout.Space();
        
        // CORS Settings
        EditorGUILayout.LabelField("CORS Settings", EditorStyles.boldLabel);
        enableCors = EditorGUILayout.Toggle("Enable CORS", enableCors);
        
        if (enableCors)
        {
            EditorGUI.indentLevel++;
            allowOrigin = EditorGUILayout.TextField("Access-Control-Allow-Origin", allowOrigin);
            
            EditorGUILayout.LabelField("Allowed Methods:");
            EditorGUI.indentLevel++;
            for (int i = 0; i < allowedMethods.Count; i++)
            {
                EditorGUILayout.BeginHorizontal();
                allowedMethods[i] = EditorGUILayout.TextField(allowedMethods[i]);
                if (GUILayout.Button("Remove", GUILayout.Width(80)))
                {
                    allowedMethods.RemoveAt(i);
                    break;
                }
                EditorGUILayout.EndHorizontal();
            }
            
            if (GUILayout.Button("Add Method", GUILayout.Width(100)))
            {
                allowedMethods.Add("GET");
            }
            EditorGUI.indentLevel--;
            
            allowedHeaders = EditorGUILayout.TextField("Allowed Headers", allowedHeaders);
            EditorGUI.indentLevel--;
        }
        
        EditorGUILayout.Space();
        
        // MIME Types
        EditorGUILayout.LabelField("MIME Type Mappings", EditorStyles.boldLabel);
        for (int i = 0; i < mimeTypes.Count; i++)
        {
            EditorGUILayout.BeginHorizontal();
            mimeTypes[i].fileExtension = EditorGUILayout.TextField("Extension", mimeTypes[i].fileExtension);
            mimeTypes[i].mimeType = EditorGUILayout.TextField("MIME Type", mimeTypes[i].mimeType);
            if (GUILayout.Button("Remove", GUILayout.Width(80)))
            {
                mimeTypes.RemoveAt(i);
                break;
            }
            EditorGUILayout.EndHorizontal();
        }
        
        if (GUILayout.Button("Add MIME Type", GUILayout.Width(120)))
        {
            mimeTypes.Add(new MimeType(".ext", "application/octet-stream"));
        }
        
        EditorGUILayout.Space();
        
        // Compression Settings
        EditorGUILayout.LabelField("Compression Settings", EditorStyles.boldLabel);
        doStaticCompression = EditorGUILayout.Toggle("Static Compression", doStaticCompression);
        doDynamicCompression = EditorGUILayout.Toggle("Dynamic Compression", doDynamicCompression);
        
        EditorGUILayout.Space();
        
        // Output Path
        EditorGUILayout.LabelField("Output Settings", EditorStyles.boldLabel);
        EditorGUILayout.BeginHorizontal();
        outputPath = EditorGUILayout.TextField("Output Path", outputPath);
        if (GUILayout.Button("Browse", GUILayout.Width(80)))
        {
            string path = EditorUtility.SaveFolderPanel("Select Output Directory", outputPath, "");
            if (!string.IsNullOrEmpty(path))
            {
                // Convert to relative path if within Assets folder
                if (path.StartsWith(Application.dataPath))
                {
                    outputPath = "Assets" + path.Substring(Application.dataPath.Length);
                }
                else
                {
                    outputPath = path;
                }
            }
        }
        EditorGUILayout.EndHorizontal();
        
        EditorGUILayout.Space();
        
        // Generate Button
        if (GUILayout.Button("Generate web.config", GUILayout.Height(30)))
        {
            GenerateWebConfig();
        }
        
        EditorGUILayout.EndScrollView();
    }
    
    private void GenerateWebConfig()
    {
        StringBuilder sb = new StringBuilder();
        sb.AppendLine("<?xml version=\"1.0\" encoding=\"UTF-8\"?>");
        sb.AppendLine("<configuration>");
        sb.AppendLine("    <system.webServer>");
        
        // HTTP Protocol (CORS)
        if (enableCors)
        {
            sb.AppendLine("        <httpProtocol>");
            sb.AppendLine("            <customHeaders>");
            sb.AppendLine($"                <add name=\"Access-Control-Allow-Origin\" value=\"{allowOrigin}\" />");
            
            string methods = string.Join(", ", allowedMethods);
            sb.AppendLine($"                <add name=\"Access-Control-Allow-Methods\" value=\"{methods}\" />");
            
            sb.AppendLine($"                <add name=\"Access-Control-Allow-Headers\" value=\"{allowedHeaders}\" />");
            sb.AppendLine("            </customHeaders>");
            sb.AppendLine("        </httpProtocol>");
        }
        
        // Static Content (MIME Types)
        if (mimeTypes.Count > 0)
        {
            sb.AppendLine("        <staticContent>");
            foreach (var mime in mimeTypes)
            {
                sb.AppendLine($"            <mimeMap fileExtension=\"{mime.fileExtension}\" mimeType=\"{mime.mimeType}\" />");
            }
            sb.AppendLine("        </staticContent>");
        }
        
        // URL Compression
        sb.AppendLine("        <urlCompression");
        sb.AppendLine($"            doStaticCompression=\"{doStaticCompression.ToString().ToLower()}\"");
        sb.AppendLine($"            doDynamicCompression=\"{doDynamicCompression.ToString().ToLower()}\" />");
        
        sb.AppendLine("    </system.webServer>");
        sb.AppendLine("</configuration>");
        
        // Save to file
        string fullPath = outputPath;
        if (!fullPath.StartsWith(Application.dataPath))
        {
            if (outputPath.StartsWith("Assets/"))
            {
                fullPath = Path.Combine(Application.dataPath, outputPath.Substring(7));
            }
            else
            {
                fullPath = outputPath;
            }
        }
        
        if (!Directory.Exists(fullPath))
        {
            Directory.CreateDirectory(fullPath);
        }
        
        string filePath = Path.Combine(fullPath, "web.config");
        File.WriteAllText(filePath, sb.ToString());
        
        AssetDatabase.Refresh();
        
        EditorUtility.DisplayDialog("Success", $"web.config file generated at:\n{filePath}", "OK");
    }
    
    private class MimeType
    {
        public string fileExtension;
        public string mimeType;
        
        public MimeType(string ext, string mime)
        {
            fileExtension = ext;
            mimeType = mime;
        }
    }
}