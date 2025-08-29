using UnityEngine;
using System.Collections.Generic;

public class SimpleTowerManager : MonoBehaviour
{
    [Header("All Towers")]
    public SimpleTower[] allTowers;
    
    [Header("Towers Bought")]
    public List<int> towersBought = new List<int>(); // Indices of bought towers
    
    [Header("Current Selection")]
    public int currentTowerIndex = 0;
    
    private static SimpleTowerManager instance;
    public static SimpleTowerManager Instance
    {
        get
        {
            if (instance == null)
            {
                instance = FindObjectOfType<SimpleTowerManager>();
            }
            return instance;
        }
    }
    
    void Awake()
    {
        if (instance == null)
        {
            instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }
    
    void Start()
    {
        // Load saved tower selection
        currentTowerIndex = PlayerPrefs.GetInt("CurrentTowerIndex", 0);
        
        // Ensure index is valid
        if (currentTowerIndex >= allTowers.Length)
        {
            currentTowerIndex = 0;
        }
        
        // Initialize towers bought list
        RefreshTowersBought();
    }
    
    public SimpleTower GetCurrentTower()
    {
        if (allTowers != null && currentTowerIndex < allTowers.Length)
        {
            return allTowers[currentTowerIndex];
        }
        return null;
    }
    
    public void SetCurrentTower(int towerIndex)
    {
        if (towerIndex >= 0 && towerIndex < allTowers.Length && IsTowerBought(towerIndex))
        {
            currentTowerIndex = towerIndex;
            PlayerPrefs.SetInt("CurrentTowerIndex", currentTowerIndex);
            PlayerPrefs.Save();
            
            Debug.Log($"Selected tower: {allTowers[currentTowerIndex].towerName}");
        }
        else if (!IsTowerBought(towerIndex))
        {
            Debug.LogWarning($"Cannot select tower {towerIndex} - not bought yet!");
        }
    }
    
    public void SetCurrentTower(string towerName)
    {
        for (int i = 0; i < allTowers.Length; i++)
        {
            if (allTowers[i].towerName == towerName)
            {
                SetCurrentTower(i);
                return;
            }
        }
        Debug.LogWarning($"Tower with name '{towerName}' not found!");
    }
    
    public string GetCurrentTowerSceneName()
    {
        SimpleTower currentTower = GetCurrentTower();
        return currentTower?.sceneToLoad ?? "GameScene";
    }
    
    public Sprite GetCurrentTowerImage()
    {
        SimpleTower currentTower = GetCurrentTower();
        return currentTower?.towerImage;
    }
    
    public bool IsTowerBought(int towerIndex)
    {
        if (towerIndex >= 0 && towerIndex < allTowers.Length)
        {
            return PlayerPrefs.GetInt($"TowerPurchased_{towerIndex}", allTowers[towerIndex].isUnlockedByDefault ? 1 : 0) == 1;
        }
        return false;
    }
    
    // Legacy method name for compatibility
    public bool IsTowerPurchased(int towerIndex)
    {
        return IsTowerBought(towerIndex);
    }
    
    public bool IsTowerBought(string towerName)
    {
        for (int i = 0; i < allTowers.Length; i++)
        {
            if (allTowers[i].towerName == towerName)
            {
                return IsTowerBought(i);
            }
        }
        return false;
    }
    
    // Legacy method name for compatibility
    public bool IsTowerPurchased(string towerName)
    {
        return IsTowerBought(towerName);
    }
    
    public void BuyTower(int towerIndex)
    {
        if (towerIndex >= 0 && towerIndex < allTowers.Length)
        {
            SimpleTower tower = allTowers[towerIndex];
            
            // Check if already bought
            if (IsTowerBought(towerIndex))
            {
                Debug.Log($"Tower {tower.towerName} is already bought!");
                return;
            }
            
            // Check if player has enough currency
            int currentGold = PlayerPrefs.GetInt("PlayerGold", 0);
            int currentDiamonds = PlayerPrefs.GetInt("PlayerDiamonds", 0);
            
            if (currentGold >= tower.goldPrice && currentDiamonds >= tower.diamondPrice)
            {
                // Deduct costs
                currentGold -= tower.goldPrice;
                currentDiamonds -= tower.diamondPrice;
                
                // Save new currency amounts
                PlayerPrefs.SetInt("PlayerGold", currentGold);
                PlayerPrefs.SetInt("PlayerDiamonds", currentDiamonds);
                
                // Mark tower as purchased
                PlayerPrefs.SetInt($"TowerPurchased_{towerIndex}", 1);
                PlayerPrefs.Save();
                
                // Refresh bought towers list
                RefreshTowersBought();
                
                Debug.Log($"Bought tower: {tower.towerName} for {tower.goldPrice} gold and {tower.diamondPrice} diamonds");
            }
            else
            {
                Debug.Log($"Not enough currency! Need {tower.goldPrice} gold and {tower.diamondPrice} diamonds");
            }
        }
    }
    
    // Legacy method name for compatibility
    public void PurchaseTower(int towerIndex)
    {
        BuyTower(towerIndex);
    }
    
    public void BuyTower(string towerName)
    {
        for (int i = 0; i < allTowers.Length; i++)
        {
            if (allTowers[i].towerName == towerName)
            {
                BuyTower(i);
                return;
            }
        }
    }
    
    // Legacy method name for compatibility
    public void PurchaseTower(string towerName)
    {
        BuyTower(towerName);
    }
    
    public void RefreshTowersBought()
    {
        towersBought.Clear();
        for (int i = 0; i < allTowers.Length; i++)
        {
            if (IsTowerBought(i))
            {
                towersBought.Add(i);
            }
        }
        Debug.Log($"Towers bought: {towersBought.Count} out of {allTowers.Length}");
    }
    
    public List<int> GetTowersBought()
    {
        RefreshTowersBought();
        return new List<int>(towersBought);
    }
}

[System.Serializable]
public class SimpleTower
{
    [Header("Tower Information")]
    public string towerName = "Tower 1";
    public Sprite towerImage; // For home screen display when this tower is active
    
    [Header("Pricing")]
    public int goldPrice = 0; // Cost in gold
    public int diamondPrice = 0; // Cost in diamonds
    public bool isUnlockedByDefault = false; // Free starter tower
    
    [Header("Scene Settings")]
    public string sceneToLoad = "GameScene";
}

