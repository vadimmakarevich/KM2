using UnityEngine;

public class AudioVibrationManager : MonoBehaviour
{
    public static AudioVibrationManager Instance { get; private set; }

    [Header("Audio Clips")]
    public AudioClip buttonClickSound;
    public AudioClip explosionSound;
    public AudioClip menuMusic;
    public AudioClip survivalModeMusic;
    public AudioClip timerModeMusic;
    public AudioClip timerTickSound;
    public AudioClip mergeSound;

    [Header("Prefab Sounds")]
    public AudioClip[] prefabSpawnSounds;
    public AudioClip[] prefabCollisionSounds;
    public AudioClip[] prefabMergeSounds;

    [Header("Volume Settings")]
    [Range(0f, 1f)]
    public float sfxVolume = 1f;
    [Range(0f, 1f)]
    public float musicVolume = 0.5f;

    private AudioSource sfxSource;
    private AudioSource musicSource;

    private bool isMusicEnabled = true;
    private bool isSFXEnabled = true;
    private bool isVibrationEnabled = false;
    private bool isInitialized = false;

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            DestroyImmediate(gameObject);
            return;
        }

        if (isInitialized)
        {
            #if UNITY_EDITOR
            Debug.Log($"[{gameObject.name}] AudioVibrationManager уже инициализирован, пропускаем повторную инициализацию.");
            #endif
            return;
        }
        isInitialized = true;

        AudioSource[] existingSources = gameObject.GetComponents<AudioSource>();
        foreach (var source in existingSources)
        {
            DestroyImmediate(source);
        }

        sfxSource = gameObject.AddComponent<AudioSource>();
        musicSource = gameObject.AddComponent<AudioSource>();

        sfxSource.playOnAwake = false;
        musicSource.playOnAwake = false;
        musicSource.loop = true;
        musicSource.ignoreListenerPause = true;

        isMusicEnabled = ProgressManager.LoadInt("MusicEnabled", 1) == 1;
        isSFXEnabled = ProgressManager.LoadInt("SFXEnabled", 1) == 1;
        isVibrationEnabled = ProgressManager.LoadInt("VibrationEnabled", 0) == 1;

        musicVolume = ProgressManager.LoadFloat("MusicVolume", 0.5f);
        sfxVolume = ProgressManager.LoadFloat("SFXVolume", 1f);

        musicSource.volume = isMusicEnabled ? musicVolume : 0f;
        sfxSource.volume = isSFXEnabled ? sfxVolume : 0f;

        if (sfxSource == null)
        {
            #if UNITY_EDITOR
            Debug.LogError("SFXSource не создан!");
            #endif
        }
        if (musicSource == null)
        {
            #if UNITY_EDITOR
            Debug.LogError("MusicSource не создан!");
            #endif
        }
        if (buttonClickSound == null)
        {
            #if UNITY_EDITOR
            Debug.LogError("ButtonClickSound не назначен!");
            #endif
        }
        if (explosionSound == null)
        {
            #if UNITY_EDITOR
            Debug.LogError("ExplosionSound не назначен!");
            #endif
        }
        if (menuMusic == null)
        {
            #if UNITY_EDITOR
            Debug.LogError("MenuMusic не назначен!");
            #endif
        }
        if (survivalModeMusic == null)
        {
            #if UNITY_EDITOR
            Debug.LogError("SurvivalModeMusic не назначен!");
            #endif
        }
        if (timerModeMusic == null)
        {
            #if UNITY_EDITOR
            Debug.LogError("TimerModeMusic не назначен!");
            #endif
        }
        if (timerTickSound == null)
        {
            #if UNITY_EDITOR
            Debug.LogError("TimerTickSound не назначен!");
            #endif
        }
        if (mergeSound == null)
        {
            #if UNITY_EDITOR
            Debug.LogError("MergeSound не назначен!");
            #endif
        }

        ValidateAudioArray(prefabSpawnSounds, "prefabSpawnSounds", 13);
        ValidateAudioArray(prefabCollisionSounds, "prefabCollisionSounds", 13);
        ValidateAudioArray(prefabMergeSounds, "prefabMergeSounds", 13);

        #if UNITY_EDITOR
        Debug.Log($"[{gameObject.name}] AudioVibrationManager инициализирован. MusicEnabled: {isMusicEnabled}, SFXEnabled: {isSFXEnabled}, VibrationEnabled: {isVibrationEnabled}");
        Debug.Log($"Начальная громкость: Music: {musicSource.volume}, SFX: {sfxSource.volume}");
        #endif
    }

    private void ValidateAudioArray(AudioClip[] array, string arrayName, int expectedLength)
    {
        if (array == null || array.Length != expectedLength)
        {
            #if UNITY_EDITOR
            Debug.LogError($"{arrayName} должен содержать {expectedLength} элементов, сейчас: {(array == null ? "null" : array.Length.ToString())}");
            #endif
        }
        else
        {
            for (int i = 0; i < array.Length; i++)
            {
                if (array[i] == null)
                {
                    #if UNITY_EDITOR
                    Debug.LogWarning($"{arrayName}[{i}] не заполнен!");
                    #endif
                }
            }
        }
    }

    void OnApplicationQuit()
    {
        StopBackgroundMusic();
        #if UNITY_EDITOR
        Debug.Log("Приложение закрывается, музыка остановлена.");
        #endif
    }

    void OnApplicationPause(bool pauseStatus)
    {
        if (pauseStatus)
        {
            #if UNITY_EDITOR
            Debug.Log("Приложение приостановлено, останавливаем музыку.");
            #endif
            StopBackgroundMusic();
        }
    }

    public void PlaySFX(AudioClip clip)
    {
        if (!isSFXEnabled || clip == null || sfxSource == null)
        {
            #if UNITY_EDITOR
            Debug.LogWarning($"SFX не проигран: {(isSFXEnabled ? "" : "SFX отключены, ")}clip: {(clip == null ? "null" : clip.name)}, sfxSource: {(sfxSource == null ? "null" : "exists")}");
            #endif
            return;
        }
        sfxSource.PlayOneShot(clip, sfxVolume);
        #if UNITY_EDITOR
        Debug.Log($"SFX: {clip.name}, громкость: {sfxSource.volume}");
        #endif
    }

    public void PlayBackgroundMusic(AudioClip clip)
    {
        if (!isMusicEnabled || clip == null || musicSource == null)
        {
            #if UNITY_EDITOR
            Debug.LogWarning($"Музыка не проиграна: {(isMusicEnabled ? "" : "Музыка отключена, ")}clip: {(clip == null ? "null" : clip.name)}, musicSource: {(musicSource == null ? "null" : "exists")}");
            #endif
            return;
        }
        if (musicSource.isPlaying)
        {
            musicSource.Stop();
        }
        musicSource.clip = clip;
        musicSource.Play();
        #if UNITY_EDITOR
        Debug.Log($"Фоновая музыка: {clip.name}, громкость: {musicSource.volume}");
        #endif
    }

    public void StopBackgroundMusic()
    {
        if (musicSource != null && musicSource.isPlaying)
        {
            musicSource.Stop();
            #if UNITY_EDITOR
            Debug.Log("Фоновая музыка остановлена.");
            #endif
        }
    }

    public void Vibrate()
    {
        if (!isVibrationEnabled)
        {
            #if UNITY_EDITOR
            Debug.Log("Вибрация отключена.");
            #endif
            return;
        }
        #if UNITY_ANDROID || UNITY_IOS
        Handheld.Vibrate();
        #if UNITY_EDITOR
        Debug.Log("Вибрация активирована.");
        #endif
        #else
        #if UNITY_EDITOR
        Debug.Log("Вибрация не поддерживается на этой платформе.");
        #endif
        #endif
    }

    public AudioClip GetPrefabSpawnSound(int index)
    {
        return GetSoundFromArray(prefabSpawnSounds, index, buttonClickSound, "spawn");
    }

    public AudioClip GetPrefabCollisionSound(int index)
    {
        return GetSoundFromArray(prefabCollisionSounds, index, buttonClickSound, "collision");
    }

    public AudioClip GetPrefabMergeSound(int index)
    {
        return GetSoundFromArray(prefabMergeSounds, index, mergeSound, "merge");
    }

    private AudioClip GetSoundFromArray(AudioClip[] array, int index, AudioClip defaultSound, string type)
    {
        if (array == null || index < 0 || index >= array.Length || array[index] == null)
        {
            #if UNITY_EDITOR
            Debug.LogWarning($"Звук {type} не найден для индекса: {index}. Используется запасной звук.");
            #endif
            return defaultSound;
        }
        return array[index];
    }

    public void SetMusicEnabled(bool enabled)
    {
        isMusicEnabled = enabled;
        ProgressManager.SaveBool("MusicEnabled", enabled);
        musicSource.volume = enabled ? musicVolume : 0f;
        if (!enabled && musicSource.isPlaying)
        {
            musicSource.Stop();
        }
        else if (enabled && !musicSource.isPlaying && musicSource.clip != null)
        {
            musicSource.Play();
        }
        #if UNITY_EDITOR
        Debug.Log($"SetMusicEnabled: {enabled}, Music volume: {musicSource.volume}");
        #endif
    }

    public void SetSoundEnabled(bool enabled)
    {
        isSFXEnabled = enabled;
        ProgressManager.SaveBool("SFXEnabled", enabled);
        sfxSource.volume = enabled ? sfxVolume : 0f;
        #if UNITY_EDITOR
        Debug.Log($"SetSoundEnabled: {enabled}, SFX volume: {sfxSource.volume}");
        #endif
    }

    public void SetVibrationEnabled(bool enabled)
    {
        isVibrationEnabled = enabled;
        ProgressManager.SaveBool("VibrationEnabled", enabled);
        #if UNITY_EDITOR
        Debug.Log($"SetVibrationEnabled: {enabled}");
        #endif
    }

    public bool IsMusicEnabled() => isMusicEnabled;
    public bool IsSFXEnabled() => isSFXEnabled;
    public bool IsVibrationEnabled() => isVibrationEnabled;

    public void SetMusicVolume(float volume)
    {
        musicVolume = Mathf.Clamp01(volume);
        musicSource.volume = isMusicEnabled ? musicVolume : 0f;
        ProgressManager.SaveFloat("MusicVolume", musicVolume);
        #if UNITY_EDITOR
        Debug.Log($"SetMusicVolume: {musicVolume}, Music volume: {musicSource.volume}");
        #endif
    }

    public void SetSFXVolume(float volume)
    {
        sfxVolume = Mathf.Clamp01(volume);
        sfxSource.volume = isSFXEnabled ? sfxVolume : 0f;
        ProgressManager.SaveFloat("SFXVolume", sfxVolume);
        #if UNITY_EDITOR
        Debug.Log($"SetSFXVolume: {sfxVolume}, SFX volume: {sfxSource.volume}");
        #endif
    }

    public float GetMusicVolume() => musicVolume;
    public float GetSFXVolume() => sfxVolume;
}