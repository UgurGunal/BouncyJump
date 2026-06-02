using UnityEngine;
using System.Collections.Generic;
using UnityEngine.SceneManagement;

public class TowerManager : MonoBehaviour
{
    [Header("All Towers")]
    public Tower[] allTowers;
    
    [Header("Towers Bought")]
    public List<int> towersBought = new List<int>(); // Indices of bought towers
    
    [Header("Current Selection")]
    public int currentTowerIndex = 0;

    [Header("Save Data")]
    [Tooltip("Used to detect when the shop config changed. When this value differs from the stored value, tower purchase keys are reset.")]
    public int towerShopSaveVersion = 1;
    
    private static TowerManager instance;

    public static TowerManager Instance
    {
        get
        {
            if (instance != null)
                return instance;

            instance = FindInLoadedScenes();
            return instance;
        }
    }

    /// <summary>Finds TowerManager even if inactive; prefers an active object.</summary>
    public static TowerManager FindInLoadedScenes()
    {
        TowerManager[] managers = Resources.FindObjectsOfTypeAll<TowerManager>();
        TowerManager fallback = null;

        for (int i = 0; i < managers.Length; i++)
        {
            TowerManager manager = managers[i];
            if (manager == null || manager.hideFlags != HideFlags.None)
                continue;

            Scene scene = manager.gameObject.scene;
            if (!scene.IsValid() || !scene.isLoaded)
                continue;

            if (manager.gameObject.activeInHierarchy)
                return manager;

            if (fallback == null)
                fallback = manager;
        }

        return fallback;
    }
    
    /// <summary>Fired when the selected tower changes. Shop buttons use this to refresh state.</summary>
    public static System.Action OnSelectionChanged;
    
    /// <summary>Fired when a tower is purchased. Shop buttons use this to refresh state.</summary>
    public static System.Action OnTowerPurchased;
    
    void Awake()
    {
        // Singleton scoped to HomeScene (not DontDestroyOnLoad).
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }

        instance = this;
    }

    void OnDestroy()
    {
        if (instance == this)
            instance = null;
    }
    
    void Start()
    {
        if (allTowers == null || allTowers.Length == 0)
        {
            return;
        }

        int defaultUnlockedIndex = GetFirstUnlockedByDefaultIndex();

        // If the save version changed (or the player never had this key), wipe purchase keys
        // so only towers marked `isUnlockedByDefault=true` appear bought on startup.
        GameSaveService.EnsureLoaded();

        int savedVersion = GameSaveService.GetTowerShopSaveVersion();
        if (savedVersion != towerShopSaveVersion)
        {
            ResetTowerPurchaseKeys();
            GameSaveService.SetTowerShopSaveVersion(towerShopSaveVersion);
        }

        currentTowerIndex = GameSaveService.GetCurrentTowerIndex();
        if (!IsTowerBought(currentTowerIndex))
            currentTowerIndex = defaultUnlockedIndex;

        if (currentTowerIndex >= allTowers.Length)
            currentTowerIndex = defaultUnlockedIndex;

        GameSaveService.SetCurrentTowerIndex(currentTowerIndex);
        
        // Initialize towers bought list
        RefreshTowersBought();

        // Ensure all shop buttons and home theme refresh after PlayerPrefs initialization/reset.
        InvokeSelectionChanged();
    }
    
    void OnEnable()
    {
        SceneManager.sceneLoaded += HandleSceneLoaded;
    }
    
    void OnDisable()
    {
        SceneManager.sceneLoaded -= HandleSceneLoaded;
    }
    
    public Tower GetCurrentTower()
    {
        if (allTowers != null && currentTowerIndex < allTowers.Length)
        {
            return allTowers[currentTowerIndex];
        }
        return null;
    }
    
    public static int WrapTowerIndex(int index, int towerCount)
    {
        if (towerCount <= 0)
            return 0;

        index %= towerCount;
        if (index < 0)
            index += towerCount;

        return index;
    }

    /// <summary>
    /// Updates the visible home tower (cyclic navigation). Saves selection only if that tower is bought.
    /// </summary>
    public void SelectHomeTowerVisual(int towerIndex)
    {
        if (allTowers == null || allTowers.Length == 0)
            return;

        currentTowerIndex = WrapTowerIndex(towerIndex, allTowers.Length);

        if (IsTowerBought(currentTowerIndex))
            GameSaveService.SetCurrentTowerIndex(currentTowerIndex);

        InvokeSelectionChanged();
    }

    public void SetCurrentTower(int towerIndex)
    {
        if (towerIndex >= 0 && towerIndex < allTowers.Length && IsTowerBought(towerIndex))
            SelectHomeTowerVisual(towerIndex);
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
    }
    
    public string GetCurrentTowerSceneName()
    {
        Tower currentTower = GetCurrentTower();
        return currentTower?.sceneToLoad ?? "GameScene";
    }
    
    public Sprite GetCurrentTowerImage()
    {
        return GetCurrentHomeTowerImage();
    }
    
    public Sprite GetCurrentHomeTowerImage()
    {
        return GetCurrentTower()?.GetTowerForegroundSprite();
    }

    public TowerHomeTheme GetCurrentHomeTheme()
    {
        return GetCurrentTower()?.GetResolvedHomeTheme();
    }

    public bool IsTowerBought(int towerIndex)
    {
        if (allTowers == null || towerIndex < 0 || towerIndex >= allTowers.Length)
            return false;

        Tower tower = allTowers[towerIndex];
        if (tower == null)
            return false;

        if (tower.isUnlockedByDefault)
            return true;

        return GameSaveService.IsTowerPurchased(towerIndex);
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
            Tower tower = allTowers[towerIndex];
            
            // Check if already bought
            if (IsTowerBought(towerIndex))
            {
                return;
            }
            
            // Check if player has enough currency
            int currentGold = GameSaveService.GetGold();
            int currentDiamonds = GameSaveService.GetDiamonds();
            
            if (currentGold >= tower.goldPrice && currentDiamonds >= tower.diamondPrice)
            {
                GameSaveService.SetGold(currentGold - tower.goldPrice);
                GameSaveService.SetDiamonds(currentDiamonds - tower.diamondPrice);
                GameSaveService.SetTowerPurchased(towerIndex, true);
                
                // Refresh bought towers list
                RefreshTowersBought();

                if (SoundEffectsManager.Instance != null)
                    SoundEffectsManager.Instance.PlayShopPurchaseSound();

                InvokeTowerPurchased();
                SelectHomeTowerVisual(towerIndex);
            }
        }
    }

    public static void InvokeSelectionChanged()
    {
        InvokeAction(OnSelectionChanged);
    }

    public static void InvokeTowerPurchased()
    {
        InvokeAction(OnTowerPurchased);
    }

    static void InvokeAction(System.Action action)
    {
        if (action == null)
            return;

        System.Delegate[] handlers = action.GetInvocationList();
        for (int i = 0; i < handlers.Length; i++)
        {
            try
            {
                ((System.Action)handlers[i])();
            }
            catch (System.Exception e)
            {
                Debug.LogException(e);
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
    }

    private int GetFirstUnlockedByDefaultIndex()
    {
        for (int i = 0; i < allTowers.Length; i++)
        {
            if (allTowers[i] != null && allTowers[i].isUnlockedByDefault)
            {
                return i;
            }
        }
        return 0;
    }

    private void ResetTowerPurchaseKeys()
    {
        GameSaveService.ClearTowerPurchases();
    }
    
    public List<int> GetTowersBought()
    {
        RefreshTowersBought();
        return new List<int>(towersBought);
    }
    
    private void HandleSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (scene.name == gameObject.scene.name)
            InvokeSelectionChanged();
    }
}

[System.Serializable]
public class Tower
{
    [Header("Tower Information")]
    public string towerName = "Tower 1";
    [Tooltip("Legacy single tower image. Prefer Home Theme > Tower Foreground.")]
    public Sprite homeTowerImage;

    [Header("Home Screen Theme")]
    public TowerHomeTheme homeTheme = new TowerHomeTheme();
    
    [Header("Pricing")]
    public int goldPrice = 0; // Cost in gold
    public int diamondPrice = 0; // Cost in diamonds
    public bool isUnlockedByDefault = false; // Free starter tower
    
    [Header("Scene Settings")]
    public string sceneToLoad = "GameScene";
}
