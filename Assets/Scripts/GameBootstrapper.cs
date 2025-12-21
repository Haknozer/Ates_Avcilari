using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Ateş Avcıları - Yangın Güvenliği Eğitim Oyunu
/// 6-16 yaş arası çocuklar için afet bilinci geliştirme oyunu
/// Yangın alevlerini (düşmanlar) itfaiye istasyonları (kuleler) ile söndür!
/// </summary>
public class GameBootstrapper : MonoBehaviour
{
    // Çoklu yol sistemi - yangın yayılma yolları
    private List<List<Vector3>> allPaths = new List<List<Vector3>>();
    
    // Referanslar
    private List<GameObject> towers = new List<GameObject>();  // İtfaiye istasyonları
    private List<GameObject> enemies = new List<GameObject>(); // Yangın alevleri
    
    // Spawner ayarları
    private float spawnInterval = 2.5f;
    private float spawnTimer = 0f;
    
    // İtfaiye istasyonu ayarları
    private float towerRange = 3.5f;
    private float baseFireRate = 1f;
    
    // Çevre ayarları - orman haritası
    private int treeCount = 80;       // Orman ağaçları
    private int crystalCount = 20;    // Su kaynakları
    private float pathSafeDistance = 1.0f; // Yerleştirme kolaylığı için düşürüldü
    
    // Kamera referansı
    private Camera mainCamera;
    
    // Varsayılan sprite
    // private Sprite defaultSprite; // Artık Editörden alacağız
    [Header("Görseller")]
    public Sprite unitSprite;  // Yuvarlak (LevelArchitect ile oluştu)
    public Sprite towerSprite; // Kare (LevelArchitect ile oluştu)
    public Sprite rangeSprite; // Menzil halkası (Circle)

    // === PARA SİSTEMİ (Su Kaynağı) ===
    public static int gold = 15; // Başlangıç su kaynağı
    public static int score = 0;
    public static int lives = 20; // Köy sağlığı
    
    // === İTFAİYE FİYATLARI ===
    private int towerCost = 15;
    private int speedUpgradeCost = 10;
    private int multiShotUpgradeCost = 20;
    
    // === UI ELEMANLARİ ===
    // private GameObject uiCanvas; // Eski
    private GameObject selectedTower = null;
    
    // Wind System
    private Vector2 windDirection = Vector2.zero;
    private float windStrength = 0f;
    
    // Game State
    private bool isGameActive = true;
    private bool isPlacingTower = false;
    private GameObject towerPreview = null;
    
    // public UIManager uiManager; // İPTAL: Eskiye dönüş
    
    // === YANGIN GÜVENLİĞİ İPUÇLARI ===
    private string[] fireSafetyTips = new string[]
    {
        "🔥 İpucu: Yangın gördüğünde 110'u ara!",
        "🧯 İpucu: Yangın söndürücünün yerini öğren!",
        "🚪 İpucu: Yangında asansör kullanma, merdiveni kullan!",
        "💨 İpucu: Dumanda eğilerek ilerle, duman yukarı çıkar!",
        "🏃 İpucu: Yangında saklanma, hemen dışarı çık!",
        "📞 İpucu: İtfaiye numarası: 110",
        "🔌 İpucu: Kullanmadığın elektrikli aletleri fişten çek!",
        "🕯️ İpucu: Mumları asla yalnız bırakma!",
        "🍳 İpucu: Mutfakta yemek pişirirken dikkatli ol!",
        "🚨 İpucu: Duman dedektörlerini kontrol et!",
        "👨‍👩‍👧 İpucu: Ailen ile yangın kaçış planı yap!",
        "🧒 İpucu: Kibritle oynamak tehlikelidir!",
        "🌲 İpucu: Ormanda ateş yakmak yasaktır!",
        "💧 İpucu: Küçük yangınları su ile söndür!",
        "🏠 İpucu: Evden çıkarken ocağı kontrol et!"
    };
    private int currentTipIndex = 0;
    private float tipTimer = 0f;
    private float tipInterval = 8f;
    
    // === MAĞAZA BUTONLARI ===
    private Rect buyTowerRect;
    private Rect upgradeSpeedRect;
    private Rect upgradeMultiRect;
    private Rect goldDisplayRect;
    
    void Start()
    {
        // Başlangıç değerleri
        gold = 15;
        score = 0;
        lives = 20;
        
        // Kamera ayarları - orman haritası
        mainCamera = Camera.main;
        mainCamera.backgroundColor = new Color(0.08f, 0.22f, 0.08f); // Koyu orman yeşili
        mainCamera.orthographicSize = 12f;
        
        // Varsayılan sprite'ı oluştur
        // defaultSprite = CreateCircleSprite();
        
        // Oyunu oluştur
        // CreateBackground(); // Artık sahnede var
        CreatePath();       // Sadece verileri hesapla, çizim yapma
        CreateCrystals(); // SADECE Kristaller kod ile oluşsun
        StartCoroutine(SpawnEnemyRoutine());
        StartCoroutine(WindRoutine()); // Rüzgar sistemini başlat
        
        Debug.Log("🔥 ATEŞ AVCILARI - Yangın Güvenliği Eğitim Oyunu");
        Debug.Log(" Köyü yangından koru! İtfaiye istasyonları kur!");
        Debug.Log(" Su Kaynağı: 15 | ❤️ Köy Sağlığı: 10");
        // if(!uiManager) uiManager = FindFirstObjectByType<UIManager>();
        // uiManager.UpdateTip(fireSafetyTips[0]);
    }
    
    void Update()
    {
        // Oyun bitti mi kontrol et
        if (lives <= 0)
        {
            // Oyun kaybedildi - düşman spawn'ı durdur
            // if(uiManager) uiManager.ShowGameOver();
            return;
        }
        
        HandleMouseClick();
        HandleTowerPlacement();
        
        // Yangın spawn
        spawnTimer += Time.deltaTime;
        if (spawnTimer >= spawnInterval)
        {
            SpawnEnemy();
            spawnTimer = 0f;
        }
        
        // Yangın güvenliği ipucu göster
        tipTimer += Time.deltaTime;
        if (tipTimer >= tipInterval)
        {
            currentTipIndex = (currentTipIndex + 1) % fireSafetyTips.Length;
            currentTipIndex = (currentTipIndex + 1) % fireSafetyTips.Length;
            Debug.Log(fireSafetyTips[currentTipIndex]);
            // if(uiManager) uiManager.UpdateTip(fireSafetyTips[currentTipIndex]);
            tipTimer = 0f;
        }
        
        MoveEnemies();
        TowerAttack();
        enemies.RemoveAll(e => e == null);
    }
    
    // === GUI SİSTEMİ (ALT BAR) ===
    // === GUI SİSTEMİ (ALT BAR) ===
    void OnGUI()
    {
        // Stil oluştur
        GUIStyle boxStyle = new GUIStyle(GUI.skin.box);
        boxStyle.fontSize = 14;
        boxStyle.alignment = TextAnchor.MiddleCenter;
        
        GUIStyle buttonStyle = new GUIStyle(GUI.skin.button);
        buttonStyle.fontSize = 12;
        buttonStyle.fontStyle = FontStyle.Bold;
        
        GUIStyle labelStyle = new GUIStyle(GUI.skin.label);
        labelStyle.fontSize = 16;
        labelStyle.fontStyle = FontStyle.Bold;
        labelStyle.normal.textColor = Color.yellow;
        
        // === ÜST BAR - Yangın Güvenliği İpucu ===
        GUIStyle tipStyle = new GUIStyle(GUI.skin.box);
        tipStyle.fontSize = 16;
        tipStyle.fontStyle = FontStyle.Bold;
        tipStyle.alignment = TextAnchor.MiddleCenter;
        tipStyle.normal.textColor = Color.white;
        
        GUI.backgroundColor = new Color(0.8f, 0.3f, 0.1f, 0.9f);
        GUI.Box(new Rect(Screen.width/2 - 250, 10, 500, 35), fireSafetyTips[currentTipIndex], tipStyle);

        // Wind Indicator UI
        string windArrow = "o";
        if (windStrength > 0.1f)
        {
            if (Mathf.Abs(windDirection.x) > Mathf.Abs(windDirection.y))
                windArrow = windDirection.x > 0 ? ">> (Doğu)" : "<< (Batı)";
            else
                windArrow = windDirection.y > 0 ? "^^ (Kuzey)" : "vv (Güney)";
        }
        GUI.Label(new Rect(20, 150, 250, 30), $"Rüzgar: {windArrow}", labelStyle);
        
        // === OYUN BİTTİ EKRANI ===
        if (lives <= 0)
        {
            GUIStyle gameOverStyle = new GUIStyle(GUI.skin.box);
            gameOverStyle.fontSize = 32;
            gameOverStyle.fontStyle = FontStyle.Bold;
            gameOverStyle.alignment = TextAnchor.MiddleCenter;
            gameOverStyle.normal.textColor = Color.white;
            
            GUI.backgroundColor = new Color(0.8f, 0.2f, 0.1f, 0.95f);
            GUI.Box(new Rect(Screen.width/2 - 200, Screen.height/2 - 80, 400, 160), 
                $"🔥 YANGIN YAYILDI! 🔥\n\nSkor: {score}\n\n📚 Yangın güvenliği ipuçlarını\nhatırlamayı unutma!", gameOverStyle);
            
            GUI.backgroundColor = Color.white;
            return;
        }
        
        // === ALT BAR ===
        float barHeight = 85;
        GUI.backgroundColor = new Color(0.2f, 0.15f, 0.1f, 0.95f);
        GUI.Box(new Rect(0, Screen.height - barHeight, Screen.width, barHeight), "", boxStyle);
        
        // Su Kaynağı (Altın) gösterimi
        labelStyle.normal.textColor = new Color(0.4f, 0.8f, 1f);
        GUI.Label(new Rect(20, Screen.height - barHeight + 5, 150, 25), $" Su: {gold}", labelStyle);
        
        // Skor
        labelStyle.normal.textColor = Color.yellow;
        GUI.Label(new Rect(20, Screen.height - barHeight + 30, 150, 25), $"⭐ Skor: {score}", labelStyle);
        
        // Köy Sağlığı (Can)
        labelStyle.normal.textColor = lives > 3 ? Color.green : Color.red;
        GUI.Label(new Rect(20, Screen.height - barHeight + 55, 150, 25), $"❤️ Köy: {lives}", labelStyle);
        
        // İtfaiye İstasyonu Satın Al butonu
        buyTowerRect = new Rect(170, Screen.height - barHeight + 15, 150, 55);
        GUI.backgroundColor = gold >= towerCost ? new Color(0.3f, 0.7f, 1f) : Color.gray;
        if (GUI.Button(buyTowerRect, $" İtfaiye Kur\n({towerCost} Su)", buttonStyle))
        {
            if (gold >= towerCost && !isPlacingTower)
            {
                StartTowerPlacement();
            }
        }
        
        // Seçili istasyon varsa güçlendirme butonlarını göster
        if (selectedTower != null)
        {
            TowerController tc = selectedTower.GetComponent<TowerController>();
            if (tc != null)
            {
                // Hızlı Püskürtme Güçlendirmesi
                upgradeSpeedRect = new Rect(340, Screen.height - barHeight + 15, 160, 55);
                GUI.backgroundColor = gold >= speedUpgradeCost ? new Color(0.3f, 1f, 0.3f) : Color.gray;
                string speedText = $"⚡ Hızlı Püskürt Lv{tc.speedLevel}\n({speedUpgradeCost} Su)";
                if (GUI.Button(upgradeSpeedRect, speedText, buttonStyle))
                {
                    if (gold >= speedUpgradeCost)
                    {
                        UpgradeSpeed(tc);
                    }
                }
                
                // Çoklu Hedef Güçlendirmesi
                upgradeMultiRect = new Rect(520, Screen.height - barHeight + 15, 160, 55);
                GUI.backgroundColor = gold >= multiShotUpgradeCost ? new Color(0.3f, 0.6f, 1f) : Color.gray;
                string multiText = $" Çoklu Hedef Lv{tc.multiShotLevel}\n({multiShotUpgradeCost} Su)";
                if (GUI.Button(upgradeMultiRect, multiText, buttonStyle))
                {
                    if (gold >= multiShotUpgradeCost)
                    {
                        UpgradeMultiShot(tc);
                    }
                }
                
                // Seçili istasyon bilgisi
                labelStyle.normal.textColor = Color.cyan;
                GUI.Label(new Rect(700, Screen.height - barHeight + 20, 200, 50), 
                    $"🚒 İtfaiye Seçili\nHız: Lv{tc.speedLevel} | Hedef: Lv{tc.multiShotLevel}", labelStyle);
            }
        }
        
        // İstasyon yerleştirme modu bildirimi
        if (isPlacingTower)
        {
            GUIStyle infoStyle = new GUIStyle(GUI.skin.box);
            infoStyle.fontSize = 18;
            infoStyle.fontStyle = FontStyle.Bold;
            infoStyle.alignment = TextAnchor.MiddleCenter;
            infoStyle.normal.textColor = Color.white;
            
            GUI.backgroundColor = new Color(0.2f, 0.5f, 0.8f, 0.9f);
            GUI.Box(new Rect(Screen.width/2 - 180, 55, 360, 40), " İtfaiye istasyonu yerleştirmek için tıkla!", infoStyle);
        }
        
        GUI.backgroundColor = Color.white;
    }
    
    // === KULE YERLEŞTİRME ===
    public void StartTowerPlacement() // Public yaptik
    {
        isPlacingTower = true;
        
        // Önizleme kule oluştur
        towerPreview = new GameObject("TowerPreview");
        SpriteRenderer sr = towerPreview.AddComponent<SpriteRenderer>();
        sr.sprite = towerSprite; // Editörden gelen
        sr.color = new Color(0.2f, 0.5f, 0.9f, 0.5f);
        towerPreview.transform.localScale = new Vector3(0.8f, 0.8f, 1f);
        sr.sortingOrder = 100;
        
        Debug.Log("🎯 Kule yerleştirme modu aktif!");
    }
    
    void HandleTowerPlacement()
    {
        if (!isPlacingTower || towerPreview == null) return;
        
        Mouse mouse = Mouse.current;
        if (mouse == null) return;
        
        Vector3 mousePos = mainCamera.ScreenToWorldPoint(mouse.position.ReadValue());
        mousePos.z = 0;
        towerPreview.transform.position = mousePos;
        
        // Yola yakınlık kontrolü - renk değişimi
        SpriteRenderer sr = towerPreview.GetComponent<SpriteRenderer>();
        if (IsNearPath(mousePos))
        {
            sr.color = new Color(1f, 0.3f, 0.3f, 0.5f); // Kırmızı - yerleştirilemez
        }
        // Kule üstüne kule kontrolü
        else if (IsOverlappingTower(mousePos))
        {
            sr.color = new Color(1f, 0.3f, 0.3f, 0.5f); // Kırmızı - yerleştirilemez
        }
        else
        {
            sr.color = new Color(0.2f, 0.5f, 0.9f, 0.5f); // Mavi - yerleştirilebilir
        }
        
        // Sol tık ile yerleştir
        if (mouse.leftButton.wasPressedThisFrame)
        {
            // UI alanı kontrolü
            if (mousePos.y < mainCamera.ScreenToWorldPoint(new Vector3(0, 80, 0)).y)
            {
                return; // UI alanında tıklandı
            }
            
            if (!IsNearPath(mousePos) && !IsOverlappingTower(mousePos))
            {
                PlaceTower(mousePos);
                gold -= towerCost;
                isPlacingTower = false;
                Destroy(towerPreview);
                towerPreview = null;
                Debug.Log($"🗼 Kule yerleştirildi! Kalan altın: {gold}");
            }
            else
            {
                Debug.Log("❌ Buraya kule yerleştirilemez! Yoldan uzakta bir yer seç.");
            }
        }
        
        // Sağ tık ile iptal
        if (mouse.rightButton.wasPressedThisFrame)
        {
            isPlacingTower = false;
            Destroy(towerPreview);
            towerPreview = null;
            Debug.Log("❌ Kule yerleştirme iptal edildi.");
        }
    }
    
    void PlaceTower(Vector3 position)
    {
        GameObject tower = new GameObject("Tower");
        SpriteRenderer sr = tower.AddComponent<SpriteRenderer>();
        sr.sprite = towerSprite; // Editörden gelen sprite
        sr.color = new Color(0.2f, 0.5f, 0.9f);
        tower.transform.position = position;
        tower.transform.localScale = new Vector3(0.8f, 0.8f, 1f);
        sr.sortingOrder = 5;
        
        // Menzil göstergesi
        GameObject rangeIndicator = new GameObject("RangeIndicator");
        rangeIndicator.transform.parent = tower.transform;
        rangeIndicator.transform.localPosition = Vector3.zero;
        
        SpriteRenderer rangeSr = rangeIndicator.AddComponent<SpriteRenderer>();
        rangeSr.sprite = rangeSprite; // Menzil göstergesi yuvarlak
        rangeSr.color = new Color(0.3f, 0.6f, 1f, 0.15f);
        rangeIndicator.transform.localScale = new Vector3(towerRange * 2.5f, towerRange * 2.5f, 1f);
        rangeSr.sortingOrder = 0;
        
        // Collider ekle (tıklama için)
        BoxCollider2D col = tower.AddComponent<BoxCollider2D>();
        col.size = new Vector2(1f, 1f);
        
        TowerController tc = tower.AddComponent<TowerController>();
        tc.lastFireTime = -baseFireRate;
        tc.speedLevel = 1;
        tc.multiShotLevel = 1;
        tc.fireRate = baseFireRate;
        
        towers.Add(tower);
    }
    
    // === GÜÇLENDİRMELER ===
    public void UpgradeSpeed(TowerController tc) // Public yaptik
    {
        gold -= speedUpgradeCost;
        tc.speedLevel++;
        tc.fireRate = baseFireRate / (1f + (tc.speedLevel - 1) * 0.3f);
        
        // Görsel güncelleme
        SpriteRenderer sr = tc.GetComponent<SpriteRenderer>();
        if (sr != null)
        {
            sr.color = new Color(0.2f + tc.speedLevel * 0.1f, 0.5f, 0.9f);
        }
        
        Debug.Log($"⚡ Hızlı Ateş Lv{tc.speedLevel}! Yeni ateş hızı: {tc.fireRate:F2}s");
    }
    
    public void UpgradeMultiShot(TowerController tc) // Public yaptik
    {
        gold -= multiShotUpgradeCost;
        tc.multiShotLevel++;
        
        // Görsel güncelleme
        SpriteRenderer sr = tc.GetComponent<SpriteRenderer>();
        if (sr != null)
        {
            sr.color = new Color(0.9f, 0.3f + tc.multiShotLevel * 0.1f, 0.2f);
        }
        
        Debug.Log($"🔥 Çoklu Ateş Lv{tc.multiShotLevel}! Aynı anda {tc.multiShotLevel} düşmana ateş!");
    }
    
    // === PARA KAZANMA ===
    public static void AddGold(int amount)
    {
        gold += amount;
        Debug.Log($"💰 +{amount} Altın! Toplam: {gold}");
    }
    
    public static void AddScore(int amount)
    {
        score += amount;
    }
    
    /// <summary>
    /// Yuvarlak sprite oluşturur
    /// </summary>
    Sprite CreateCircleSprite()
    {
        int size = 128;
        Texture2D texture = new Texture2D(size, size);
        Color[] colors = new Color[size * size];
        
        float center = size / 2f;
        float radius = size / 2f - 2f;
        
        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float dist = Vector2.Distance(new Vector2(x, y), new Vector2(center, center));
                if (dist <= radius)
                {
                    colors[y * size + x] = Color.white;
                }
                else
                {
                    colors[y * size + x] = Color.clear;
                }
            }
        }
        
        texture.SetPixels(colors);
        texture.Apply();
        texture.filterMode = FilterMode.Bilinear;
        
        return Sprite.Create(texture, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), size);
    }
    
    /// <summary>
    /// Kare sprite oluşturur
    /// </summary>
    Sprite CreateSquareSprite()
    {
        int size = 64;
        Texture2D texture = new Texture2D(size, size);
        Color[] colors = new Color[size * size];
        
        for (int i = 0; i < colors.Length; i++)
        {
            colors[i] = Color.white;
        }
        
        texture.SetPixels(colors);
        texture.Apply();
        
        return Sprite.Create(texture, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), size);
    }
    
    /// <summary>
    /// Fare tıklaması (New Input System)
    /// </summary>
    void HandleMouseClick()
    {
        // Kule yerleştirme modundaysa bu fonksiyonu atla
        if (isPlacingTower) return;
        
        Mouse mouse = Mouse.current;
        if (mouse == null) return;
        
        if (mouse.leftButton.wasPressedThisFrame)
        {
            Vector2 mousePos = mainCamera.ScreenToWorldPoint(mouse.position.ReadValue());
            
            // UI alanı kontrolü (alt 80 piksel)
            if (mouse.position.ReadValue().y < 80) return;
            
            // Tıklanan noktadaki objeyi bul (OverlapPoint daha hassas)
            Collider2D hitCol = Physics2D.OverlapPoint(mousePos);
            
            if (hitCol != null)
            {
                // Debug.Log($"🖱️ Tıklanan: {hitCol.name}"); // Debug

                // Kristal kontrolü
                CrystalCollector crystal = hitCol.GetComponent<CrystalCollector>();
                if (crystal != null)
                {
                    crystal.Collect();
                    return;
                }
                
                // Kule seçimi kontrolü
                TowerController tower = hitCol.GetComponent<TowerController>();
                if (tower != null)
                {
                    SelectTower(hitCol.gameObject);
                    return;
                }
            }
            
            // Boş alana tıklandıysa seçimi kaldır
            selectedTower = null;
        }
    }
    
    void SelectTower(GameObject tower)
    {
        selectedTower = tower;
        Debug.Log("🗼 Kule seçildi! Güçlendirme seçenekleri için alt bara bak.");
        
        // Seçim efekti
        SpriteRenderer sr = tower.GetComponent<SpriteRenderer>();
        if (sr != null)
        {
            StartCoroutine(SelectionFlash(sr));
        }
    }
    
    IEnumerator SelectionFlash(SpriteRenderer sr)
    {
        Color original = sr.color;
        sr.color = Color.white;
        yield return new WaitForSeconds(0.1f);
        sr.color = original;
    }
    
    /// <summary>
    /// Orman zemini ve sağ tarafta köy oluşturur
    /// </summary>
    void CreateBackground()
    {
        Sprite squareSprite = CreateSquareSprite();
        
        // Büyük yeşil orman zemini
        GameObject ground = new GameObject("Ground");
        SpriteRenderer sr = ground.AddComponent<SpriteRenderer>();
        sr.sprite = squareSprite;
        sr.color = new Color(0.18f, 0.38f, 0.18f); // Orman yeşili
        ground.transform.position = new Vector3(0, 0, 10);
        ground.transform.localScale = new Vector3(60f, 30f, 1f);
        sr.sortingOrder = -100;
        
        // === SAĞ TARAFTA KÖY (Korumamız gereken alan) ===
        Vector3 villageCenter = new Vector3(15f, 0f, 0f);
        
        // Köy zemini (açık kahverengi)
        GameObject villageGround = new GameObject("VillageGround");
        SpriteRenderer villageSr = villageGround.AddComponent<SpriteRenderer>();
        villageSr.sprite = squareSprite;
        villageSr.color = new Color(0.45f, 0.38f, 0.28f);
        villageGround.transform.position = new Vector3(villageCenter.x, villageCenter.y, 9);
        villageGround.transform.localScale = new Vector3(10f, 18f, 1f);
        villageSr.sortingOrder = -95;
        
        // Ana köy binası (korunacak merkez)
        GameObject mainBuilding = new GameObject("MainBuilding");
        SpriteRenderer mainSr = mainBuilding.AddComponent<SpriteRenderer>();
        mainSr.sprite = squareSprite;
        mainSr.color = new Color(0.7f, 0.55f, 0.35f); // Ahşap ev
        mainBuilding.transform.position = new Vector3(villageCenter.x, villageCenter.y, 4);
        mainBuilding.transform.localScale = new Vector3(4f, 3f, 1f);
        mainSr.sortingOrder = -35;
        
        // Ev çatısı
        GameObject roof = new GameObject("Roof");
        SpriteRenderer roofSr = roof.AddComponent<SpriteRenderer>();
        roofSr.sprite = squareSprite;
        roofSr.color = new Color(0.6f, 0.25f, 0.15f); // Kırmızı çatı
        roof.transform.position = new Vector3(villageCenter.x, villageCenter.y + 2f, 3);
        roof.transform.localScale = new Vector3(5f, 1.5f, 1f);
        roofSr.sortingOrder = -30;
        
        // Küçük evler
        Vector3[] smallHousePositions = new Vector3[]
        {
            new Vector3(villageCenter.x - 3f, villageCenter.y + 4f, 4),
            new Vector3(villageCenter.x + 3f, villageCenter.y + 4f, 4),
            new Vector3(villageCenter.x - 3f, villageCenter.y - 4f, 4),
            new Vector3(villageCenter.x + 3f, villageCenter.y - 4f, 4)
        };
        
        foreach (Vector3 housePos in smallHousePositions)
        {
            GameObject house = new GameObject("SmallHouse");
            SpriteRenderer houseSr = house.AddComponent<SpriteRenderer>();
            houseSr.sprite = squareSprite;
            houseSr.color = new Color(0.65f, 0.5f, 0.3f);
            house.transform.position = housePos;
            house.transform.localScale = new Vector3(2f, 1.5f, 1f);
            houseSr.sortingOrder = -36;
        }
        
        // Köy koruma çemberi (görsel)
        GameObject protectionCircle = new GameObject("ProtectionCircle");
        SpriteRenderer protSr = protectionCircle.AddComponent<SpriteRenderer>();
        protSr.sprite = unitSprite;
        protSr.color = new Color(0.3f, 0.7f, 0.3f, 0.2f); // Yeşil koruma alanı
        protectionCircle.transform.position = new Vector3(villageCenter.x, villageCenter.y, 6);
        protectionCircle.transform.localScale = new Vector3(12f, 12f, 1f);
        protSr.sortingOrder = -90;
        
        // "KÖY" yazısı yerine basit işaret
        Debug.Log("🏠 Köy sağ tarafta oluşturuldu - Yangından koruyun!");
    }
    
    /// <summary>
    /// Yangın yayılma yolları - Soldan sağdaki köye doğru
    /// </summary>
    void CreatePath()
    {
        // Sprite squareSprite = CreateSquareSprite();
        // GameObject pathParent = new GameObject("FirePaths");
        
        // Köy merkezi (hedef nokta)
        Vector3 villageTarget = new Vector3(12f, 0f, 0f);
        
        // Yol rengi - yanık toprak/kül rengi
        Color firePathColor = new Color(0.35f, 0.28f, 0.22f);
        
        // Yangın başlangıç noktası rengi (ateş kırmızısı)
        Color fireSpawnColor = new Color(1f, 0.35f, 0.15f);
        
        // === YOL 1: SOL ORTA (Ana yol) ===
        List<Vector3> path1 = new List<Vector3>()
        {
            new Vector3(-20f, 0f, 0f),    // Yangın başlangıcı
            new Vector3(-14f, 0f, 0f),
            new Vector3(-14f, 3f, 0f),
            new Vector3(-8f, 3f, 0f),
            new Vector3(-8f, -1f, 0f),
            new Vector3(-2f, -1f, 0f),
            new Vector3(-2f, 0f, 0f),
            new Vector3(4f, 0f, 0f),
            new Vector3(8f, 0f, 0f),
            villageTarget                  // Köye ulaşım
        };
        allPaths.Add(path1);
        
        // === YOL 2: SOL ÜST (Kuzeybatı) ===
        List<Vector3> path2 = new List<Vector3>()
        {
            new Vector3(-18f, 8f, 0f),    // Yangın başlangıcı
            new Vector3(-12f, 8f, 0f),
            new Vector3(-12f, 5f, 0f),
            new Vector3(-6f, 5f, 0f),
            new Vector3(-6f, 2f, 0f),
            new Vector3(0f, 2f, 0f),
            new Vector3(0f, 4f, 0f),
            new Vector3(6f, 4f, 0f),
            new Vector3(10f, 2f, 0f),
            villageTarget                  // Köye ulaşım
        };
        allPaths.Add(path2);
        
        // === YOL 3: SOL ALT (Güneybatı) ===
        List<Vector3> path3 = new List<Vector3>()
        {
            new Vector3(-18f, -7f, 0f),   // Yangın başlangıcı
            new Vector3(-12f, -7f, 0f),
            new Vector3(-12f, -4f, 0f),
            new Vector3(-5f, -4f, 0f),
            new Vector3(-5f, -6f, 0f),
            new Vector3(2f, -6f, 0f),
            new Vector3(2f, -3f, 0f),
            new Vector3(8f, -3f, 0f),
            new Vector3(10f, -1f, 0f),
            villageTarget                  // Köye ulaşım
        };
        allPaths.Add(path3);
        
        // === YOL 4: KUZEY (Üstten) ===
        List<Vector3> path4 = new List<Vector3>()
        {
            new Vector3(-5f, 10f, 0f),    // Yangın başlangıcı
            new Vector3(-5f, 7f, 0f),
            new Vector3(0f, 7f, 0f),
            new Vector3(0f, 5f, 0f),
            new Vector3(5f, 5f, 0f),
            new Vector3(5f, 3f, 0f),
            new Vector3(10f, 2f, 0f),
            villageTarget                  // Köye ulaşım
        };
        allPaths.Add(path4);
        
        Debug.Log($"🔥 {allPaths.Count} yangın yayılma yolu hesaplandı!");
    }
    
    /// <summary>
    /// Çevre elemanları oluşturur
    /// </summary>
    /// <summary>
    /// Kristaller oluşturur (Dinamik ve rastgele)
    /// </summary>
    void CreateCrystals()
    {
        GameObject envParent = GameObject.Find("Environment");
        if (!envParent) envParent = new GameObject("Environment");
        
        // Önceki kristalleri temizle (varsa)
        foreach(Transform child in envParent.transform)
        {
            if (child.name.StartsWith("Crystal")) Destroy(child.gameObject);
        }

        // Kristaller
        Sprite squareSprite = CreateSquareSprite(); // Veya project'ten çekilebilir ama kodla oluşturmak garanti
        for (int i = 0; i < crystalCount; i++)
        {
            Vector3 pos = GetRandomSafePosition();
            if (pos == Vector3.zero) continue;
            
            GameObject crystal = new GameObject($"Crystal_{i}");
            crystal.transform.parent = envParent.transform;
            
            SpriteRenderer sr = crystal.AddComponent<SpriteRenderer>();
            sr.sprite = squareSprite;
            sr.color = new Color(0.3f, 0.8f, 1f); // Mavi kristal
            
            crystal.transform.position = pos;
            crystal.transform.localScale = new Vector3(0.35f, 0.35f, 1f);
            crystal.transform.rotation = Quaternion.Euler(0, 0, 45); // 45 derece döndür (elmas şekli)
            sr.sortingOrder = -10;
            
            // Collider ve script (KESİN EKLENMELİ)
            BoxCollider2D col = crystal.GetComponent<BoxCollider2D>();
            if (col == null) col = crystal.AddComponent<BoxCollider2D>();
            
            if(crystal.GetComponent<CrystalCollector>() == null) crystal.AddComponent<CrystalCollector>();
            col.size = new Vector2(1.5f, 1.5f);
        }
        
        Debug.Log($"💎 {crystalCount} kristal oluşturuldu");
    }
    
    /// <summary>
    /// Yoldan uzak rastgele pozisyon - büyük harita
    /// </summary>
    Vector3 GetRandomSafePosition()
    {
        for (int attempt = 0; attempt < 100; attempt++)
        {
            float x = Random.Range(-18f, 18f);
            float y = Random.Range(-9f, 9f);
            Vector3 pos = new Vector3(x, y, 0);
            
            if (!IsNearPath(pos))
            {
                return pos;
            }
        }
        return Vector3.zero;
    }
    
    bool IsNearPath(Vector3 pos)
    {
        // Köy merkezine yakın mı kontrol et (köy sağda x=15)
        Vector3 villageCenter = new Vector3(15f, 0f, 0f);
        if (Vector3.Distance(pos, villageCenter) < 6f)
        {
            return true; // Köy alanına yerleştirme
        }
        
        // Tüm yolları kontrol et
        foreach (List<Vector3> path in allPaths)
        {
            for (int i = 0; i < path.Count - 1; i++)
            {
                Vector2 start = path[i];
                Vector2 end = path[i + 1];
                Vector2 p = pos;
                
                Vector2 line = end - start;
                float len = line.magnitude;
                if (len == 0) continue;
                
                Vector2 dir = line / len;
                float proj = Mathf.Clamp(Vector2.Dot(p - start, dir), 0, len);
                Vector2 closest = start + dir * proj;
                
                if (Vector2.Distance(p, closest) < pathSafeDistance)
                {
                    return true;
                }
            }
        }
        return false;
    }

    bool IsOverlappingTower(Vector3 pos)
    {
        // 1.5 birim yarıçapta başka collider (kule/kristal) var mı?
        Collider2D hit = Physics2D.OverlapCircle(pos, 1.5f);
        if (hit != null)
        {
            // Eğer çarptığımız şey kule veya kristalse true döndür
            if (hit.GetComponent<TowerController>() != null || hit.GetComponent<CrystalCollector>() != null)
            {
                return true;
            }
        }
        return false;
    }
    
    /// <summary>
    /// Kuleler oluşturur
    /// </summary>
    void CreateTowers()
    {
        Sprite squareSprite = CreateSquareSprite();
        GameObject towersParent = new GameObject("Towers");
        
        Vector3[] positions = new Vector3[]
        {
            new Vector3(-6f, 1.5f, 0),
            new Vector3(-2.5f, 4f, 0),
            new Vector3(1f, -3.5f, 0),
            new Vector3(4.5f, 3f, 0)
        };
        
        foreach (Vector3 pos in positions)
        {
            GameObject tower = new GameObject("Tower");
            tower.transform.parent = towersParent.transform;
            
            SpriteRenderer sr = tower.AddComponent<SpriteRenderer>();
            sr.sprite = squareSprite;
            sr.color = new Color(0.2f, 0.5f, 0.9f); // Mavi kule
            
            tower.transform.position = pos;
            tower.transform.localScale = new Vector3(0.8f, 0.8f, 1f);
            tower.transform.localScale = new Vector3(0.8f, 0.8f, 1f);
            sr.sortingOrder = 5;

            // Collider ekle (Çakışma ve tıklama için şart)
            tower.AddComponent<BoxCollider2D>();
            
            // Menzil göstergesi (yarı saydam daire)
            GameObject rangeIndicator = new GameObject("RangeIndicator");
            rangeIndicator.transform.parent = tower.transform;
            rangeIndicator.transform.localPosition = Vector3.zero;
            
            SpriteRenderer rangeSr = rangeIndicator.AddComponent<SpriteRenderer>();
            rangeSr.sprite = rangeSprite;
            rangeSr.color = new Color(0.3f, 0.6f, 1f, 0.15f);
            rangeIndicator.transform.localScale = new Vector3(towerRange * 2.5f, towerRange * 2.5f, 1f);
            rangeSr.sortingOrder = 0;
            
            TowerController tc = tower.AddComponent<TowerController>();
            tc.lastFireTime = -baseFireRate;
            
            towers.Add(tower);
        }
        
        Debug.Log($"🗼 {towers.Count} kule yerleştirildi");
    }
    
    /// <summary>
    /// Yangın alevi spawn - rastgele yoldan
    /// </summary>
    void SpawnEnemy()
    {
        if (allPaths.Count == 0) return;
        
        // Rastgele bir yangın yolu seç
        int randomPathIndex = Random.Range(0, allPaths.Count);
        List<Vector3> selectedPath = allPaths[randomPathIndex];
        
        if (selectedPath.Count == 0) return;
        
        // Yangın alevi oluştur (hepsi kırmızı-turuncu)
        GameObject fire = new GameObject($"Fire_{randomPathIndex}");
        SpriteRenderer sr = fire.AddComponent<SpriteRenderer>();
        sr.sprite = unitSprite; // Yuvarlak ateş
        
        sr.sprite = unitSprite; // Yuvarlak ateş
        sr.sortingOrder = 20;
        
        fire.transform.position = selectedPath[0]; // Başlangıç noktası
        
        EnemyController ec = fire.AddComponent<EnemyController>();
        ec.pathIndex = 0;
        ec.pathPoints = selectedPath;
        
        // Düşman Tipi Belirleme
        float rand = Random.value;
        if (rand < 0.2f) // %20 İhtimalle "Kıvılcım" (Hızlı, Az Can)
        {
            ec.type = EnemyType.Spark;
            ec.speed = Random.Range(2.5f, 3.5f);
            ec.health = 1;
            sr.color = new Color(1f, 1f, 0.4f); // Sarımtırak
            fire.transform.localScale = new Vector3(0.5f, 0.5f, 1f);
            fire.name += "_Spark";
        }
        else if (rand < 0.4f) // %20 İhtimalle "Petrol/Yağ" (Yavaş, Çok Can)
        {
            ec.type = EnemyType.Oil;
            ec.speed = Random.Range(0.8f, 1.2f);
            ec.health = 8;
            sr.color = new Color(0.4f, 0f, 0.4f); // Koyu mor/siyahımsı
            fire.transform.localScale = new Vector3(0.9f, 0.9f, 1f);
            fire.name += "_Oil";
        }
        else // %60 Normal
        {
            ec.type = EnemyType.Normal;
            ec.speed = Random.Range(1.2f, 2.2f);
            ec.health = 3;
            // Rastgele ateş rengi (kırmızı-turuncu tonları)
            sr.color = new Color(Random.Range(0.9f, 1f), Random.Range(0.2f, 0.5f), Random.Range(0.05f, 0.2f));
            fire.transform.localScale = new Vector3(0.7f, 0.7f, 1f);
        }
        
        CircleCollider2D col = fire.AddComponent<CircleCollider2D>();
        col.radius = 0.4f;
        col.isTrigger = true;
        
        enemies.Add(fire);
    }
    
    /// <summary>
    /// Yangın hareketi - köye doğru yayılıyor
    /// </summary>
    void MoveEnemies()
    {
        for (int i = enemies.Count - 1; i >= 0; i--)
        {
            GameObject fire = enemies[i];
            if (fire == null) continue;
            
            EnemyController ec = fire.GetComponent<EnemyController>();
            if (ec == null || ec.pathPoints == null) continue;
            
            if (ec.pathIndex < ec.pathPoints.Count)
            {
                Vector3 target = ec.pathPoints[ec.pathIndex];
                Vector3 dir = (target - fire.transform.position).normalized;
                
                // Rüzgar Etkisi
                // Eğer rüzgar ateşle aynı yöndeyse hızlandır, tersine yavaşlat
                float windFactor = Vector2.Dot(dir, windDirection.normalized) * windStrength;
                float finalSpeed = ec.speed + windFactor;
                if (finalSpeed < 0.5f) finalSpeed = 0.5f; // Çok yavaşlamasın
                
                fire.transform.position += dir * finalSpeed * Time.deltaTime;
                
                // Ateş Yayılma (Ağaçlara)
                if (Random.value < 0.02f) // Her frame %2 şans (düşük ama sürekli kontrol)
                {
                    Collider2D[] hits = Physics2D.OverlapCircleAll(fire.transform.position, 1.2f);
                    foreach(var hit in hits)
                    {
                        FlammableTree tree = hit.GetComponent<FlammableTree>();
                        if (tree != null) tree.Ignite();
                    }
                }
                
                // Ateş titreme efekti
                float flicker = Mathf.Sin(Time.time * 10f + i) * 0.05f;
                fire.transform.localScale = new Vector3(0.7f + flicker, 0.7f + flicker, 1f);
                
                if (Vector3.Distance(fire.transform.position, target) < 0.15f)
                {
                    ec.pathIndex++;
                }
            }
            else
            {
                // Yangın köye ulaştı!
                enemies.RemoveAt(i);
                Destroy(fire);
                lives--;
                Debug.Log($"<color=red>🔥 YANGIN KÖYE ULAŞTI! Kalan can: {lives}</color>");
                
                if (lives <= 0)
                {
                    Debug.Log("<color=red>💔 KÖY YANDI! OYUN BİTTİ!</color>");
                    Debug.Log("📚 Yangın güvenliği ipuçlarını hatırla!");
                }
            }
        }
    }
    
    /// <summary>
    /// Kule saldırısı
    /// </summary>
    void TowerAttack()
    {
        foreach (GameObject tower in towers)
        {
            if (tower == null) continue;
            
            TowerController tc = tower.GetComponent<TowerController>();
            if (tc == null) continue;
            
            // Kule'nin kendi fireRate'ini kullan
            if (Time.time - tc.lastFireTime < tc.fireRate) continue;
            
            // Menzildeki düşmanları bul ve mesafeye göre sırala
            List<(GameObject enemy, float dist)> enemiesInRange = new List<(GameObject, float)>();
            
            foreach (GameObject enemy in enemies)
            {
                if (enemy == null) continue;
                float dist = Vector3.Distance(tower.transform.position, enemy.transform.position);
                if (dist < towerRange)
                {
                    enemiesInRange.Add((enemy, dist));
                }
            }
            
            // Mesafeye göre sırala
            enemiesInRange.Sort((a, b) => a.dist.CompareTo(b.dist));
            
            // Çoklu ateş seviyesine göre hedef sayısı
            int targetsToShoot = Mathf.Min(tc.multiShotLevel, enemiesInRange.Count);
            
            if (targetsToShoot > 0)
            {
                for (int i = 0; i < targetsToShoot; i++)
                {
                    FireBullet(tower, enemiesInRange[i].enemy);
                }
                tc.lastFireTime = Time.time;
            }
        }
    }
    
    /// <summary>
    /// Su püskürt - yangını söndür
    /// </summary>
    void FireBullet(GameObject tower, GameObject target)
    {
        GameObject water = new GameObject("Water");
        SpriteRenderer sr = water.AddComponent<SpriteRenderer>();
        sr.sprite = unitSprite; // Yuvarlak su damlası
        sr.color = new Color(0.3f, 0.7f, 1f); // Mavi su damlası
        
        water.transform.position = tower.transform.position;
        water.transform.localScale = new Vector3(0.25f, 0.25f, 1f);
        sr.sortingOrder = 25;
        
        StartCoroutine(MoveWater(water, target));
    }
    
    IEnumerator MoveWater(GameObject water, GameObject target)
    {
        float speed = 12f;
        
        while (water != null && target != null)
        {
            Vector3 dir = (target.transform.position - water.transform.position).normalized;
            water.transform.position += dir * speed * Time.deltaTime;
            
            if (Vector3.Distance(water.transform.position, target.transform.position) < 0.25f)
            {
                // Yangına su sıçradı!
                EnemyController ec = target.GetComponent<EnemyController>();
                if (ec != null)
                {
                    ec.health--;
                    
                    // Buhar efekti (beyaz flash)
                    SpriteRenderer sr = target.GetComponent<SpriteRenderer>();
                    if (sr != null)
                    {
                        Color original = sr.color;
                        sr.color = new Color(0.9f, 0.9f, 0.9f); // Buhar
                        yield return new WaitForSeconds(0.05f);
                        if (sr != null) sr.color = original;
                    }
                    
                    if (ec.health <= 0)
                    {
                        enemies.Remove(target);
                        Destroy(target);
                        // Yangın söndürüldüğünde ödül!
                        GameBootstrapper.AddGold(5);
                        GameBootstrapper.AddScore(10);
                        Debug.Log("<color=cyan>💧 YANGIN SÖNDÜRÜLDÜ! +5 Su, +10 Puan</color>");
                    }
                }
                
                Destroy(water);
                yield break;
            }
            
            yield return null;
        }
        
        if (water != null) Destroy(water);
    }
    IEnumerator SpawnEnemyRoutine()
    {
        yield return new WaitForSeconds(1f); // Başlangıç gecikmesi
        
        while (isGameActive && lives > 0)
        {
            SpawnEnemy();
            
            // Zorluk artışı: Skor arttıkça düşmanlar daha sık gelir
            float interval = Mathf.Max(0.8f, 3.0f - (score / 100f));
            yield return new WaitForSeconds(interval);
        }
    }

    IEnumerator WindRoutine()
    {
        while (isGameActive)
        {
            // Rüzgar yönünü ve şiddetini değiştir
            float randX = Random.Range(-1f, 1f);
            float randY = Random.Range(-1f, 1f);
            windDirection = new Vector2(randX, randY).normalized;
            
            // Bazen rüzgar dursun, bazen essin
            if (Random.value > 0.3f)
                windStrength = Random.Range(0.5f, 2.0f); // Rüzgar şiddeti
            else
                windStrength = 0f; // Rüzgar durdu
            
            yield return new WaitForSeconds(Random.Range(10f, 20f));
        }
    }
}

// ==================== YARDIMCI BİLEŞENLER ====================

public class TowerController : MonoBehaviour
{
    public float lastFireTime;
    public int speedLevel = 1;      // Hızlı ateş seviyesi
    public int multiShotLevel = 1;  // Çoklu ateş seviyesi
    public float fireRate = 1f;     // Ateş hızı (güçlendirme ile azalır)
}

public enum EnemyType { Normal, Spark, Oil }

public class EnemyController : MonoBehaviour
{
    public EnemyType type;
    public int pathIndex;
    public float speed;
    public int health;
    public List<Vector3> pathPoints;
}

public class CrystalCollector : MonoBehaviour
{
    private bool collected = false;
    
    public void Collect()
    {
        if (collected) return;
        collected = true;
        
        // Su kaynağı toplama: +3 su ve +25 puan!
        GameBootstrapper.AddGold(3);
        GameBootstrapper.AddScore(25);
        Debug.Log("<color=cyan>� Su Kaynağı Toplandı! +3 Su, +25 Puan</color>");
        StartCoroutine(CollectAnimation());
    }
    
    IEnumerator CollectAnimation()
    {
        SpriteRenderer sr = GetComponent<SpriteRenderer>();
        BoxCollider2D col = GetComponent<BoxCollider2D>();
        if (col != null) col.enabled = false;
        
        float duration = 0.3f;
        float elapsed = 0f;
        Vector3 startScale = transform.localScale;
        Vector3 startPos = transform.position;
        
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;
            
            transform.localScale = startScale * (1f + t * 0.5f);
            transform.position = startPos + Vector3.up * t * 0.5f;
            transform.Rotate(0, 0, 720 * Time.deltaTime);
            
            if (sr != null)
            {
                sr.color = new Color(sr.color.r, sr.color.g, sr.color.b, 1f - t);
            }
            
            yield return null;
        }
        
        Destroy(gameObject);
    }
}


