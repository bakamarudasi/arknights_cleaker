using UnityEngine;
using UnityEngine.UIElements;
using System;
using System.Collections.Generic;

/// <summary>
/// オペレーター画面での会話機能を制御
/// CharacterPoseData.PoseEntry.conversationsからリストを生成し、
/// ConversationControllerを使って会話を再生する
/// </summary>
public class OperatorTalkController
{
    // UI要素
    private VisualElement root;
    private Button btnRandomTalk;
    private VisualElement talkListContainer;

    // 現在の会話リスト
    private List<CharacterPoseData.PoseConversation> currentConversations = new List<CharacterPoseData.PoseConversation>();
    private CharacterPoseData.PoseEntry currentPose;
    private string currentCharacterId;

    // 再生済み会話の追跡（playOnce用）
    private HashSet<string> playedConversations = new HashSet<string>();

    // コールバック参照
    private EventCallback<ClickEvent> callbackRandomTalk;

    // イベント
    public event Action<CharacterPoseData.PoseConversation> OnConversationStarted;
    public event Action OnConversationEnded;

    private const string CLS_LOCKED = "locked";

    public void Initialize(VisualElement rootElement)
    {
        root = rootElement;
        SetupUI();
    }

    private void SetupUI()
    {
        if (root == null) return;

        btnRandomTalk = root.Q<Button>("btn-random-talk");
        talkListContainer = root.Q<VisualElement>("talk-list-container");

        callbackRandomTalk = OnRandomTalkClicked;
        btnRandomTalk?.RegisterCallback(callbackRandomTalk);
    }

    /// <summary>
    /// ポーズが変更されたときに呼ばれる
    /// 会話リストを更新する
    /// </summary>
    public void UpdateForPose(string characterId, CharacterPoseData.PoseEntry pose)
    {
        currentCharacterId = characterId;
        currentPose = pose;

        RefreshConversationList();
    }

    /// <summary>
    /// 会話リストを再構築
    /// </summary>
    public void RefreshConversationList()
    {
        if (talkListContainer == null) return;

        talkListContainer.Clear();
        currentConversations.Clear();

        if (currentPose == null || currentPose.conversations == null)
        {
            ShowEmptyMessage();
            UpdateRandomTalkButton();
            return;
        }

        int currentAffection = GetCurrentAffectionLevel();

        foreach (var conv in currentPose.conversations)
        {
            currentConversations.Add(conv);
            CreateTalkItem(conv, currentAffection);
        }

        if (currentConversations.Count == 0)
        {
            ShowEmptyMessage();
        }

        UpdateRandomTalkButton();
    }

    private void CreateTalkItem(CharacterPoseData.PoseConversation conv, int currentAffection)
    {
        bool isLocked = conv.requiredAffectionLevel > currentAffection;
        bool isPlayed = conv.playOnce && IsConversationPlayed(conv);

        var item = new VisualElement();
        item.AddToClassList("talk-item");

        if (isLocked)
            item.AddToClassList(CLS_LOCKED);

        var titleLabel = new Label(conv.title);
        titleLabel.AddToClassList("talk-item-title");

        var statusLabel = new Label();
        statusLabel.AddToClassList("talk-item-status");

        if (isLocked)
        {
            statusLabel.text = $"Lv.{conv.requiredAffectionLevel}";
        }
        else if (isPlayed)
        {
            statusLabel.text = "PLAYED";
        }
        else if (conv.isRandomTalk)
        {
            statusLabel.text = "RANDOM";
        }

        item.Add(titleLabel);
        item.Add(statusLabel);

        if (!isLocked)
        {
            item.RegisterCallback<ClickEvent>(evt => OnTalkItemClicked(conv));
        }

        talkListContainer.Add(item);
    }

    private void ShowEmptyMessage()
    {
        var emptyLabel = new Label("No conversations available");
        emptyLabel.AddToClassList("talk-empty-text");
        talkListContainer.Add(emptyLabel);
    }

    private void UpdateRandomTalkButton()
    {
        if (btnRandomTalk == null) return;

        int currentAffection = GetCurrentAffectionLevel();
        var randomTalks = currentPose?.GetRandomTalks(currentAffection);

        bool hasRandomTalks = randomTalks != null && randomTalks.Count > 0;
        btnRandomTalk.SetEnabled(hasRandomTalks);
        btnRandomTalk.style.opacity = hasRandomTalks ? 1f : 0.5f;
    }

    private void OnRandomTalkClicked(ClickEvent evt)
    {
        if (currentPose == null) return;

        int currentAffection = GetCurrentAffectionLevel();
        var randomTalks = currentPose.GetRandomTalks(currentAffection);

        if (randomTalks == null || randomTalks.Count == 0)
        {
            LogUIController.Msg("ランダム会話がありません");
            return;
        }

        // ランダムに選択
        var selected = randomTalks[UnityEngine.Random.Range(0, randomTalks.Count)];
        StartConversation(selected);
    }

    private void OnTalkItemClicked(CharacterPoseData.PoseConversation conv)
    {
        StartConversation(conv);
    }

    private void StartConversation(CharacterPoseData.PoseConversation conv)
    {
        if (conv == null || conv.conversationData == null)
        {
            LogUIController.Msg("会話データがありません");
            return;
        }

        // playOnceで既に再生済み
        if (conv.playOnce && IsConversationPlayed(conv))
        {
            LogUIController.Msg("この会話は既に見ました");
            return;
        }

        OnConversationStarted?.Invoke(conv);

        // シーン内のConversationControllerを探す
        var conversationController = UnityEngine.Object.FindAnyObjectByType<ConversationController>();
        if (conversationController != null)
        {
            conversationController.Initialize(conv.conversationData, () =>
            {
                OnConversationComplete(conv);
            });
        }
        else
        {
            // ConversationControllerがない場合はログを出す
            Debug.LogWarning("[OperatorTalk] No ConversationController found in scene");
            OnConversationComplete(conv);
        }
    }

    private void OnConversationComplete(CharacterPoseData.PoseConversation conv)
    {
        // playOnceの場合は再生済みとしてマーク
        if (conv.playOnce)
        {
            MarkConversationPlayed(conv);
            RefreshConversationList();
        }

        OnConversationEnded?.Invoke();
    }

    private int GetCurrentAffectionLevel()
    {
        if (string.IsNullOrEmpty(currentCharacterId)) return 0;

        if (AffectionManager.Instance != null)
        {
            return AffectionManager.Instance.GetLevel(currentCharacterId);
        }
        return 0;
    }

    // 再生済み会話の管理
    private string GetConversationKey(CharacterPoseData.PoseConversation conv)
    {
        return $"{currentCharacterId}_{currentPose?.poseId}_{conv.title}";
    }

    private bool IsConversationPlayed(CharacterPoseData.PoseConversation conv)
    {
        return playedConversations.Contains(GetConversationKey(conv));
    }

    private void MarkConversationPlayed(CharacterPoseData.PoseConversation conv)
    {
        playedConversations.Add(GetConversationKey(conv));
        // TODO: 永続化が必要なら SaveManager と連携
    }

    public void Dispose()
    {
        if (callbackRandomTalk != null)
            btnRandomTalk?.UnregisterCallback(callbackRandomTalk);

        callbackRandomTalk = null;
    }
}
