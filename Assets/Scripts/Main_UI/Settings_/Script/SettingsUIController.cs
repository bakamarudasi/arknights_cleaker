using UnityEngine;
using UnityEngine.UIElements;

/// <summary>
/// 設定画面のUIコントローラー
/// 音量調整、データリセット、クレジット表示
/// </summary>
public class SettingsUIController : IViewController
{
    private VisualElement root;

    // ========================================
    // UI要素参照
    // ========================================

    // 音量スライダー
    private Slider sliderMaster;
    private Slider sliderBGM;
    private Slider sliderSE;
    private Slider sliderVoice;

    // 音量ラベル
    private Label valueMaster;
    private Label valueBGM;
    private Label valueSE;
    private Label valueVoice;

    // アクションボタン
    private Button btnReset;

    // 確認ダイアログ
    private VisualElement confirmOverlay;
    private Button btnConfirmCancel;
    private Button btnConfirmOk;

    // ========================================
    // 初期化
    // ========================================

    public void Initialize(VisualElement contentArea)
    {
        root = contentArea;

        QueryElements();
        SetupCallbacks();
        LoadCurrentSettings();

        LogUIController.LogSystem("Settings View Initialized.");
    }

    private void QueryElements()
    {
        // 音量スライダー
        sliderMaster = root.Q<Slider>("slider-master");
        sliderBGM = root.Q<Slider>("slider-bgm");
        sliderSE = root.Q<Slider>("slider-se");
        sliderVoice = root.Q<Slider>("slider-voice");

        // 音量ラベル
        valueMaster = root.Q<Label>("value-master");
        valueBGM = root.Q<Label>("value-bgm");
        valueSE = root.Q<Label>("value-se");
        valueVoice = root.Q<Label>("value-voice");

        // アクションボタン
        btnReset = root.Q<Button>("btn-reset");

        // 確認ダイアログ
        confirmOverlay = root.Q<VisualElement>("confirm-overlay");
        btnConfirmCancel = root.Q<Button>("btn-confirm-cancel");
        btnConfirmOk = root.Q<Button>("btn-confirm-ok");
    }

    private void SetupCallbacks()
    {
        // 音量スライダー
        if (sliderMaster != null)
        {
            sliderMaster.RegisterValueChangedCallback(evt =>
            {
                OnMasterVolumeChanged(evt.newValue);
            });
        }

        if (sliderBGM != null)
        {
            sliderBGM.RegisterValueChangedCallback(evt =>
            {
                OnBGMVolumeChanged(evt.newValue);
            });
        }

        if (sliderSE != null)
        {
            sliderSE.RegisterValueChangedCallback(evt =>
            {
                OnSEVolumeChanged(evt.newValue);
            });
        }

        if (sliderVoice != null)
        {
            sliderVoice.RegisterValueChangedCallback(evt =>
            {
                OnVoiceVolumeChanged(evt.newValue);
            });
        }

        // リセットボタン
        if (btnReset != null)
        {
            btnReset.clicked += ShowResetConfirmDialog;
        }

        // 確認ダイアログ
        if (btnConfirmCancel != null)
        {
            btnConfirmCancel.clicked += HideConfirmDialog;
        }

        if (btnConfirmOk != null)
        {
            btnConfirmOk.clicked += OnConfirmReset;
        }
    }

    private void LoadCurrentSettings()
    {
        // AudioManagerから現在の設定を取得
        if (AudioManager.Instance != null)
        {
            if (sliderMaster != null)
            {
                sliderMaster.value = AudioManager.Instance.masterVolume;
                UpdateVolumeLabel(valueMaster, AudioManager.Instance.masterVolume);
            }

            if (sliderBGM != null)
            {
                sliderBGM.value = AudioManager.Instance.bgmVolume;
                UpdateVolumeLabel(valueBGM, AudioManager.Instance.bgmVolume);
            }

            if (sliderSE != null)
            {
                sliderSE.value = AudioManager.Instance.seVolume;
                UpdateVolumeLabel(valueSE, AudioManager.Instance.seVolume);
            }

            if (sliderVoice != null)
            {
                sliderVoice.value = AudioManager.Instance.voiceVolume;
                UpdateVolumeLabel(valueVoice, AudioManager.Instance.voiceVolume);
            }
        }
        else
        {
            // AudioManagerがない場合はデフォルト値を表示
            UpdateVolumeLabel(valueMaster, 1f);
            UpdateVolumeLabel(valueBGM, 0.7f);
            UpdateVolumeLabel(valueSE, 1f);
            UpdateVolumeLabel(valueVoice, 1f);
        }
    }

    // ========================================
    // 音量変更処理
    // ========================================

    private void OnMasterVolumeChanged(float value)
    {
        UpdateVolumeLabel(valueMaster, value);
        AudioManager.Instance?.SetMasterVolume(value);
    }

    private void OnBGMVolumeChanged(float value)
    {
        UpdateVolumeLabel(valueBGM, value);
        AudioManager.Instance?.SetBGMVolume(value);
    }

    private void OnSEVolumeChanged(float value)
    {
        UpdateVolumeLabel(valueSE, value);
        AudioManager.Instance?.SetSEVolume(value);
    }

    private void OnVoiceVolumeChanged(float value)
    {
        UpdateVolumeLabel(valueVoice, value);
        AudioManager.Instance?.SetVoiceVolume(value);
    }

    private void UpdateVolumeLabel(Label label, float value)
    {
        if (label != null)
        {
            label.text = $"{Mathf.RoundToInt(value * 100)}%";
        }
    }

    // ========================================
    // リセット処理
    // ========================================

    private void ShowResetConfirmDialog()
    {
        confirmOverlay?.AddToClassList("show");
        LogUIController.Msg("Warning: Data reset requested.");
    }

    private void HideConfirmDialog()
    {
        confirmOverlay?.RemoveFromClassList("show");
    }

    private void OnConfirmReset()
    {
        HideConfirmDialog();

        // 全てのPlayerPrefsを削除
        PlayerPrefs.DeleteAll();
        PlayerPrefs.Save();

        LogUIController.Msg("All save data has been deleted.");
        LogUIController.LogSystem("Game will restart...");

        // スタート画面に戻る
        root.schedule.Execute(() =>
        {
            MainUIController.Instance?.SwitchToMenu(MenuType.Start);
        }).ExecuteLater(500);
    }

    // ========================================
    // クリーンアップ
    // ========================================

    public void Dispose()
    {
        // ボタンイベント解除
        if (btnReset != null)
        {
            btnReset.clicked -= ShowResetConfirmDialog;
        }

        if (btnConfirmCancel != null)
        {
            btnConfirmCancel.clicked -= HideConfirmDialog;
        }

        if (btnConfirmOk != null)
        {
            btnConfirmOk.clicked -= OnConfirmReset;
        }

        LogUIController.LogSystem("Settings View Disposed.");
    }
}
