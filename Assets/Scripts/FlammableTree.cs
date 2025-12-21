using UnityEngine;
using System.Collections;

public class FlammableTree : MonoBehaviour
{
    public bool isBurning = false;
    public bool isBurnt = false;
    public float maxHealth = 5f;
    public float currentHealth;
    
    private SpriteRenderer sr;
    private Color originalColor;
    private GameBootstrapper gameManager;
    
    void Start()
    {
        currentHealth = maxHealth;
        sr = GetComponent<SpriteRenderer>();
        originalColor = sr.color;
        gameManager = FindFirstObjectByType<GameBootstrapper>();
    }
    
    public void Ignite()
    {
        if (isBurning || isBurnt) return;
        
        isBurning = true;
        Debug.Log("🔥 AĞAÇ YANIYOR! Söndürmezsen yayılacak!");
        StartCoroutine(BurnRoutine());
    }
    
    IEnumerator BurnRoutine()
    {
        while (isBurning && currentHealth > 0)
        {
            currentHealth -= Time.deltaTime;
            
            // Yanma Efekti (Kızar ve Titre)
            float flicker = Mathf.PingPong(Time.time * 5f, 0.3f);
            sr.color = Color.Lerp(originalColor, Color.red, 0.5f + flicker);
            
            // Etrafa yayılma şansı (her saniye %5)
            if (Random.value < 0.05f)
            {
                // Yakındaki ağaçları tutuşturabilir (GameBootstrapper'da SpreadFireToTrees var mı? Yoksa burada yapalım)
                // Basitlik için burada yakındaki diğer ağaçları bulalım
                Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, 1.5f);
                foreach(var hit in hits)
                {
                    FlammableTree otherTree = hit.GetComponent<FlammableTree>();
                    if (otherTree != null && !otherTree.isBurning && !otherTree.isBurnt)
                    {
                        otherTree.Ignite();
                    }
                }
            }
            
            yield return null;
        }
        
        if (currentHealth <= 0)
        {
            BurnDown();
        }
    }
    
    void BurnDown()
    {
        isBurning = false;
        isBurnt = true;
        sr.color = Color.black; // Kömür olmuş
        
        // Ceza: Skor düşür
        GameBootstrapper.AddScore(-50);
        Debug.Log("🌲 Ağaç kül oldu! -50 Puan");
    }
    
    // Su ile söndürme (Kuleler buraya vurmalı mı? Yoksa üzerine tıklayarak mı?)
    // Şimdilik üzerine tıklayarak söndürelim (oyuncu aktif olsun)
    void OnMouseDown()
    {
        if (isBurning)
        {
            Extinguish();
        }
    }
    
    public void Extinguish()
    {
        if (!isBurning) return;
        
        isBurning = false;
        sr.color = originalColor; // Eski haline dön (hafif hasarlı kalabilir ama görseli bozmayalım)
        currentHealth = maxHealth;
        Debug.Log("💧 Ağaç söndürüldü!");
        GameBootstrapper.AddScore(10);
    }
}
