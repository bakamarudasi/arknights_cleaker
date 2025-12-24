using UnityEngine;
using UnityEngine.UIElements;
using System;
using System.Collections.Generic;

/// <summary>
/// オペレーター画面での会話機能を制御
/// CharacterSceneData.conversationsまたはCharacterPoseData.PoseEntry.conversationsからリストを生成し、
/// ConversationControllerを使って会話を再生する
/// </summary>
public class OperatorTalkController
{
    // UI要素
    private VisualElement root;
    private Button btnRandomTalk;
    private VisualElement talkListContainer;

    // 現在の会話リスト（シーンベース）
    private List<SceneConversation> currentSceneConversations = new List<SceneConversation>();
    private CharacterSceneData currentScene;

    // レガシー: 現在の会話リスト（ポーズベース）
    private List<CharacterPoseData.PoseConversation> currentConversations = new List<CharacterPoseData.PoseConversation>();
    private CharacterPoseData.PoseEntry currentPose;

    private string currentCharacterId;
    private bool isUsingSceneData = false;

    // 再生済み会話の追跡（playOnce用）
    private HashSet<string> playedConversations = new HashSet<string>();

    // コールバック参照
    private EventCallback<ClickEvent> callbackRandomTalk;

    // イベント
    public event Action<SceneConversation> OnSceneConversationStarted;
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
    /// シーンが変更されたときに呼ばれる
    /// 会話リストを更新する
    /// </summary>
    public void UpdateForScene(string characterId, CharacterSceneData scene)
    {
        currentCharacterId = characterId;
        currentScene = scene;
        currentPose = null;
        isUsingSceneData = true;

        RefreshConversationList();
    }

    /// <summary>
    /// ポーズが変更されたときに呼ばれる - レガシー互換
    /// 会話リストを更新する
    /// </summary>
    public void UpdateForPose(string characterId, CharacterPoseData.PoseEntry pose)
    {
        currentCharacterId = characterId;
        currentPose = pose;
        currentScene = null;
        isUsingSceneData = false;

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
        currentSceneConversations.Clear();

        int currentAffection = GetCurrentAffectionLevel();

        if (isUsingSceneData)
        {
            // シーンベースの会話リスト
            if (currentScene == null || currentScene.conversations == null)
            {
                ShowEmptyMessage();
                UpdateRandomTalkButton();
                return;
            }

            foreach (var conv in currentScene.conversations)
            {
                currentSceneConversations.Add(conv);
                CreateSceneTalkItem(conv, currentAffection);
            }

            if (currentSceneConversations.Count == 0)
            {
                ShowEmptyMessage();
            }
        }
        else
        {
            // レガシー: ポーズベースの会話リスト
            if (currentPose == null || currentPose.conversations == null)
            {
                ShowEmptyMessage();
                UpdateRandomTalkButton();
                return;
            }

            foreach (var conv in currentPose.conversations)
            {
                currentConversations.Add(conv);
                CreateTalkItem(conv, currentAffection);
            }

            if (currentConversations.Count == 0)
            {
                ShowEmptyMessage();
            }
        }

        UpdateRandomTalkButton();
    }

    private void CreateSceneTalkItem(SceneConversation conv, int currentAffection)
    {
        bool isLocked = conv.requiredAffectionLevel > currentAffection;
        bool isPlayed = conv.playOnce && IsSceneConversationPlayed(conv);

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
            item.RegisterCallback<ClickEvent>(evt => OnSceneTalkItemClicked(conv));
        }

        talkListContainer.Add(item);
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
        bool hasRandomTalks = false;

        if (isUsingSceneData)
        {
            var randomTalks = currentScene?.GetRandomTalks(currentAffection);
            hasRandomTalks = randomTalks != null && randomTalks.Count > 0;
        }
        else
        {
            var randomTalks = currentPose?.GetRandomTalks(currentAffection);
            hasRandomTalks = randomTalks != null && randomTalks.Count > 0;
        }

        btnRandomTalk.SetEnabled(hasRandomTalks);
        btnRandomTalk.style.opacity = hasRandomTalks ? 1f : 0.5f;
    }

    private void OnRandomTalkClicked(ClickEvent evt)
    {
        int currentAffection = GetCurrentAffectionLevel();

        if (isUsingSceneData)
        {
            if (currentScene == null) return;

            var randomTalks = currentScene.GetRandomTalks(currentAffection);
            if (randomTalks == null || randomTalks.Count == 0)
            {
                LogUIController.Msg("ランダム会話がありません");
                return;
            }

            var selected = randomTalks[UnityEngine.Random.Range(0, randomTalks.Count)];
            StartSceneConversation(selected);
        }
        else
        {
            if (currentPose == null) return;

            var randomTalks = currentPose.GetRandomTalks(currentAffection);
            if (randomTalks == null || randomTalks.Count == 0)
            {
                LogUIController.Msg("ランダム会話がありません");
                return;
            }

            var selected = randomTalks[UnityEngine.Random.Range(0, randomTalks.Count)];
            StartConversation(selected);
        }
    }

    // シーン会話用ハンドラ
    private void OnSceneTalkItemClicked(SceneConversation conv)
    {
        StartSceneConversation(conv);
    }

    private void StartSceneConversation(SceneConversation conv)
    {
        if (conv == null || conv.conversationData == null)
        {
            LogUIController.Msg("会話データがありません");
            return;
        }

        if (conv.playOnce && IsSceneConversationPlayed(conv))
        {
            LogUIController.Msg("この会話は既に見ました");
            return;
        }

        OnSceneConversationStarted?.Invoke(conv);

        var conversationController = UnityEngine.Object.FindAnyObjectByType<ConversationController>();
        if (conversationController != null)
        {
            conversationController.Initialize(conv.conversationData, () =>
            {
                OnSceneConversationComplete(conv);
            });
        }
        else
        {
            Debug.LogWarning("[OperatorTalk] No ConversationController found in scene");
            OnSceneConversationComplete(conv);
        }
    }

    private void OnSceneConversationComplete(SceneConversation conv)
    {
        if (conv.playOnce)
        {
            MarkSceneConversationPlayed(conv);
            RefreshConversationList();
        }

        OnConversationEnded?.Invoke();
    }

    // レガシー: ポーズ会話用ハンドラ
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

    private void OnConversationComplete(CharacterPoseData.PoseConversation conv)
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

    // 再生済み会話の管理（シーンベース）
    private string GetSceneConversationKey(SceneConversation conv)
    {
        return $"{currentCharacterId}_{currentScene?.sceneId}_{conv.title}";
    }

    private bool IsSceneConversationPlayed(SceneConversation conv)
    {
        return playedConversations.Contains(GetSceneConversationKey(conv));
    }

    private void MarkSceneConversationPlayed(SceneConversation conv)
    {
        playedConversations.Add(GetSceneConversationKey(conv));
        // TODO: 永続化が必要なら SaveManager と連携
    }

    // 再生済み会話の管理（レガシー: ポーズベース）
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
