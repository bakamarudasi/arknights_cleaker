using UnityEngine;
using System;

/// <summary>
/// キャラクターの興奮度を管理するマネージャー
/// 興奮度はタッチで上昇し、時間経過で減少する
/// </summary>
public class ExcitementManager : MonoBehaviour
{
    public static ExcitementManager Instance { get; private set; }

    [Header("興奮度設定")]
    [SerializeField] private float maxExcitement = 100f;
    [SerializeField] private float decayRate = 0.5f; // 1秒あたりの減少量
    [SerializeField] private float decayInterval = 0.2f; // 減少処理の間隔

    [Header("ゾーン別興奮度加算")]
    [SerializeField] private float headExcitement = 5f;
    [SerializeField] private float bodyExcitement = 12f;
    [SerializeField] private float handExcitement = 2f;
    [SerializeField] private float specialExcitement = 20f;

    [Header("閾値")]
    [SerializeField] private float lowThreshold = 50f;   // 低興奮の閾値
    [SerializeField] private float highThreshold = 80f;  // 高興奮の閾値

    // 現在の興奮度
    private float _currentExcitement;
    private float _lastDecayTime;

    // ========================================
    // イベント
    // ========================================

    /// <summary>興奮度変化時 (newValue, delta)</summary>
    public event Action<float, float> OnExcitementChanged;

    /// <summary>興奮レベル変化時 (level: 0=通常, 1=低興奮, 2=高興奮)</summary>
    public event Action<int> OnExcitementLevelChanged;

    // ========================================
    // プロパティ
    // ========================================

    public float CurrentExcitement => _currentExcitement;
    public float MaxExcitement => maxExcitement;
    public float ExcitementPercent => _currentExcitement / maxExcitement * 100f;

    public int ExcitementLevel
    {
        get
        {
            if (_currentExcitement >= highThreshold) return 2;
            if (_currentExcitement >= lowThreshold) return 1;
            return 0;
        }
    }

    public bool IsFeverMode => _currentExcitement >= highThreshold;

    // ========================================
    // 初期化
    // ========================================

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else { Destroy(gameObject); return; }
    }

    private void Update()
    {
        ProcessDecay();
    }

    // ========================================
    // 興奮度処理
    // ========================================

    /// <summary>
    /// ゾーンタッチ時の興奮度加算
    /// </summary>
    public void OnZoneTouched(CharacterInteractionZone.ZoneType zoneType, int comboCount = 1)
    {
        float baseAmount = zoneType switch
        {
            CharacterInteractionZone.ZoneType.Head => headExcitement,
            CharacterInteractionZone.ZoneType.Body => bodyExcitement,
            CharacterInteractionZone.ZoneType.Hand => handExcitement,
            CharacterInteractionZone.ZoneType.Special => specialExcitement,
            _ => 5f
        };

        // コンボボーナス（コンボ数に応じて1.0〜1.5倍）
        float comboMultiplier = 1f + Mathf.Min(comboCount - 1, 5) * 0.1f;
        float amount = baseAmount * comboMultiplier;

        AddExcitement(amount);
    }

    /// <summary>
    /// 興奮度を加算
    /// </summary>
    public void AddExcitement(float amount)
    {
        if (amount <= 0) return;

        int oldLevel = ExcitementLevel;
        float oldValue = _currentExcitement;

        _currentExcitement = Mathf.Clamp(_currentExcitement + amount, 0f, maxExcitement);

        int newLevel = ExcitementLevel;

        // イベント発火
        OnExcitementChanged?.Invoke(_currentExcitement, _currentExcitement - oldValue);

        if (newLevel != oldLevel)
        {
            OnExcitementLevelChanged?.Invoke(newLevel);

            if (newLevel == 2)
            {
                LogUIController.Msg("<color=#FF4757>FEVER MODE!</color>");
            }
        }
    }

    /// <summary>
    /// 興奮度を減少させる（時間経過）
    /// </summary>
    private void ProcessDecay()
    {
        if (_currentExcitement <= 0) return;

        if (Time.time - _lastDecayTime >= decayInterval)
        {
            int oldLevel = ExcitementLevel;

            float decayAmount = decayRate * decayInterval;
            _currentExcitement = Mathf.Max(0f, _currentExcitement - decayAmount);
            _lastDecayTime = Time.time;

            int newLevel = ExcitementLevel;

            // 変化があった場合のみイベント発火
            if (decayAmount > 0)
            {
                OnExcitementChanged?.Invoke(_currentExcitement, -decayAmount);
            }

            if (newLevel != oldLevel)
            {
                OnExcitementLevelChanged?.Invoke(newLevel);
            }
        }
    }

    /// <summary>
    /// 興奮度をリセット
    /// </summary>
    public void ResetExcitement()
    {
        int oldLevel = ExcitementLevel;

        _currentExcitement = 0f;

        OnExcitementChanged?.Invoke(0f, -_currentExcitement);

        if (oldLevel != 0)
        {
            OnExcitementLevelChanged?.Invoke(0);
        }
    }

    // ========================================
    // セーブ/ロード（必要に応じて）
    // ========================================

    public float GetSaveData() => _currentExcitement;

    public void LoadSaveData(float data)
    {
        _currentExcitement = Mathf.Clamp(data, 0f, maxExcitement);
    }
}
