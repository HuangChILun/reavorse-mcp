using UnityEngine;
using UnityEditor;
using Newtonsoft.Json.Linq;
using System;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Collections.Generic;

namespace UnityMCP.Editor.Commands
{
    /// <summary>
    /// Handles script-related commands
    /// </summary>
    public static class ScriptCommandHandler
    {
        /// <summary>
        /// View the contents of a Unity script file
        /// </summary>
        /// <param name="params">Parameters including the script path and options</param>
        /// <returns>Script content or error message</returns>
        public static object ViewScript(JObject @params)
        {
            try {
                string scriptPath = (string)@params["script_path"] ?? throw new Exception("Parameter 'script_path' is required.");
                bool requireExists = (bool?)@params["require_exists"] ?? true;

                // Normalize path
                string relativePath = scriptPath.Replace('\\', '/').Trim('/');
                if (!relativePath.StartsWith("Assets/", StringComparison.OrdinalIgnoreCase))
                {
                    relativePath = $"Assets/{relativePath}";
                }

                // Convert to full filesystem path
                string assetRelativePath = relativePath.StartsWith("Assets/") ? 
                    relativePath.Substring(7) : relativePath;
                string fullPath = Path.Combine(Application.dataPath, assetRelativePath);

                // Check if file exists
                if (!File.Exists(fullPath))
                {
                    if (requireExists)
                        return new { exists = false, message = $"Script file not found: {relativePath}" };
                    else
                        return new { exists = false, message = $"Script file not found: {relativePath}" };
                }

                // Read file content
                string content = File.ReadAllText(fullPath);
                
                // Handle large files
                bool isLarge = content.Length > 10000;
                string encodedContent = isLarge ? Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(content)) : null;

                return new { 
                    exists = true, 
                    content = content,
                    encodedContent = encodedContent,
                    contentEncoded = isLarge,
                    path = relativePath
                };
            }
            catch (Exception e)
            {
                return new { error = $"Error viewing script: {e.Message}", stackTrace = e.StackTrace };
            }
        }

        /// <summary>
        /// Create a new Unity script file
        /// </summary>
        /// <param name="params">Parameters including script name, type, and content</param>
        /// <returns>Success message or error details</returns>
        public static object CreateScript(JObject @params)
        {
            try {
                // Extract and validate required parameters
                string scriptName = (string)@params["script_name"] ?? throw new Exception("Parameter 'script_name' is required.");
                string scriptType = (string)@params["script_type"] ?? "MonoBehaviour";
                string namespaceName = (string)@params["namespace"];
                string scriptFolder = (string)@params["script_folder"];
                bool overwrite = (bool?)@params["overwrite"] ?? false;
                string content = (string)@params["content"];
                
                // Validate script name
                if (!Regex.IsMatch(scriptName, @"^[a-zA-Z_][a-zA-Z0-9_]*$"))
                {
                    return new { 
                        success = false, 
                        error = $"Invalid script name: '{scriptName}'. Use only letters, numbers, underscores, and don't start with a number."
                    };
                }
                
                // Ensure the script name has a .cs extension
                if (!scriptName.EndsWith(".cs", StringComparison.OrdinalIgnoreCase))
                    scriptName += ".cs";
                    
                // Process script path
                string relativeDir = scriptFolder ?? "Scripts"; // Default to the Scripts folder
                if (!string.IsNullOrEmpty(relativeDir))
                {
                    relativeDir = relativeDir.Replace('\\', '/').Trim('/');
                    if (relativeDir.StartsWith("Assets/", StringComparison.OrdinalIgnoreCase))
                    {
                        relativeDir = relativeDir.Substring("Assets/".Length).TrimStart('/');
                    }
                }
                if (string.IsNullOrEmpty(relativeDir)) {
                    relativeDir = "Scripts"; // Ensure a default value
                }
                
                // Build full path
                string fullPathDir = Path.Combine(Application.dataPath, relativeDir);
                string fullPath = Path.Combine(fullPathDir, scriptName);
                string relativePath = Path.Combine("Assets", relativeDir, scriptName).Replace('\\', '/');
                
                // Check if file already exists
                if (File.Exists(fullPath) && !overwrite)
                {
                    return new { 
                        success = false, 
                        error = $"Script already exists at '{relativePath}'. Use overwrite=true to replace it."
                    };
                }
                
                // Ensure directory exists
                try {
                    Directory.CreateDirectory(fullPathDir);
                }
                catch (Exception e) {
                    return new { 
                        success = false, 
                        error = $"Failed to create directory '{relativeDir}': {e.Message}"
                    };
                }
                
                // If no content is provided, generate default content
                if (string.IsNullOrEmpty(content))
                {
                    // Extract the script name without the .cs extension
                    string nameWithoutExtension = scriptName.EndsWith(".cs") ? 
                        scriptName.Substring(0, scriptName.Length - 3) : scriptName;
                        
                    content = GenerateScriptContent(nameWithoutExtension, scriptType, namespaceName);
                }
                
                // Write file
                File.WriteAllText(fullPath, content);
                AssetDatabase.ImportAsset(relativePath);
                AssetDatabase.Refresh();
                
                return new { 
                    success = true, 
                    message = $"Created script: {relativePath}",
                    script_path = relativePath
                };
            }
            catch (Exception e)
            {
                return new { 
                    success = false, 
                    error = $"Failed to create script: {e.Message}",
                    stackTrace = e.StackTrace
                };
            }
        }

        /// <summary>
        /// Update the content of an existing Unity script file
        /// </summary>
        /// <param name="params">Parameters including the script path and new content</param>
        /// <returns>Success message or error details</returns>
        public static object UpdateScript(JObject @params)
        {
            try {
                string scriptPath = (string)@params["script_path"] ?? throw new Exception("Parameter 'script_path' is required.");
                string content = (string)@params["content"] ?? throw new Exception("Parameter 'content' is required.");
                bool createIfMissing = (bool?)@params["create_if_missing"] ?? false;
                bool createFolderIfMissing = (bool?)@params["create_folder_if_missing"] ?? false;
                
                // Handle Base64 encoded content
                if (@params.ContainsKey("contentEncoded") && (bool)@params["contentEncoded"] && @params.ContainsKey("encodedContent"))
                {
                    try {
                        byte[] data = Convert.FromBase64String((string)@params["encodedContent"]);
                        content = System.Text.Encoding.UTF8.GetString(data);
                    }
                    catch (Exception e) {
                        return new { 
                            success = false, 
                            error = $"Failed to decode content: {e.Message}" 
                        };
                    }
                }
                
                // Normalize path
                string relativePath = scriptPath.Replace('\\', '/').Trim('/');
                if (!relativePath.StartsWith("Assets/", StringComparison.OrdinalIgnoreCase))
                {
                    relativePath = $"Assets/{relativePath}";
                }
                
                // Convert to full filesystem path
                string assetRelativePath = relativePath.StartsWith("Assets/") ? 
                    relativePath.Substring(7) : relativePath;
                string fullPath = Path.Combine(Application.dataPath, assetRelativePath);
                
                // Ensure directory exists
                string directory = Path.GetDirectoryName(fullPath);
                if (!Directory.Exists(directory))
                {
                    if (createFolderIfMissing)
                    {
                        try {
                            Directory.CreateDirectory(directory);
                        }
                        catch (Exception e) {
                            return new { 
                                success = false, 
                                error = $"Failed to create directory '{directory}': {e.Message}" 
                            };
                        }
                    }
                    else
                    {
                        return new { 
                            success = false, 
                            error = $"Directory does not exist: {Path.GetDirectoryName(relativePath)}" 
                        };
                    }
                }
                
                // Check if file exists
                if (!File.Exists(fullPath))
                {
                    if (createIfMissing)
                    {
                        // Create new file
                        try {
                            File.WriteAllText(fullPath, content);
                            AssetDatabase.ImportAsset(relativePath);
                            AssetDatabase.Refresh();
                            
                            return new { 
                                success = true, 
                                message = $"Created script: {relativePath}",
                                path = relativePath
                            };
                        }
                        catch (Exception e) {
                            return new { 
                                success = false, 
                                error = $"Failed to create script: {e.Message}" 
                            };
                        }
                    }
                    else
                    {
                        return new { 
                            success = false, 
                            error = $"Script file not found: {relativePath}" 
                        };
                    }
                }
                
                // Update existing file
                try {
                    File.WriteAllText(fullPath, content);
                    AssetDatabase.ImportAsset(relativePath);
                    AssetDatabase.Refresh();
                    
                    return new { 
                        success = true, 
                        message = $"Updated script: {relativePath}",
                        path = relativePath
                    };
                }
                catch (Exception e) {
                    return new { 
                        success = false, 
                        error = $"Failed to update script: {e.Message}" 
                    };
                }
            }
            catch (Exception e)
            {
                return new { 
                    success = false, 
                    error = $"Error updating script: {e.Message}",
                    stackTrace = e.StackTrace
                };
            }
        }

        /// <summary>
        /// List all script files in the specified folder
        /// </summary>
        /// <param name="params">Parameters including the folder path</param>
        /// <returns>List of scripts or error details</returns>
        public static object ListScripts(JObject @params)
        {
            string folderPath = (string)@params["folder_path"] ?? "Assets";

            // Normalize folder path
            if (folderPath.Equals("Assets", StringComparison.OrdinalIgnoreCase))
            {
                folderPath = Application.dataPath;
            }
            else if (folderPath.StartsWith("Assets/", StringComparison.OrdinalIgnoreCase))
            {
                // Remove the "Assets/" prefix since Application.dataPath already points to it
                string relativePath = folderPath.Substring(7);
                folderPath = Path.Combine(Application.dataPath, relativePath);
            }
            else
            {
                // Assume it is a relative path starting from Assets
                folderPath = Path.Combine(Application.dataPath, folderPath);
            }

            if (!Directory.Exists(folderPath))
                return new { success = false, error = $"Folder not found: {folderPath}" };

            try {
                string[] scripts = Directory.GetFiles(folderPath, "*.cs", SearchOption.AllDirectories)
                    .Select(path => path.Replace(Application.dataPath, "Assets").Replace('\\', '/'))
                    .ToArray();

                return new { success = true, scripts = scripts };
            }
            catch (Exception e) {
                return new { success = false, error = $"Error listing scripts: {e.Message}" };
            }
        }

        /// <summary>
        /// Attach a script component to a GameObject
        /// </summary>
        /// <param name="params">Parameters including the target object's name and script name</param>
        /// <returns>Success message or error details</returns>
        public static object AttachScript(JObject @params)
        {
            try {
                string objectName = (string)@params["object_name"] ?? throw new Exception("Parameter 'object_name' is required.");
                string scriptName = (string)@params["script_name"] ?? throw new Exception("Parameter 'script_name' is required.");
                string scriptPath = (string)@params["script_path"]; // Optional parameter
                
                // Find the target GameObject
                GameObject targetObject = GameObject.Find(objectName);
                if (targetObject == null)
                    return new { success = false, error = $"GameObject '{objectName}' not found in scene." };
                
                // Ensure the script name has a .cs extension (for search)
                if (!scriptName.EndsWith(".cs", StringComparison.OrdinalIgnoreCase))
                    scriptName += ".cs";
                
                // Remove any path separators from the script name (if any)
                string scriptFilename = Path.GetFileName(scriptName);
                string scriptClassname = Path.GetFileNameWithoutExtension(scriptFilename);
                
                // Find the script asset
                MonoScript scriptAsset = null;
                
                // If a specific path is provided, try that path first
                if (!string.IsNullOrEmpty(scriptPath))
                {
                    // Normalize path
                    string fullScriptPath = scriptPath;
                    if (!fullScriptPath.StartsWith("Assets/"))
                        fullScriptPath = $"Assets/{fullScriptPath}";
                        
                    scriptAsset = AssetDatabase.LoadAssetAtPath<MonoScript>(fullScriptPath);
                }
                
                // If the script is not found, search by filename
                if (scriptAsset == null)
                {
                    string[] guids = AssetDatabase.FindAssets($"t:script {scriptClassname}");
                    
                    foreach (string guid in guids)
                    {
                        string path = AssetDatabase.GUIDToAssetPath(guid);
                        
                        // Only consider .cs files
                        if (!path.EndsWith(".cs", StringComparison.OrdinalIgnoreCase))
                            continue;
                            
                        // Check if the filename matches
                        string foundFilename = Path.GetFileName(path);
                        if (string.Equals(foundFilename, scriptFilename, StringComparison.OrdinalIgnoreCase))
                        {
                            scriptAsset = AssetDatabase.LoadAssetAtPath<MonoScript>(path);
                            break;
                        }
                    }
                }
                
                if (scriptAsset == null)
                    return new { success = false, error = $"Script '{scriptName}' not found in project." };
                
                // Get the script's class type
                Type scriptType = scriptAsset.GetClass();
                if (scriptType == null)
                    return new { success = false, error = $"Could not get class type from script '{scriptName}'." };
                
                // Check if the script is already attached
                if (targetObject.GetComponent(scriptType) != null)
                    return new { success = true, message = $"Script '{scriptClassname}' is already attached to '{objectName}'." };
                
                // Add the script component
                Component component = targetObject.AddComponent(scriptType);
                if (component == null)
                    return new { success = false, error = $"Failed to add component '{scriptClassname}' to '{objectName}'." };
                
                return new { 
                    success = true, 
                    message = $"Script '{scriptClassname}' attached to '{objectName}' successfully.",
                    component_name = scriptClassname
                };
            }
            catch (Exception e) {
                return new { success = false, error = $"Error attaching script: {e.Message}" };
            }
        }

        /// <summary>
        /// Generate basic C# script content based on name and type
        /// </summary>
        private static string GenerateScriptContent(string name, string scriptType, string namespaceName)
        {
            string usingStatements = "using UnityEngine;\nusing System.Collections;\n";
            string classDeclaration;
            string body = "\n    // Use this for initialization\n    void Start() {\n\n    }\n\n    // Update is called once per frame\n    void Update() {\n\n    }\n";

            string baseClass = "";
            if (!string.IsNullOrEmpty(scriptType))
            {
                if (scriptType.Equals("MonoBehaviour", StringComparison.OrdinalIgnoreCase))
                    baseClass = " : MonoBehaviour";
                else if (scriptType.Equals("ScriptableObject", StringComparison.OrdinalIgnoreCase))
                {
                    baseClass = " : ScriptableObject";
                    body = ""; // ScriptableObjects don't usually need Start/Update
                }
                else if (scriptType.Equals("Editor", StringComparison.OrdinalIgnoreCase) || scriptType.Equals("EditorWindow", StringComparison.OrdinalIgnoreCase))
                {
                    usingStatements += "using UnityEditor;\n";
                    if (scriptType.Equals("Editor", StringComparison.OrdinalIgnoreCase))
                        baseClass = " : Editor";
                    else
                        baseClass = " : EditorWindow";
                    body = ""; // Editor scripts have different structures
                }
                // Add more types as needed
            }

            classDeclaration = $"public class {name}{baseClass}";

            string fullContent = $"{usingStatements}\n";
            bool useNamespace = !string.IsNullOrEmpty(namespaceName);

            if (useNamespace)
            {
                fullContent += $"namespace {namespaceName}\n{{\n";
                // Indent class and body if using namespace
                classDeclaration = "    " + classDeclaration;
                body = string.Join("\n", body.Split('\n').Select(line => "    " + line));
            }

            fullContent += $"{classDeclaration}\n{{\n{body}\n}}";

            if (useNamespace)
            {
                fullContent += "\n}"; // Close namespace
            }

            return fullContent.Trim() + "\n"; // Ensure a trailing newline
        }
    }
}
