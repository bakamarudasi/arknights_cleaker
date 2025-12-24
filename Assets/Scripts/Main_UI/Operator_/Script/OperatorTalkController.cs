using UnityEngine;
using UnityEngine.UIElements;
using System;
using System.Collections.Generic;

/// <summary>
/// オペレーター画面での会話機能を制御
/// CharacterSceneData.conversationsからリストを生成し、
/// ConversationControllerを使って会話を再生する
/// </summary>
public class OperatorTalkController
{
    // UI要素
    private VisualElement root;
    private Button btnRandomTalk;
    private VisualElement talkListContainer;

    // 現在の会話リスト
    private List<SceneConversation> currentConversations = new List<SceneConversation>();
    private CharacterSceneData currentScene;
    private string currentCharacterId;

    // 再生済み会話の追跡（playOnce用）
    private HashSet<string> playedConversations = new HashSet<string>();

    // コールバック参照
    private EventCallback<ClickEvent> callbackRandomTalk;

    // イベント
    public event Action<SceneConversation> OnConversationStarted;
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
    /// シーンが変更されたときに呼ばれる
    /// 会話リストを更新する
    /// </summary>
    public void UpdateForScene(string characterId, CharacterSceneData scene)
    {
        currentCharacterId = characterId;
        currentScene = scene;

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

        int currentAffection = GetCurrentAffectionLevel();

        if (currentScene == null || currentScene.conversations == null)
        {
            ShowEmptyMessage();
            UpdateRandomTalkButton();
            return;
        }

        foreach (var conv in currentScene.conversations)
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

    private void CreateTalkItem(SceneConversation conv, int currentAffection)
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
        var randomTalks = currentScene?.GetRandomTalks(currentAffection);
        bool hasRandomTalks = randomTalks != null && randomTalks.Count > 0;

        btnRandomTalk.SetEnabled(hasRandomTalks);
        btnRandomTalk.style.opacity = hasRandomTalks ? 1f : 0.5f;
    }

    private void OnRandomTalkClicked(ClickEvent evt)
    {
        if (currentScene == null) return;

        int currentAffection = GetCurrentAffectionLevel();
        var randomTalks = currentScene.GetRandomTalks(currentAffection);

        if (randomTalks == null || randomTalks.Count == 0)
        {
            LogUIController.Msg("ランダム会話がありません");
            return;
        }

        var selected = randomTalks[UnityEngine.Random.Range(0, randomTalks.Count)];
        StartConversation(selected);
    }

    private void OnTalkItemClicked(SceneConversation conv)
    {
        StartConversation(conv);
    }

    private void StartConversation(SceneConversation conv)
    {
        if (conv == null || conv.conversationData == null)
        {
            LogUIController.Msg("会話データがありません");
            return;
        }

        if (conv.playOnce && IsConversationPlayed(conv))
        {
            LogUIController.Msg("この会話は既に見ました");
            return;
        }

        OnConversationStarted?.Invoke(conv);

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
            Debug.LogWarning("[OperatorTalk] No ConversationController found in scene");
            OnConversationComplete(conv);
        }
    }

    private void OnConversationComplete(SceneConversation conv)
    {
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
    private string GetConversationKey(SceneConversation conv)
    {
        return $"{currentCharacterId}_{currentScene?.sceneId}_{conv.title}";
    }

    private bool IsConversationPlayed(SceneConversation conv)
    {
        return playedConversations.Contains(GetConversationKey(conv));
    }

    private void MarkConversationPlayed(SceneConversation conv)
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
