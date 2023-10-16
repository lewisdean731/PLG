using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

[CreateAssetMenu]
public class TextureData : UpdateableData
{
    const int textureSize = 512;
    const TextureFormat textureFormat = TextureFormat.RGB565;

    float savedMinHeight;
    float savedMaxHeight;

    public Layer[] layers;
    public void ApplyToMaterial(Material material)
    {
        material.SetInt("layerCount", layers.Length);
        material.SetFloatArray("baseTextureScales", layers.Select(layer => layer.textureScale).ToArray());
        material.SetColorArray("baseColours", layers.Select(layer => layer.tint).ToArray());
        material.SetFloatArray("baseColourStrengths", layers.Select(layer => layer.tintStrength).ToArray());
        material.SetFloatArray("baseStartHeights", layers.Select(layer => layer.startHeight).ToArray());
        material.SetFloatArray("baseBlends", layers.Select(layer => layer.blendStrength).ToArray());
        Texture2DArray texturesArray = GenerateTextureArray(layers.Select(layer => layer.texture).ToArray());
        material.SetTexture("baseTextures", texturesArray);

        UpdateMeshHeights(material, savedMinHeight, savedMaxHeight);
    }

    public void UpdateMeshHeights(Material material, float minHeight, float maxHeight)
    {
        savedMinHeight = minHeight;
        savedMaxHeight = maxHeight;

        material.SetFloat("minHeight", minHeight);
        material.SetFloat("maxHeight", maxHeight);
    }

    Texture2DArray GenerateTextureArray(Texture2D[] textures)
    {
        Texture2DArray textureArray = new Texture2DArray(textureSize, textureSize, textures.Length, textureFormat, true);
        for(int i = 0; i < textures.Length; i++)
        {
            textureArray.SetPixels(textures[i].GetPixels(), i);
        }
        textureArray.Apply();
        return textureArray;
    }

    [System.Serializable]
    public class Layer
    {
        public Texture2D texture;
        public float textureScale;
        public Color tint;
        [Range(0f, 1f)]
        public float tintStrength;
        [Range(0f, 1f)]
        public float startHeight;
        [Range(0f, 1f)]
        public float blendStrength;
    }
}
