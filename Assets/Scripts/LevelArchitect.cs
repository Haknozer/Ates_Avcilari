#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.IO;

public class LevelArchitect : MonoBehaviour
{
    [Header("Settings")]
    public float pathTileSize = 1.0f;
    
    [Header("Assets")]
    public Sprite groundSprite;
    public Sprite pathSprite;
    public Sprite villageGroundSprite;
    public Sprite[] hazardSprites; // Yangın çıkış noktaları (Çöp, Elektrik vb.)
    public Sprite villageHouseSprite; // Köy evleri
    public Sprite treeSprite; // Ağaç (Kuş bakışı)
    
    // Path coordinates copied from GameBootstrapper
    private List<List<Vector3>> GetPaths()
    {
        List<List<Vector3>> allPaths = new List<List<Vector3>>();
        Vector3 villageTarget = new Vector3(12f, 0f, 0f);

        // Path 1: Center
        allPaths.Add(new List<Vector3>() {
            new Vector3(-20f, 0f, 0f), new Vector3(-14f, 0f, 0f), new Vector3(-14f, 3f, 0f),
            new Vector3(-8f, 3f, 0f), new Vector3(-8f, -1f, 0f), new Vector3(-2f, -1f, 0f),
            new Vector3(-2f, 0f, 0f), new Vector3(4f, 0f, 0f), new Vector3(8f, 0f, 0f), villageTarget
        });

        // Path 2: Top Left
        allPaths.Add(new List<Vector3>() {
            new Vector3(-18f, 8f, 0f), new Vector3(-12f, 8f, 0f), new Vector3(-12f, 5f, 0f),
            new Vector3(-6f, 5f, 0f), new Vector3(-6f, 2f, 0f), new Vector3(0f, 2f, 0f),
            new Vector3(0f, 4f, 0f), new Vector3(6f, 4f, 0f), new Vector3(10f, 2f, 0f), villageTarget
        });

        // Path 3: Bottom Left
        allPaths.Add(new List<Vector3>() {
            new Vector3(-18f, -7f, 0f), new Vector3(-12f, -7f, 0f), new Vector3(-12f, -4f, 0f),
            new Vector3(-5f, -4f, 0f), new Vector3(-5f, -6f, 0f), new Vector3(2f, -6f, 0f),
            new Vector3(2f, -3f, 0f), new Vector3(8f, -3f, 0f), new Vector3(10f, -1f, 0f), villageTarget
        });

        // Path 4: Top
        allPaths.Add(new List<Vector3>() {
            new Vector3(-5f, 10f, 0f), new Vector3(-5f, 7f, 0f), new Vector3(0f, 7f, 0f),
            new Vector3(0f, 5f, 0f), new Vector3(5f, 5f, 0f), new Vector3(5f, 3f, 0f),
            new Vector3(10f, 2f, 0f), villageTarget
        });

        return allPaths;
    }

    [ContextMenu("Build Level")]
    public void BuildLevel()
    {
        Debug.Log("🏗️ Level generation started...");
        CreateSprites();
        ClearScene();
        CreateGround();
        CreateVillage();
        CreatePaths();
        CreateEnvironment();
        Debug.Log("✅ Level generation complete!");
    }

    void CreateSprites()
    {
        string dir = "Assets/Sprites";
        if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);

        CreateAndSaveSprite("Square", 64, Color.white, dir);
        CreateAndSaveSprite("Circle", 128, Color.white, dir);
        
        AssetDatabase.Refresh();
    }

    void ClearScene()
    {
        string[] tags = { "Ground", "Village", "FirePaths", "Environment" };
        foreach(string tag in tags) {
            GameObject obj = GameObject.Find(tag);
            if (obj) DestroyImmediate(obj);
        }
    }

    void CreateGround()
    {
        GameObject ground = new GameObject("Ground");
        SpriteRenderer sr = ground.AddComponent<SpriteRenderer>();
        // Eğer slot boşsa Square yükle
        sr.sprite = groundSprite != null ? groundSprite : LoadSprite("Square");
        sr.color = groundSprite != null ? Color.white : new Color(0.18f, 0.38f, 0.18f); // Görsel varsa boyama
        // Tiling (Döşeme) için Draw mode
        if (groundSprite != null) {
            sr.drawMode = SpriteDrawMode.Tiled;
            sr.size = new Vector2(60f, 30f);
        }
        ground.transform.position = new Vector3(0, 0, 10);
        ground.transform.localScale = new Vector3(60f, 30f, 1f);
        sr.sortingOrder = -100;
    }

    void CreateVillage()
    {
        GameObject village = new GameObject("Village");
        Vector3 center = new Vector3(15f, 0f, 0f);

        // Ground
        CreateObj("VillageGround", village.transform, center + new Vector3(0, 0, 9), new Vector3(10, 18, 1), new Color(0.45f, 0.38f, 0.28f), -95);
        
        // Main Building
        CreateObj("MainBuilding", village.transform, center + new Vector3(0, 0, 4), new Vector3(4, 3, 1), new Color(0.7f, 0.55f, 0.35f), -35);
        CreateObj("Roof", village.transform, center + new Vector3(0, 2, 3), new Vector3(5, 1.5f, 1), new Color(0.6f, 0.25f, 0.15f), -30);

        // Houses
        Vector3[] houses = {
            new Vector3(-3, 4, 4), new Vector3(3, 4, 4),
            new Vector3(-3, -4, 4), new Vector3(3, -4, 4)
        };

        foreach(Vector3 offset in houses) {
            GameObject house = CreateObj("SmallHouse", village.transform, center + offset, new Vector3(2, 2, 1), Color.white, -36);
            if (villageHouseSprite != null) {
                house.GetComponent<SpriteRenderer>().sprite = villageHouseSprite;
            } else {
                 house.GetComponent<SpriteRenderer>().color = new Color(0.65f, 0.5f, 0.3f);
            }
        }

        // Protection Circle
        GameObject circle = CreateObj("ProtectionCircle", village.transform, center + new Vector3(0, 0, 6), new Vector3(12, 12, 1), new Color(0.3f, 0.7f, 0.3f, 0.2f), -90);
        circle.GetComponent<SpriteRenderer>().sprite = LoadSprite("Circle");
    }

    void CreatePaths()
    {
        GameObject parent = new GameObject("FirePaths");
        List<List<Vector3>> paths = GetPaths();
        Sprite square = LoadSprite("Square");
        Sprite circle = LoadSprite("Circle");

        for (int i = 0; i < paths.Count; i++)
        {
            GameObject pathObj = new GameObject($"Path_{i}");
            pathObj.transform.parent = parent.transform;
            
            // Draw Spawn Point (Random Hazard)
            GameObject spawnPoint = CreateObj("SpawnPoint", pathObj.transform, paths[i][0], Vector3.one * 2.0f, Color.white, -47);
            if (hazardSprites != null && hazardSprites.Length > 0) {
                spawnPoint.GetComponent<SpriteRenderer>().sprite = hazardSprites[Random.Range(0, hazardSprites.Length)];
            } else {
                spawnPoint.GetComponent<SpriteRenderer>().sprite = circle;
                spawnPoint.GetComponent<SpriteRenderer>().color = new Color(1f, 0.35f, 0.15f);
            }

            // Draw Path Tiles
            for (int k = 0; k < paths[i].Count - 1; k++)
            {
                Vector3 start = paths[i][k];
                Vector3 end = paths[i][k+1];
                float dist = Vector3.Distance(start, end);
                int segments = Mathf.CeilToInt(dist / 0.9f);

                for(int j=0; j<=segments; j++)
                {
                    float t = (float)j / segments;
                    Vector3 pos = Vector3.Lerp(start, end, t);
                    pos.z = 5;
                    GameObject tile = CreateObj($"Tile_{k}_{j}", pathObj.transform, pos, new Vector3(1.2f, 1.2f, 1f), new Color(0.35f, 0.28f, 0.22f), -50);
                    if (pathSprite != null) {
                        tile.GetComponent<SpriteRenderer>().sprite = pathSprite;
                        tile.GetComponent<SpriteRenderer>().color = Color.white;
                    } else {
                        tile.GetComponent<SpriteRenderer>().sprite = square;
                    }
                }
            }
        }
    }

    void CreateEnvironment()
    {
        GameObject env = new GameObject("Environment");
        Sprite treeSprite = LoadSprite("Square"); // Placeholder for tree, ideally circle
        Sprite crystalSprite = LoadSprite("Square");

        // Trees
        for(int i=0; i<80; i++) {
            Vector3 pos = GetRandomSafePosition();
            if(pos != Vector3.zero) {
                GameObject tree = CreateObj($"Tree_{i}", env.transform, pos, Vector3.one * Random.Range(1.5f, 2.5f), Color.white, -20);
                if (treeSprite != null) {
                    tree.GetComponent<SpriteRenderer>().sprite = treeSprite;
                } else {
                    tree.GetComponent<SpriteRenderer>().sprite = LoadSprite("Circle");
                    tree.GetComponent<SpriteRenderer>().color = new Color(0.1f, 0.35f, 0.1f);
                }
                
                // Kulelerin üstüne koyulmasını engellemek için Collider ekle
                tree.AddComponent<BoxCollider2D>();
                
                // Yanabilir özellik ekle
                tree.AddComponent<FlammableTree>();
            }
        }

        // Crystals are now handled dynamically by GameBootstrapper

    }

    GameObject CreateObj(string name, Transform parent, Vector3 pos, Vector3 scale, Color col, int order)
    {
        GameObject obj = new GameObject(name);
        obj.transform.parent = parent;
        obj.transform.position = pos;
        obj.transform.localScale = scale;
        SpriteRenderer sr = obj.AddComponent<SpriteRenderer>();
        sr.sprite = LoadSprite("Square");
        sr.color = col;
        sr.sortingOrder = order;
        return obj;
    }

    Vector3 GetRandomSafePosition()
    {
        // Simple random position within bounds, excluding path
        // For simplicity in this tool, we will use a basic random without complex path checking to save time
        // or we replicate the logic. Replicating basic bounds:
        float x = Random.Range(-25f, 25f);
        float y = Random.Range(-13f, 13f);
        
        // Basic check against village
        if (x > 8f) return Vector3.zero; // Avoid village roughly
        
        return new Vector3(x, y, 0);
    }

    // --- Helpers ---

    void CreateAndSaveSprite(string name, int size, Color color, string dir)
    {
        string path = $"{dir}/{name}.png";
        if (File.Exists(path)) return;

        Texture2D tex = new Texture2D(size, size);
        Color[] cols = new Color[size*size];
        
        if (name == "Circle") {
            float center = size/2f;
            float radius = (size/2f)-2;
            for(int y=0; y<size; y++) {
                for(int x=0; x<size; x++) {
                    if(Vector2.Distance(new Vector2(x,y), new Vector2(center,center)) <= radius) cols[y*size+x] = color;
                    else cols[y*size+x] = Color.clear;
                }
            }
        } else {
             for(int i=0; i<cols.Length; i++) cols[i] = color;
        }

        tex.SetPixels(cols);
        tex.Apply();
        File.WriteAllBytes(path, tex.EncodeToPNG());
        Debug.Log($"Generated Asset: {path}");
    }

    Sprite LoadSprite(string name)
    {
        return AssetDatabase.LoadAssetAtPath<Sprite>($"Assets/Sprites/{name}.png");
    }
}
#endif
