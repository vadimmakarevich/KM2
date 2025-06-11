using UnityEngine;
using UnityEngine.Advertisements;
using System.Collections; // Добавлено для IEnumerator и WaitForSeconds

public class AdManager : MonoBehaviour, IUnityAdsInitializationListener, IUnityAdsLoadListener, IUnityAdsShowListener
{
    [SerializeField] private string androidGameId = "5831765"; // Замени на Game ID для Android
    [SerializeField] private string interstitialAdUnitId = "Interstitial_Android"; // Замени на Placement ID
    [SerializeField] private bool testMode = false; // Установи false перед релизом

    private static AdManager instance;
    // Делегат для уведомления о завершении показа рекламы
    public delegate void OnAdCompletedHandler();
    private OnAdCompletedHandler onAdCompleted;

    void Awake()
    {
        // Singleton pattern
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
        InitializeAds();
    }

    public void InitializeAds()
    {
        if (!Advertisement.isInitialized && Advertisement.isSupported)
        {
            // Отключаем персонализированную рекламу
            MetaData gdprMetaData = new MetaData("gdpr");
            gdprMetaData.Set("consent", "false"); // Отключаем персонализированную рекламу
            Advertisement.SetMetaData(gdprMetaData);

            // Дополнительно отключаем персонализированную рекламу для CCPA (если применимо)
            MetaData ccpaMetaData = new MetaData("privacy");
            ccpaMetaData.Set("user_non_behavioral", "true"); // Указываем, что пользователь не хочет персонализированную рекламу
            Advertisement.SetMetaData(ccpaMetaData);

            Debug.Log("Инициализация Unity Ads с неперсонализированной рекламой...");
            Advertisement.Initialize(androidGameId, testMode, this);
        }
    }

    // Callback при завершении инициализации
    public void OnInitializationComplete()
    {
        Debug.Log("Unity Ads инициализирован успешно!");
        LoadInterstitialAd();
    }

    public void OnInitializationFailed(UnityAdsInitializationError error, string message)
    {
        Debug.LogError($"Ошибка инициализации Unity Ads: {error} - {message}");
    }

    // Загрузка полноэкранной рекламы
    public void LoadInterstitialAd()
    {
        Debug.Log("Загрузка полноэкранной рекламы...");
        Advertisement.Load(interstitialAdUnitId, this);
    }

    public void OnUnityAdsAdLoaded(string placementId)
    {
        Debug.Log($"Реклама загружена: {placementId}");
    }

    public void OnUnityAdsFailedToLoad(string placementId, UnityAdsLoadError error, string message)
    {
        Debug.LogError($"Ошибка загрузки рекламы {placementId}: {error} - {message}");
        StartCoroutine(RetryLoadAdAfterDelay(5f)); // Пробуем снова через 5 секунд
    }

    private IEnumerator RetryLoadAdAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        LoadInterstitialAd();
    }

    // Показ рекламы
    public void ShowInterstitialAd()
    {
        Debug.Log("Попытка показа полноэкранной рекламы...");
        Advertisement.Show(interstitialAdUnitId, this);
    }

    public void OnUnityAdsShowFailure(string placementId, UnityAdsShowError error, string message)
    {
        Debug.LogError($"Ошибка показа рекламы {placementId}: {error} - {message}");
        LoadInterstitialAd(); // Пробуем загрузить снова
        Time.timeScale = 1f; // Возобновляем игру, если реклама не была показана
        // Если реклама не показалась, вызываем делегат, чтобы не блокировать анимацию
        onAdCompleted?.Invoke();
    }

    public void OnUnityAdsShowStart(string placementId)
    {
        Debug.Log($"Реклама началась: {placementId}");
        Time.timeScale = 0f; // Приостанавливаем игру во время рекламы
    }

    public void OnUnityAdsShowClick(string placementId)
    {
        Debug.Log($"Нажатие на рекламу: {placementId}");
    }

    public void OnUnityAdsShowComplete(string placementId, UnityAdsShowCompletionState showResult)
    {
        Debug.Log($"Реклама завершена: {placementId}, Результат: {showResult}");
        Time.timeScale = 1f; // Возобновляем игру
        LoadInterstitialAd(); // Загружаем следующую рекламу
        // Уведомляем подписчиков о завершении рекламы
        onAdCompleted?.Invoke();
    }

    // Метод для установки делегата
    public void SetOnAdCompletedHandler(OnAdCompletedHandler handler)
    {
        onAdCompleted = handler;
    }

    // Метод для вызова из других скриптов
    public static AdManager Instance
    {
        get { return instance; }
    }
}