using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEngine;

[System.Serializable]
[CreateAssetMenu(fileName = "UnrealToUnityImporterMaterialAttribute", menuName = "UnrealToUnityImporter/Create material attributes")]
public class MaterialAttributes : SerializedScriptableObject
{
    public MaterialBlendMode[] blendModes;
    public Material material;
    public Dictionary<string, TextureShaderAttributes> unrealToUnityTextureParameterMapping;

    MaterialAttributes()
    {
        unrealToUnityTextureParameterMapping = new()
        {
            { "BaseColor", new TextureShaderAttributes() },
            { "Metallic", new TextureShaderAttributes() },
            { "Normal", new TextureShaderAttributes() },
            { "Roughness", new TextureShaderAttributes() },
            { "Emissive", new TextureShaderAttributes() }
        };
    }
}