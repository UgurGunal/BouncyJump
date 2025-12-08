using UnityEngine;
using System.Collections.Generic;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class SimpleTowerManager : MonoBehaviour
{
    [Header("All Towers")]
    public SimpleTower[] allTowers;
    
    [Header("Towers Bought")]
    public List<int> towersBought = new List<int>(); // Indices of bought towers
    
    [Header("Current Selection")]
    public int currentTowerIndex = 0;
    
    [Header("Home Screen UI")]
    public Image homeScreenTowerImage;
    
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
        // Singleton pattern with DontDestroyOnLoad for persistence across scenes
        if (instance == null)
        {
            instance = this;
            DontDestroyOnLoad(gameObject);
            Debug.Log("SimpleTowerManager: Instance created and marked as persistent");
        }
        else
        {
            Debug.Log("SimpleTowerManager: Duplicate instance destroyed");
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
        
        // Ensure home screen visuals match current selection
        UpdateHomeScreenTowerImage();
    }
    
    void OnEnable()
    {
        SceneManager.sceneLoaded += HandleSceneLoaded;
    }
    
    void OnDisable()
    {
        SceneManager.sceneLoaded -= HandleSceneLoaded;
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
            
            UpdateHomeScreenTowerImage();
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
        return currentTower?.shopTowerImage;
    }
    
    public Sprite GetCurrentHomeTowerImage()
    {
        SimpleTower currentTower = GetCurrentTower();
        if (currentTower == null)
        {
            return null;
        }
        
        // Fall back to shop image if no dedicated home image is assigned
        return currentTower.homeTowerImage != null ? currentTower.homeTowerImage : currentTower.shopTowerImage;
    }
    
    public void UpdateHomeScreenTowerImage()
    {
        if (homeScreenTowerImage == null)
        {
            // Attempt to locate the home screen tower image in the active scene
            GameObject towerImageObject = GameObject.Find("TowerImage");
            if (towerImageObject != null)
            {
                homeScreenTowerImage = towerImageObject.GetComponent<Image>();
            }
            
            if (homeScreenTowerImage == null)
            {
                return;
            }
        }
        
        Sprite imageToUse = GetCurrentHomeTowerImage();
        homeScreenTowerImage.sprite = imageToUse;
        homeScreenTowerImage.enabled = imageToUse != null;
        
        // Preserve aspect so sprites do not stretch unexpectedly
        if (!homeScreenTowerImage.preserveAspect)
        {
            homeScreenTowerImage.preserveAspect = true;
        }
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
    
    private void HandleSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        // Force the manager to re-link to the home screen image in the newly loaded scene
        homeScreenTowerImage = null;
        UpdateHomeScreenTowerImage();
    }
}

[System.Serializable]
public class SimpleTower
{
    [Header("Tower Information")]
    public string towerName = "Tower 1";
    [Tooltip("Image shown in shop listings for this tower.")]
    public Sprite shopTowerImage; // Shop image
    [Tooltip("Image used on the home screen when this tower is active. If left empty, the shop image will be used instead.")]
    public Sprite homeTowerImage;
    
    [Header("Pricing")]
    public int goldPrice = 0; // Cost in gold
    public int diamondPrice = 0; // Cost in diamonds
    public bool isUnlockedByDefault = false; // Free starter tower
    
    [Header("Scene Settings")]
    public string sceneToLoad = "GameScene";
}

