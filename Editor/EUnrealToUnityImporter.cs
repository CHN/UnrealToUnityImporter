using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

public static class EUnrealToUnityImporter
{
    [System.Serializable]
    struct FUnrealToUnityExporterTextureDescriptor
    {
        public bool bUseTexture;
        public bool bUseColor;
        public bool bUseScalar;
        public Color color;
        public float scalar;
        public string parameterName;
        public string texturePath;
    };
    
    [System.Serializable]
    struct FUnrealToUnityExporterMaterialDescriptor
    {
        public string materialPath;
        public MaterialBlendMode blendMode;
        public List<FUnrealToUnityExporterTextureDescriptor> textureDescriptors;
    }
    
    [System.Serializable]
    struct FUnrealToUnityExporterMeshDescriptor
    {
        public string meshPath;
        public bool bEnableReadWrite;
    };

    [System.Serializable]
    struct FUnrealToUnityExporterImportDescriptor
    {
        public string exportDirectory;
        public List<FUnrealToUnityExporterMaterialDescriptor> materialDescriptors;
        public List<FUnrealToUnityExporterMeshDescriptor> meshDescriptors;
    };

    public static readonly string UnrealToUnityImporterAttributeAssetGUIDPrefsKey = "UnrealToUnityImporterAttributeAssetGUID";
    public static readonly string ImportFolder = "UnrealToUnityExporter";

    public static UnrealToUnityImporterAttributes LoadSelectedImporterAttributes()
    {
        string importerAttributesGUID = EditorPrefs.GetString(UnrealToUnityImporterAttributeAssetGUIDPrefsKey);
        string importerAttributesAssetPath = AssetDatabase.GUIDToAssetPath(importerAttributesGUID);
        UnrealToUnityImporterAttributes importerAttributes = AssetDatabase.LoadAssetAtPath<UnrealToUnityImporterAttributes>(importerAttributesAssetPath);
        return importerAttributes;
    }
    
    public static void ImportMeshesFromDescriptor(string meshImportDescriptorPath)
    {
        UnrealToUnityImporterAttributes importerAttributes = LoadSelectedImporterAttributes();

        if (importerAttributes == null)
        {
            EUnrealToUnityImporterServer.SetImporterAttributes();
            importerAttributes = LoadSelectedImporterAttributes();

            if (importerAttributes == null)
            {
                Debug.LogError("Importer attributes isn't correctly set");
                return;
            }
        }
        
        string meshImportDescriptorContent = File.ReadAllText(meshImportDescriptorPath);
        FUnrealToUnityExporterImportDescriptor importDescriptor = JsonUtility.FromJson<FUnrealToUnityExporterImportDescriptor>(meshImportDescriptorContent);

        string importDirectory = Path.Combine(Application.dataPath, ImportFolder);
        
        foreach (FUnrealToUnityExporterMaterialDescriptor materialDescriptor in importDescriptor.materialDescriptors)
        {
            if (importerAttributes.GetMaterialAttributeForBlendMode(materialDescriptor.blendMode) == null)
            {
                Debug.LogError($"Material attribute not available for {materialDescriptor.blendMode} blend mode, operation aborted");
                return;
            }
            
            foreach (FUnrealToUnityExporterTextureDescriptor textureDescriptor in materialDescriptor.textureDescriptors)
            {
                if (!textureDescriptor.bUseTexture)
                {
                    continue;
                }
                
                string textureImportAssetsPath = GetAssetsRelativePath(textureDescriptor.texturePath);
                Texture2D existingTexture = AssetDatabase.LoadAssetAtPath<Texture2D>(textureImportAssetsPath);

                if (existingTexture)
                {
                    //string sourceImagePath = Path.Combine(importDescriptor.exportDirectory, texturePath);
                    //using System.Drawing.Image sourceTexture = System.Drawing.Image.FromFile(sourceImagePath);
                    //
                    //if (Mathf.Approximately(sourceTexture.Size.Width, existingTexture.width) && 
                    //    Mathf.Approximately(sourceTexture.Size.Height, existingTexture.height))
                    //{
                    //    continue;
                    //}

                    AssetDatabase.DeleteAsset(textureImportAssetsPath);
                }
                
                MoveFileAndPreserveFolderHierarchy(importDescriptor.exportDirectory, importDirectory, textureDescriptor.texturePath);
            }
        }
        
        AssetDatabase.Refresh();

        foreach (FUnrealToUnityExporterMaterialDescriptor materialDescriptor in importDescriptor.materialDescriptors)
        {
            string materialAssetsPath = GetAssetsRelativePath(materialDescriptor.materialPath) + ".mat";
            MaterialAttributes materialAttributes = importerAttributes.GetMaterialAttributeForBlendMode(materialDescriptor.blendMode);
            
            Material material = new Material(materialAttributes.material);
            material.parent = materialAttributes.material;
            string materialDirectory = Path.Combine(importDirectory, materialDescriptor.materialPath);
            new FileInfo(materialDirectory).Directory.Create();
            AssetDatabase.CreateAsset(material, materialAssetsPath);
            
            foreach (FUnrealToUnityExporterTextureDescriptor textureDescriptor in materialDescriptor.textureDescriptors)
            {
                if (!materialAttributes.unrealToUnityTextureParameterMapping.TryGetValue(textureDescriptor.parameterName, out TextureShaderAttributes textureShaderAttributes))
                {
                    continue;
                }
                
                if (textureDescriptor.bUseTexture)
                {
                    string texturePath = GetAssetsRelativePath(textureDescriptor.texturePath);
                    Texture2D texture = AssetDatabase.LoadAssetAtPath<Texture2D>(texturePath);
                    material.SetTexture(textureShaderAttributes.texturePropertyName, texture);
                    
                    TextureImporter textureImporter = (TextureImporter)TextureImporter.GetAtPath(texturePath);
                    
                    if (textureDescriptor.parameterName == "Normal")
                    {
                        textureImporter.textureType = TextureImporterType.NormalMap;
                    }
                    
                    textureImporter.maxTextureSize = texture.width;
                    textureImporter.SaveAndReimport();

                    if (!string.IsNullOrEmpty(textureShaderAttributes.textureEnablerPropertyName))
                    {
                        material.SetInt(textureShaderAttributes.textureEnablerPropertyName, 1);
                    }
                }
                else if (textureDescriptor.bUseColor)
                {
                    material.SetColor(textureShaderAttributes.colorPropertyName, textureDescriptor.color);
                }
                else if (textureDescriptor.bUseScalar)
                {
                    material.SetFloat(textureShaderAttributes.scalarPropertyName, textureDescriptor.scalar);
                }
            }
        }
        
        AssetDatabase.Refresh();
        
        foreach (FUnrealToUnityExporterMeshDescriptor meshDescriptor in importDescriptor.meshDescriptors)
        {
            MoveFileAndPreserveFolderHierarchy(importDescriptor.exportDirectory, importDirectory, meshDescriptor.meshPath);
        }
        
        AssetDatabase.Refresh();
        
        foreach (FUnrealToUnityExporterMeshDescriptor meshDescriptor in importDescriptor.meshDescriptors)
        {
            string meshAssetsPath = GetAssetsRelativePath(meshDescriptor.meshPath);
            ModelImporter modelImporter = (ModelImporter)ModelImporter.GetAtPath(meshAssetsPath);
            modelImporter.bakeAxisConversion = true;
            modelImporter.isReadable = meshDescriptor.bEnableReadWrite;
            modelImporter.SearchAndRemapMaterials(ModelImporterMaterialName.BasedOnMaterialName, ModelImporterMaterialSearch.Everywhere);
            modelImporter.SaveAndReimport();
        }
        
        AssetDatabase.Refresh();
    }

    private static void MoveFileAndPreserveFolderHierarchy(string sourceRootDir, string destDir, string filePath)
    {
        string destPath = Path.Combine(destDir, filePath);

        if (!File.Exists(destPath))
        {
            string sourcePath = Path.Combine(sourceRootDir, filePath);
            new FileInfo(destPath).Directory.Create();
            File.Move(sourcePath, destPath);
        }
    }

    private static string GetAssetsRelativePath(string importPath)
    {
        return Path.Combine("Assets", ImportFolder, importPath);
    }
}