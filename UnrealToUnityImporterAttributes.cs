using System.Collections.Generic;
using System.Linq;
using Sirenix.OdinInspector;
using UnityEngine;

[System.Serializable]
public enum MaterialBlendMode
{
    BLEND_Opaque,
    BLEND_Masked,
    BLEND_Translucent,
    BLEND_Additive,
    BLEND_Modulate,
    BLEND_AlphaComposite,
    BLEND_AlphaHoldout,
    BLEND_TranslucentColoredTransmittance,
}

[System.Serializable]
public struct TextureShaderAttributes
{
    public string texturePropertyName;
    public string textureEnablerPropertyName;
    public string colorPropertyName;
    public string scalarPropertyName;
}

[System.Serializable]
[CreateAssetMenu(fileName = "UnrealToUnityImporterAttribute", menuName = "UnrealToUnityImporter/Create attributes")]
public class UnrealToUnityImporterAttributes : SerializedScriptableObject
{
    public List<MaterialAttributes> materialAttributes;

    public MaterialAttributes GetMaterialAttributeForBlendMode(MaterialBlendMode blendMode)
    {
        foreach (MaterialAttributes materialAttribute in materialAttributes)
        {
            if (materialAttribute.blendModes.Contains(blendMode))
            {
                return materialAttribute;
            }
        }

        return null;
    }
}