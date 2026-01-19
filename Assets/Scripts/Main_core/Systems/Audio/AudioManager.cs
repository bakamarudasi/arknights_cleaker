using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// サウンド管理システム（シングルトン）
/// BGM / SE / Voice を一元管理
/// </summary>
public class AudioManager : BaseSingleton<AudioManager>
{

    // ========================================
    // 設定
    // ========================================
    [Header("AudioSources")]
    [SerializeField] private AudioSource bgmSource;
    [SerializeField] private AudioSource seSource;
    [SerializeField] private AudioSource voiceSource;

    [Header("SE設定")]
    [Tooltip("同時再生可能なSE数")]
    [SerializeField] private int maxConcurrentSE = 8;

    [Header("音量設定")]
    [Range(0f, 1f)] public float masterVolume = 1f;
    [Range(0f, 1f)] public float bgmVolume = 0.7f;
    [Range(0f, 1f)] public float seVolume = 1f;
    [Range(0f, 1f)] public float voiceVolume = 1f;

    // ========================================
    // SEプール
    // ========================================
    private List<AudioSource> sePool = new();
    private int sePoolIndex = 0;

    // ========================================
    // 定数
    // ========================================
    private const string PREF_MASTER = "AudioMaster";
    private const string PREF_BGM = "AudioBGM";
    private const string PREF_SE = "AudioSE";
    private const string PREF_VOICE = "AudioVoice";

    // ========================================
    // 初期化
    // ========================================

    protected override void OnAwake()
    {
        InitializeAudioSources();
        LoadVolumeSettings();
    }

    private void InitializeAudioSources()
    {
        // BGM用
        if (bgmSource == null)
        {
            bgmSource = gameObject.AddComponent<AudioSource>();
            bgmSource.loop = true;
            bgmSource.playOnAwake = false;
        }

        // メインSE用
        if (seSource == null)
        {
            seSource = gameObject.AddComponent<AudioSource>();
            seSource.playOnAwake = false;
        }

        // Voice用
        if (voiceSource == null)
        {
            voiceSource = gameObject.AddComponent<AudioSource>();
            voiceSource.playOnAwake = false;
        }

        // SEプール作成（同時再生用）
        for (int i = 0; i < maxConcurrentSE; i++)
        {
            var source = gameObject.AddComponent<AudioSource>();
            source.playOnAwake = false;
            sePool.Add(source);
        }
    }

    // ========================================
    // BGM
    // ========================================

    /// <summary>
    /// BGMを再生
    /// </summary>
    public void PlayBGM(AudioClip clip, float fadeTime = 0.5f)
    {
        if (clip == null) return;

        if (fadeTime > 0 && bgmSource.isPlaying)
        {
            StartCoroutine(CrossFadeBGM(clip, fadeTime));
        }
        else
        {
            bgmSource.clip = clip;
            bgmSource.volume = bgmVolume * masterVolume;
            bgmSource.Play();
        }
    }

    /// <summary>
    /// BGMを停止
    /// </summary>
    public void StopBGM(float fadeTime = 0.5f)
    {
        if (fadeTime > 0)
        {
            StartCoroutine(FadeOutBGM(fadeTime));
        }
        else
        {
            bgmSource.Stop();
        }
    }

    /// <summary>
    /// BGMを一時停止/再開
    /// </summary>
    public void PauseBGM(bool pause)
    {
        if (pause) bgmSource.Pause();
        else bgmSource.UnPause();
    }

    private System.Collections.IEnumerator CrossFadeBGM(AudioClip newClip, float duration)
    {
        float startVolume = bgmSource.volume;

        // フェードアウト
        for (float t = 0; t < duration / 2; t += Time.deltaTime)
        {
            bgmSource.volume = Mathf.Lerp(startVolume, 0, t / (duration / 2));
            yield return null;
        }

        // 切り替え
        bgmSource.clip = newClip;
        bgmSource.Play();

        // フェードイン
        float targetVolume = bgmVolume * masterVolume;
        for (float t = 0; t < duration / 2; t += Time.deltaTime)
        {
            bgmSource.volume = Mathf.Lerp(0, targetVolume, t / (duration / 2));
            yield return null;
        }
        bgmSource.volume = targetVolume;
    }

    private System.Collections.IEnumerator FadeOutBGM(float duration)
    {
        float startVolume = bgmSource.volume;
        for (float t = 0; t < duration; t += Time.deltaTime)
        {
            bgmSource.volume = Mathf.Lerp(startVolume, 0, t / duration);
            yield return null;
        }
        bgmSource.Stop();
        bgmSource.volume = bgmVolume * masterVolume;
    }

    // ========================================
    // SE
    // ========================================

    /// <summary>
    /// SEを再生
    /// </summary>
    public void PlaySE(AudioClip clip, float volumeScale = 1f)
    {
        if (clip == null) return;

        // プールから空いてるSourceを探す
        var source = GetAvailableSESource();
        source.clip = clip;
        source.volume = seVolume * masterVolume * volumeScale;
        source.Play();
    }

    /// <summary>
    /// SEを再生（PlayOneShot）
    /// </summary>
    public void PlaySEOneShot(AudioClip clip, float volumeScale = 1f)
    {
        if (clip == null) return;
        seSource.PlayOneShot(clip, seVolume * masterVolume * volumeScale);
    }

    private AudioSource GetAvailableSESource()
    {
        // ラウンドロビンで選択
        var source = sePool[sePoolIndex];
        sePoolIndex = (sePoolIndex + 1) % sePool.Count;
        return source;
    }

    // ========================================
    // Voice
    // ========================================

    /// <summary>
    /// ボイスを再生（前のボイスを停止）
    /// </summary>
    public void PlayVoice(AudioClip clip, float volumeScale = 1f)
    {
        if (clip == null) return;

        voiceSource.Stop();
        voiceSource.clip = clip;
        voiceSource.volume = voiceVolume * masterVolume * volumeScale;
        voiceSource.Play();
    }

    /// <summary>
    /// ボイスを停止
    /// </summary>
    public void StopVoice()
    {
        voiceSource.Stop();
    }

    /// <summary>
    /// ボイス再生中か
    /// </summary>
    public bool IsVoicePlaying => voiceSource.isPlaying;

    // ========================================
    // 音量設定
    // ========================================

    /// <summary>
    /// マスター音量を設定
    /// </summary>
    public void SetMasterVolume(float volume)
    {
        masterVolume = Mathf.Clamp01(volume);
        ApplyVolumes();
        SaveVolumeSettings();
    }

    /// <summary>
    /// BGM音量を設定
    /// </summary>
    public void SetBGMVolume(float volume)
    {
        bgmVolume = Mathf.Clamp01(volume);
        ApplyVolumes();
        SaveVolumeSettings();
    }

    /// <summary>
    /// SE音量を設定
    /// </summary>
    public void SetSEVolume(float volume)
    {
        seVolume = Mathf.Clamp01(volume);
        SaveVolumeSettings();
    }

    /// <summary>
    /// ボイス音量を設定
    /// </summary>
    public void SetVoiceVolume(float volume)
    {
        voiceVolume = Mathf.Clamp01(volume);
        ApplyVolumes();
        SaveVolumeSettings();
    }

    private void ApplyVolumes()
    {
        bgmSource.volume = bgmVolume * masterVolume;
        voiceSource.volume = voiceVolume * masterVolume;
    }

    private void SaveVolumeSettings()
    {
        PlayerPrefs.SetFloat(PREF_MASTER, masterVolume);
        PlayerPrefs.SetFloat(PREF_BGM, bgmVolume);
        PlayerPrefs.SetFloat(PREF_SE, seVolume);
        PlayerPrefs.SetFloat(PREF_VOICE, voiceVolume);
        PlayerPrefs.Save();
    }

    private void LoadVolumeSettings()
    {
        masterVolume = PlayerPrefs.GetFloat(PREF_MASTER, 1f);
        bgmVolume = PlayerPrefs.GetFloat(PREF_BGM, 0.7f);
        seVolume = PlayerPrefs.GetFloat(PREF_SE, 1f);
        voiceVolume = PlayerPrefs.GetFloat(PREF_VOICE, 1f);
        ApplyVolumes();
    }

    // ========================================
    // ユーティリティ
    // ========================================

    /// <summary>
    /// 全ての音を停止
    /// </summary>
    public void StopAll()
    {
        bgmSource.Stop();
        seSource.Stop();
        voiceSource.Stop();
        foreach (var source in sePool)
        {
            source.Stop();
        }
    }

    /// <summary>
    /// 全ての音を一時停止
    /// </summary>
    public void PauseAll(bool pause)
    {
        if (pause)
        {
            bgmSource.Pause();
            voiceSource.Pause();
        }
        else
        {
            bgmSource.UnPause();
            voiceSource.UnPause();
        }
    }
}
