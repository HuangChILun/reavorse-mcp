using UnityEngine;
using Newtonsoft.Json.Linq;
using UnityEngine.Rendering.Universal;
using UnityEngine.Rendering;
using UnityEditor;
using System.IO;
using System;

namespace UnityMCP.Editor.Commands
{
    /// <summary>
    /// Handles material-related commands
    /// </summary>
    public static class MaterialCommandHandler
    {
        /// <summary>
        /// Sets or modifies a material on an object
        /// </summary>
        public static object SetMaterial(JObject @params)
        {
            string objectName = (string)@params["object_name"] ?? throw new System.Exception("Parameter 'object_name' is required.");
            var obj = GameObject.Find(objectName) ?? throw new System.Exception($"Object '{objectName}' not found.");
            var renderer = obj.GetComponent<Renderer>() ?? throw new System.Exception($"Object '{objectName}' has no renderer.");

            // Check if URP is being used
            bool isURP = GraphicsSettings.currentRenderPipeline is UniversalRenderPipelineAsset;

            Material material = null;
            string materialName = (string)@params["material_name"];
            bool createIfMissing = (bool)(@params["create_if_missing"] ?? true);
            string materialPath = null;

            // If material name is specified, try to find or create it
            if (!string.IsNullOrEmpty(materialName))
            {
                // Ensure Materials folder exists
                const string materialsFolder = "Assets/Materials";
                if (!Directory.Exists(materialsFolder))
                {
                    Directory.CreateDirectory(materialsFolder);
                }

                materialPath = $"{materialsFolder}/{materialName}.mat";
                material = AssetDatabase.LoadAssetAtPath<Material>(materialPath);

                if (material == null && createIfMissing)
                {
                    // Create new material with appropriate shader
                    material = new Material(isURP ? Shader.Find("Universal Render Pipeline/Lit") : Shader.Find("Standard"));
                    material.name = materialName;

                    // Save the material asset
                    AssetDatabase.CreateAsset(material, materialPath);
                    AssetDatabase.SaveAssets();
                }
                else if (material == null)
                {
                    throw new System.Exception($"Material '{materialName}' not found and create_if_missing is false.");
                }
            }
            else
            {
                // Create a temporary material if no name specified
                material = new Material(isURP ? Shader.Find("Universal Render Pipeline/Lit") : Shader.Find("Standard"));
            }

            // Apply color if specified
            if (@params.ContainsKey("color"))
            {
                var colorArray = (JArray)@params["color"];
                if (colorArray.Count < 3 || colorArray.Count > 4)
                    throw new System.Exception("Color must be an array of 3 (RGB) or 4 (RGBA) floats.");

                Color color = new(
                    (float)colorArray[0],
                    (float)colorArray[1],
                    (float)colorArray[2],
                    colorArray.Count > 3 ? (float)colorArray[3] : 1.0f
                );
                material.color = color;

                // If this is a saved material, make sure to save the color change
                if (!string.IsNullOrEmpty(materialPath))
                {
                    EditorUtility.SetDirty(material);
                    AssetDatabase.SaveAssets();
                }
            }

            // Apply the material to the renderer
            renderer.material = material;

            return new { material_name = material.name, path = materialPath };
        }
        public static object CreateAdvancedMaterial(JObject @params)
        {
            string materialName = (string)@params["material_name"] ?? throw new Exception("Parameter 'material_name' is required.");
            string shaderType = (string)@params["shader_type"] ?? "Standard";
            string renderMode = (string)@params["render_mode"] ?? "Opaque";
            bool createIfMissing = (bool?)@params["create_if_missing"] ?? true;
            string savePath = (string)@params["save_path"] ?? "Assets/Materials";
            
            // Ensure the Materials folder exists
            if (!Directory.Exists(savePath))
                Directory.CreateDirectory(savePath);
            
            string materialPath = $"{savePath}/{materialName}.mat";
            
            // Check if the material already exists
            Material material = AssetDatabase.LoadAssetAtPath<Material>(materialPath);
            if (material != null && !createIfMissing)
                return new { error = $"Material '{materialName}' already exists and create_if_missing is false." };
            
            // Create material
            bool isURP = GraphicsSettings.currentRenderPipeline is UniversalRenderPipelineAsset;
            bool isHDRP = false; 
            
            Shader shader;
            if (isURP)
            {
                shader = Shader.Find(shaderType == "Standard" ? "Universal Render Pipeline/Lit" : $"Universal Render Pipeline/{shaderType}");
            }
            else if (isHDRP)
            {
                shader = Shader.Find(shaderType == "Standard" ? "HDRP/Lit" : $"HDRP/{shaderType}");
            }
            else
            {
                shader = Shader.Find(shaderType == "Standard" ? "Standard" : shaderType);
            }
            
            if (shader == null)
                return new { error = $"Shader '{shaderType}' not found." };
            
            // Create new material or update existing one
            if (material == null)
            {
                material = new Material(shader);
                material.name = materialName;
                AssetDatabase.CreateAsset(material, materialPath);
            }
            else
            {
                material.shader = shader;
            }
            
            // Set render mode
            switch (renderMode.ToLower())
            {
                case "transparent":
                    material.SetFloat("_Mode", 3); // Value for transparent mode in Standard shader
                    material.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
                    material.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
                    material.SetInt("_ZWrite", 0);
                    material.DisableKeyword("_ALPHATEST_ON");
                    material.EnableKeyword("_ALPHABLEND_ON");
                    material.DisableKeyword("_ALPHAPREMULTIPLY_ON");
                    material.renderQueue = 3000;
                    break;
                case "cutout":
                    material.SetFloat("_Mode", 1); // Value for cutout mode in Standard shader
                    material.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.One);
                    material.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.Zero);
                    material.SetInt("_ZWrite", 1);
                    material.EnableKeyword("_ALPHATEST_ON");
                    material.DisableKeyword("_ALPHABLEND_ON");
                    material.DisableKeyword("_ALPHAPREMULTIPLY_ON");
                    material.renderQueue = 2450;
                    break;
                default: 
                    material.SetFloat("_Mode", 0); // Value for opaque mode in Standard shader
                    material.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.One);
                    material.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.Zero);
                    material.SetInt("_ZWrite", 1);
                    material.DisableKeyword("_ALPHATEST_ON");
                    material.DisableKeyword("_ALPHABLEND_ON");
                    material.DisableKeyword("_ALPHAPREMULTIPLY_ON");
                    material.renderQueue = -1;
                    break;
            }
            
            // Save changes
            EditorUtility.SetDirty(material);
            AssetDatabase.SaveAssets();
            
            return new { 
                message = $"Advanced material '{materialName}' created/updated successfully", 
                material_name = material.name, 
                path = materialPath 
            };
        }
        
        public static object SetMaterialProperties(JObject @params)
        {
            string materialPath = (string)@params["material_path"] ?? throw new Exception("Parameter 'material_path' is required.");
            
            // Load material
            Material material = AssetDatabase.LoadAssetAtPath<Material>(materialPath);
            if (material == null)
                return new { error = $"Material not found at path: {materialPath}" };
            
            // Update material properties
            if (@params["color"] != null)
            {
                JArray colorArray = (JArray)@params["color"];
                Color color = new(
                    (float)colorArray[0],
                    (float)colorArray[1],
                    (float)colorArray[2],
                    colorArray.Count > 3 ? (float)colorArray[3] : 1.0f
                );
                material.color = color;
                
                // Apply appropriate color properties for various render pipelines
                if (material.HasProperty("_BaseColor")) // URP
                    material.SetColor("_BaseColor", color);
                else if (material.HasProperty("_Color")) // Standard
                    material.SetColor("_Color", color);
            }
            
            // Set metallic value
            if (@params["metallic"] != null)
            {
                float metallic = (float)@params["metallic"];
                if (material.HasProperty("_Metallic"))
                    material.SetFloat("_Metallic", Mathf.Clamp01(metallic));
            }
            
            // Set smoothness
            if (@params["smoothness"] != null)
            {
                float smoothness = (float)@params["smoothness"];
                if (material.HasProperty("_Smoothness")) // URP
                    material.SetFloat("_Smoothness", Mathf.Clamp01(smoothness));
                else if (material.HasProperty("_Glossiness")) // Standard
                    material.SetFloat("_Glossiness", Mathf.Clamp01(smoothness));
            }
            
            // Set normal map strength
            if (@params["normal_scale"] != null)
            {
                float normalScale = (float)@params["normal_scale"];
                if (material.HasProperty("_BumpScale"))
                    material.SetFloat("_BumpScale", normalScale);
            }
            
            // Set ambient occlusion strength
            if (@params["occlusion_strength"] != null)
            {
                float occlusionStrength = (float)@params["occlusion_strength"];
                if (material.HasProperty("_OcclusionStrength"))
                    material.SetFloat("_OcclusionStrength", Mathf.Clamp01(occlusionStrength));
            }
            
            // Set height map scale
            if (@params["height_scale"] != null)
            {
                float heightScale = (float)@params["height_scale"];
                if (material.HasProperty("_Parallax"))
                    material.SetFloat("_Parallax", heightScale);
            }
            
            // Set emission color and intensity
            if (@params["emission_color"] != null)
            {
                JArray emissionArray = (JArray)@params["emission_color"];
                Color emissionColor = new(
                    (float)emissionArray[0],
                    (float)emissionArray[1],
                    (float)emissionArray[2],
                    emissionArray.Count > 3 ? (float)emissionArray[3] : 1.0f
                );
                
                float emissionIntensity = (float)(@params["emission_intensity"] ?? 1.0f);
                
                // Enable emission
                material.EnableKeyword("_EMISSION");
                
                // Set emission color and intensity
                Color finalEmission = emissionColor * emissionIntensity;
                if (material.HasProperty("_EmissionColor"))
                    material.SetColor("_EmissionColor", finalEmission);
            }
            
            // Save changes
            EditorUtility.SetDirty(material);
            AssetDatabase.SaveAssets();
            
            return new { 
                message = $"Material properties updated successfully", 
                material_name = material.name 
            };
        }

        public static object SetMaterialTexture(JObject @params)
        {
            string materialPath = (string)@params["material_path"] ?? throw new Exception("Parameter 'material_path' is required.");
            string textureType = (string)@params["texture_type"] ?? throw new Exception("Parameter 'texture_type' is required.");
            string texturePath = (string)@params["texture_path"] ?? throw new Exception("Parameter 'texture_path' is required.");
            
            // Load material and texture
            Material material = AssetDatabase.LoadAssetAtPath<Material>(materialPath);
            if (material == null)
                return new { error = $"Material not found at path: {materialPath}" };
            
            Texture2D texture = AssetDatabase.LoadAssetAtPath<Texture2D>(texturePath);
            if (texture == null)
                return new { error = $"Texture not found at path: {texturePath}" };
            
            // Map texture type to material property name
            string propertyName = MapTextureTypeToProperty(textureType, material);
            if (propertyName == null)
                return new { error = $"Texture type '{textureType}' not supported for this material" };
            
            // Set texture
            material.SetTexture(propertyName, texture);
            
            // Set texture tiling and offset
            if (@params["tiling"] != null)
            {
                JArray tilingArray = (JArray)@params["tiling"];
                if (tilingArray.Count >= 2)
                {
                    Vector2 tiling = new((float)tilingArray[0], (float)tilingArray[1]);
                    material.SetTextureScale(propertyName, tiling);
                }
            }
            
            if (@params["offset"] != null)
            {
                JArray offsetArray = (JArray)@params["offset"];
                if (offsetArray.Count >= 2)
                {
                    Vector2 offset = new((float)offsetArray[0], (float)offsetArray[1]);
                    material.SetTextureOffset(propertyName, offset);
                }
            }
            
            // Save changes
            EditorUtility.SetDirty(material);
            AssetDatabase.SaveAssets();
            
            return new { 
                message = $"Material texture '{textureType}' set successfully", 
                material_name = material.name, 
                texture_name = texture.name 
            };
        }

        private static string MapTextureTypeToProperty(string textureType, Material material)
        {
            // Check the shader type used by the material
            bool isURP = material.shader.name.Contains("Universal Render Pipeline");
            bool isHDRP = material.shader.name.Contains("HDRP");
            
            // Return the correct property name based on shader and texture type
            switch (textureType.ToLower())
            {
                case "albedo":
                case "diffuse":
                case "main":
                    if (isURP && material.HasProperty("_BaseMap"))
                        return "_BaseMap";
                    else if (material.HasProperty("_MainTex"))
                        return "_MainTex";
                    break;
                    
                case "normal":
                case "bump":
                    if (material.HasProperty("_BumpMap"))
                        return "_BumpMap";
                    else if (material.HasProperty("_NormalMap"))
                        return "_NormalMap";
                    break;
                    
                case "metallic":
                    if (isURP && material.HasProperty("_MetallicGlossMap"))
                        return "_MetallicGlossMap";
                    else if (material.HasProperty("_MetallicMap"))
                        return "_MetallicMap";
                    break;
                    
                case "smoothness":
                case "roughness":
                    if (material.HasProperty("_SpecGlossMap"))
                        return "_SpecGlossMap";
                    else if (material.HasProperty("_SmoothnessMap"))
                        return "_SmoothnessMap";
                    break;
                    
                case "occlusion":
                case "ao":
                    if (material.HasProperty("_OcclusionMap"))
                        return "_OcclusionMap";
                    break;
                    
                case "height":
                case "parallax":
                    if (material.HasProperty("_ParallaxMap"))
                        return "_ParallaxMap";
                    else if (material.HasProperty("_HeightMap"))
                        return "_HeightMap";
                    break;
                    
                case "emission":
                case "emissive":
                    if (material.HasProperty("_EmissionMap"))
                        return "_EmissionMap";
                    else if (material.HasProperty("_EmissiveMap"))
                        return "_EmissiveMap";
                    break;
                    
                case "detail":
                case "detail_mask":
                    if (material.HasProperty("_DetailMask"))
                        return "_DetailMask";
                    break;
                    
                case "detail_albedo":
                case "detail_diffuse":
                    if (material.HasProperty("_DetailAlbedoMap"))
                        return "_DetailAlbedoMap";
                    break;
                    
                case "detail_normal":
                    if (material.HasProperty("_DetailNormalMap"))
                        return "_DetailNormalMap";
                    break;
            }
            
            return null; 
        }

        public static object CreateMaterialFromTemplate(JObject @params)
        {
            string materialName = (string)@params["material_name"] ?? throw new Exception("Parameter 'material_name' is required.");
            string template = (string)@params["template"] ?? throw new Exception("Parameter 'template' is required.");
            string savePath = (string)@params["save_path"] ?? "Assets/Materials";
            
            // Ensure the Materials folder exists
            if (!Directory.Exists(savePath))
                Directory.CreateDirectory(savePath);
            
            string materialPath = $"{savePath}/{materialName}.mat";
            
            // Create material
            Material material;
            bool isURP = GraphicsSettings.currentRenderPipeline is UniversalRenderPipelineAsset;
            
            // Apply predefined settings based on the template name
            switch (template.ToLower())
            {
                case "metal":
                    // Create metal material
                    if (isURP)
                        material = new Material(Shader.Find("Universal Render Pipeline/Lit"));
                    else
                        material = new Material(Shader.Find("Standard"));
                        
                    material.SetFloat("_Metallic", 1.0f);
                    material.SetFloat(isURP ? "_Smoothness" : "_Glossiness", 0.8f);
                    material.SetColor(isURP ? "_BaseColor" : "_Color", new Color(0.77f, 0.78f, 0.8f));
                    break;
                    
                case "plastic":
                    // Create plastic material
                    if (isURP)
                        material = new Material(Shader.Find("Universal Render Pipeline/Lit"));
                    else
                        material = new Material(Shader.Find("Standard"));
                        
                    material.SetFloat("_Metallic", 0.0f);
                    material.SetFloat(isURP ? "_Smoothness" : "_Glossiness", 0.9f);
                    material.SetColor(isURP ? "_BaseColor" : "_Color", new Color(0.9f, 0.9f, 0.9f));
                    break;
                    
                case "wood":
                    // Create wood material
                    if (isURP)
                        material = new Material(Shader.Find("Universal Render Pipeline/Lit"));
                    else
                        material = new Material(Shader.Find("Standard"));
                        
                    material.SetFloat("_Metallic", 0.0f);
                    material.SetFloat(isURP ? "_Smoothness" : "_Glossiness", 0.3f);
                    material.SetColor(isURP ? "_BaseColor" : "_Color", new Color(0.7f, 0.5f, 0.3f));
                    break;
                    
                case "glass":
                    // Create glass material
                    if (isURP)
                        material = new Material(Shader.Find("Universal Render Pipeline/Lit"));
                    else
                        material = new Material(Shader.Find("Standard"));
                        
                    material.SetFloat("_Metallic", 0.0f);
                    material.SetFloat(isURP ? "_Smoothness" : "_Glossiness", 1.0f);
                    material.SetColor(isURP ? "_BaseColor" : "_Color", new Color(0.9f, 0.9f, 0.9f, 0.2f));
                    
                    // Set as transparent
                    material.SetFloat("_Mode", 3); // Transparent
                    material.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.One);
                    material.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
                    material.SetInt("_ZWrite", 0);
                    material.DisableKeyword("_ALPHATEST_ON");
                    material.EnableKeyword("_ALPHABLEND_ON");
                    material.DisableKeyword("_ALPHAPREMULTIPLY_ON");
                    material.renderQueue = 3000;
                    break;
                    
                case "emissive":
                    // Create emissive material
                    if (isURP)
                        material = new Material(Shader.Find("Universal Render Pipeline/Lit"));
                    else
                        material = new Material(Shader.Find("Standard"));
                        
                    material.SetFloat("_Metallic", 0.0f);
                    material.SetFloat(isURP ? "_Smoothness" : "_Glossiness", 0.5f);
                    material.SetColor(isURP ? "_BaseColor" : "_Color", new Color(0.2f, 0.2f, 0.2f));
                    
                    // Enable emission
                    material.EnableKeyword("_EMISSION");
                    material.globalIlluminationFlags = MaterialGlobalIlluminationFlags.RealtimeEmissive;
                    material.SetColor("_EmissionColor", new Color(2.0f, 0.5f, 0.0f)); // 橙色發光，強度為2
                    break;
                    
                case "fabric":
                    // Create fabric material
                    if (isURP)
                        material = new Material(Shader.Find("Universal Render Pipeline/Lit"));
                    else
                        material = new Material(Shader.Find("Standard"));
                        
                    material.SetFloat("_Metallic", 0.0f);
                    material.SetFloat(isURP ? "_Smoothness" : "_Glossiness", 0.1f);
                    material.SetColor(isURP ? "_BaseColor" : "_Color", new Color(0.6f, 0.6f, 0.8f));
                    break;
                    
                case "skin":
                    // Create skin material (subsurface scattering)
                    if (isURP)
                    {
                        material = new Material(Shader.Find("Universal Render Pipeline/Lit"));
                    }
                    else
                    {
                        material = new Material(Shader.Find("Standard"));
                    }
                        
                    material.SetFloat("_Metallic", 0.0f);
                    material.SetFloat(isURP ? "_Smoothness" : "_Glossiness", 0.3f);
                    material.SetColor(isURP ? "_BaseColor" : "_Color", new Color(0.9f, 0.7f, 0.6f));
                    break;
                    
                default:
                    return new { error = $"Unknown material template: {template}" };
            }
            
            material.name = materialName;
            AssetDatabase.CreateAsset(material, materialPath);
            AssetDatabase.SaveAssets();
            
            return new { 
                message = $"Material created from template '{template}'", 
                material_name = material.name, 
                path = materialPath 
            };
        }
    }
}