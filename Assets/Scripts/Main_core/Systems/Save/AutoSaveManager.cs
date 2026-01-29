using UnityEngine;
using System.Collections;

/// <summary>
/// オートセーブマネージャー
/// 定期的にゲームデータを保存し、インジケーターを表示
/// </summary>
public class AutoSaveManager : BaseSingleton<AutoSaveManager>
{
    private const string LOG_TAG = "[AutoSaveManager]";

    // ========================================
    // PlayerPrefs Keys
    // ========================================
    private const string KEY_MONEY = "Save_Money";
    private const string KEY_CERTIFICATES = "Save_Certificates";
    private const string KEY_SAVE_EXISTS = "SaveExists";
    private const string KEY_LAST_SAVE_TIME = "Save_LastTime";

    // ========================================
    // 設定
    // ========================================

    [Header("Auto Save Settings")]
    [SerializeField] private float autoSaveInterval = 30f; // 30秒ごと
    [SerializeField] private bool enableAutoSave = true;

    // ========================================
    // 内部状態
    // ========================================

    private Coroutine _autoSaveCoroutine;
    private WaitForSeconds _saveWait;

    // ========================================
    // 初期化
    // ========================================

    protected override void OnAwake()
    {
        _saveWait = new WaitForSeconds(autoSaveInterval);
    }

    private void Start()
    {
        // ゲームデータをロード
        LoadGame();

        // オートセーブ開始
        if (enableAutoSave)
        {
            StartAutoSave();
        }
    }

    // ========================================
    // オートセーブ制御
    // ========================================

    public void StartAutoSave()
    {
        if (_autoSaveCoroutine != null) return;

        _autoSaveCoroutine = StartCoroutine(AutoSaveCoroutine());
        Debug.Log($"{LOG_TAG} Auto-save started (interval: {autoSaveInterval}s)");
    }

    public void StopAutoSave()
    {
        if (_autoSaveCoroutine != null)
        {
            StopCoroutine(_autoSaveCoroutine);
            _autoSaveCoroutine = null;
            Debug.Log($"{LOG_TAG} Auto-save stopped");
        }
    }

    private IEnumerator AutoSaveCoroutine()
    {
        while (true)
        {
            yield return _saveWait;
            SaveGame();
        }
    }

    // ========================================
    // セーブ
    // ========================================

    /// <summary>
    /// ゲームデータを保存
    /// </summary>
    public void SaveGame()
    {
        // インジケーター表示
        SaveIndicatorController.Instance?.ShowSaving();

        try
        {
            // Wallet データ
            if (WalletManager.Instance != null)
            {
                PlayerPrefs.SetString(KEY_MONEY, WalletManager.Instance.Money.ToString("F0"));
                PlayerPrefs.SetString(KEY_CERTIFICATES, WalletManager.Instance.Certificates.ToString("F0"));
            }

            // セーブ存在フラグとタイムスタンプ
            PlayerPrefs.SetInt(KEY_SAVE_EXISTS, 1);
            PlayerPrefs.SetString(KEY_LAST_SAVE_TIME, System.DateTime.Now.ToString("o"));

            // 保存実行
            PlayerPrefs.Save();

            Debug.Log($"{LOG_TAG} Game saved successfully");

            // 完了表示
            SaveIndicatorController.Instance?.ShowSaved();
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"{LOG_TAG} Failed to save game: {ex.Message}");
            SaveIndicatorController.Instance?.Hide();
        }
    }

    /// <summary>
    /// 手動セーブ（即座にセーブしてインジケーター表示）
    /// </summary>
    public void SaveGameManual()
    {
        SaveGame();
        LogUIController.Msg("Game saved.");
    }

    // ========================================
    // ロード
    // ========================================

    /// <summary>
    /// ゲームデータをロード
    /// </summary>
    public void LoadGame()
    {
        if (!HasSaveData())
        {
            Debug.Log($"{LOG_TAG} No save data found, starting fresh");
            return;
        }

        try
        {
            // Wallet データ
            if (WalletManager.Instance != null)
            {
                double money = 0;
                double certificates = 0;

                if (double.TryParse(PlayerPrefs.GetString(KEY_MONEY, "0"), out double m))
                {
                    money = m;
                }
                if (double.TryParse(PlayerPrefs.GetString(KEY_CERTIFICATES, "0"), out double c))
                {
                    certificates = c;
                }

                WalletManager.Instance.SetBalances(money, certificates);
            }

            string lastSaveTime = PlayerPrefs.GetString(KEY_LAST_SAVE_TIME, "");
            Debug.Log($"{LOG_TAG} Game loaded successfully (Last save: {lastSaveTime})");
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"{LOG_TAG} Failed to load game: {ex.Message}");
        }
    }

    /// <summary>
    /// セーブデータが存在するかチェック
    /// </summary>
    public bool HasSaveData()
    {
        return PlayerPrefs.GetInt(KEY_SAVE_EXISTS, 0) == 1;
    }

    /// <summary>
    /// セーブデータを削除
    /// </summary>
    public void DeleteSaveData()
    {
        PlayerPrefs.DeleteKey(KEY_MONEY);
        PlayerPrefs.DeleteKey(KEY_CERTIFICATES);
        PlayerPrefs.DeleteKey(KEY_SAVE_EXISTS);
        PlayerPrefs.DeleteKey(KEY_LAST_SAVE_TIME);
        PlayerPrefs.Save();

        Debug.Log($"{LOG_TAG} Save data deleted");
    }

    // ========================================
    // アプリ終了時
    // ========================================

    private void OnApplicationPause(bool pauseStatus)
    {
        if (pauseStatus && enableAutoSave)
        {
            // アプリがバックグラウンドに移行する前にセーブ
            SaveGame();
        }
    }

    private void OnApplicationQuit()
    {
        if (enableAutoSave)
        {
            // アプリ終了前にセーブ（インジケーターは表示不要）
            try
            {
                if (WalletManager.Instance != null)
                {
                    PlayerPrefs.SetString(KEY_MONEY, WalletManager.Instance.Money.ToString("F0"));
                    PlayerPrefs.SetString(KEY_CERTIFICATES, WalletManager.Instance.Certificates.ToString("F0"));
                }
                PlayerPrefs.SetInt(KEY_SAVE_EXISTS, 1);
                PlayerPrefs.SetString(KEY_LAST_SAVE_TIME, System.DateTime.Now.ToString("o"));
                PlayerPrefs.Save();
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"{LOG_TAG} Failed to save on quit: {ex.Message}");
            }
        }
    }

    protected override void OnDestroy()
    {
        StopAutoSave();
        base.OnDestroy();
    }
}
