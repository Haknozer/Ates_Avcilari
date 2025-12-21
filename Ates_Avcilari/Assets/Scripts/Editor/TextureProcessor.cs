#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using System.IO;

[InitializeOnLoad]
public class TextureProcessor : EditorWindow
{
    static TextureProcessor()
    {
        Debug.Log("🔧 TextureProcessor Loaded! Right click on sprites or check 'Tools' menu.");
    }

    // Üst Menü
    [MenuItem("Tools/Fix Black Backgrounds")]
    public static void FixBlackBackgrounds() { ProcessTextures(true); }

    [MenuItem("Tools/Fix White Backgrounds")]
    public static void FixWhiteBackgrounds() { ProcessTextures(false); }

    // Sağ Tık Menüsü (Project penceresinde dosyaya sağ tıklayınca)
    [MenuItem("Assets/Fix Black Backgrounds")]
    public static void FixBlackContext() { ProcessTextures(true); }

    [MenuItem("Assets/Fix White Backgrounds")]
    public static void FixWhiteContext() { ProcessTextures(false); }

    static void ProcessTextures(bool removeBlack)
    {
        foreach (Object obj in Selection.objects)
        {
            Texture2D texture = obj as Texture2D;
            if (texture == null) continue;

            string path = AssetDatabase.GetAssetPath(texture);
            TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;

            // 1. Make readable
            if (!importer.isReadable)
            {
                importer.isReadable = true;
                importer.textureCompression = TextureImporterCompression.Uncompressed;
                importer.SaveAndReimport();
            }

            // 2. Process pixels
            Texture2D newTex = new Texture2D(texture.width, texture.height, TextureFormat.RGBA32, false);
            Color[] pixels = texture.GetPixels();
            for (int i = 0; i < pixels.Length; i++)
            {
                Color p = pixels[i];
                bool shouldTra = false;
                
                if (removeBlack)
                {
                    if (p.r < 0.15f && p.g < 0.15f && p.b < 0.15f) shouldTra = true;
                }
                else
                {
                    // Remove White (near 1.0)
                    if (p.r > 0.9f && p.g > 0.9f && p.b > 0.9f) shouldTra = true;
                }

                if(shouldTra) pixels[i] = Color.clear;
            }
            newTex.SetPixels(pixels);
            newTex.Apply();

            // 3. Save back
            byte[] bytes = newTex.EncodeToPNG();
            File.WriteAllBytes(path, bytes);
            
            // 4. Restore settings
            importer.textureType = TextureImporterType.Sprite;
            importer.alphaIsTransparency = true;
            importer.isReadable = false;
            importer.SaveAndReimport();
            
            Debug.Log($"✅ Processed ({(removeBlack ? "Black" : "White")}): {path}");
        }
        AssetDatabase.Refresh();
    }
}
#endif
