using UnityEngine;
using System;
using System.Collections.Generic;

/// <summary>
/// キャラクターのシーン（ポーズ/衣装）管理
/// シーン切り替え、アンロック状態の確認を担当
/// </summary>
public class CharacterSceneManager
{
    // 状態
    private CharacterData _currentCharacter;
    private CharacterSceneData _currentScene;
    private string _currentSceneId;

    // イベント
    public event Action<string> OnSceneChanged;
    public event Action<CharacterData> OnCharacterLoaded;

    // プロパティ
    public CharacterData CurrentCharacter => _currentCharacter;
    public CharacterSceneData CurrentScene => _currentScene;
    public string CurrentSceneId => _currentSceneId;
    public bool HasCharacter => _currentCharacter != null;

    /// <summary>
    /// キャラクターデータを読み込み
    /// </summary>
    /// <returns>デフォルトシーンのプレハブ（なければnull）</returns>
    public GameObject LoadCharacter(CharacterData data)
    {
        if (data == null)
        {
            Debug.LogWarning("[SceneManager] CharacterData is null!");
            return null;
        }

        _currentCharacter = data;
        _currentSceneId = data.defaultSceneId;

        var defaultScene = data.GetDefaultScene();
        if (defaultScene == null)
        {
            Debug.LogWarning($"[SceneManager] No scenes found in CharacterData: {data.characterId}");
            return null;
        }

        _currentScene = defaultScene;

        Debug.Log($"[SceneManager] Loaded: {data.characterId}, default scene: {_currentSceneId}");
        OnCharacterLoaded?.Invoke(data);
        OnSceneChanged?.Invoke(_currentSceneId);

        return defaultScene.prefab;
    }

    /// <summary>
    /// シーンを切り替え
    /// </summary>
    /// <returns>新しいシーンのプレハブ（切り替え不要/失敗ならnull）</returns>
    public (GameObject prefab, float recommendedCameraSize, bool success) SetScene(string sceneId)
    {
        if (_currentCharacter == null)
        {
            Debug.LogWarning("[SceneManager] No character loaded.");
            return (null, 0, false);
        }

        var sceneData = _currentCharacter.GetScene(sceneId);
        if (sceneData == null)
        {
            Debug.LogWarning($"[SceneManager] Scene not found: {sceneId}");
            return (null, 0, false);
        }

        if (sceneData.prefab == null)
        {
            Debug.LogWarning($"[SceneManager] Scene prefab is null: {sceneId}");
            return (null, 0, false);
        }

        // 同じシーンなら切り替え不要
        if (_currentSceneId == sceneId)
        {
            Debug.Log($"[SceneManager] Already showing: {sceneId}");
            return (null, 0, true); // 成功だがprefabはnull（切り替え不要）
        }

        string previousSceneId = _currentSceneId;
        _currentSceneId = sceneId;
        _currentScene = sceneData;

        Debug.Log($"[SceneManager] Scene changed: {previousSceneId} → {sceneId}");
        OnSceneChanged?.Invoke(sceneId);

        return (sceneData.prefab, sceneData.recommendedCameraSize, true);
    }

    /// <summary>
    /// 利用可能なシーン一覧を取得
    /// </summary>
    public List<CharacterSceneData> GetAvailableScenes(int currentAffectionLevel = 999)
    {
        if (_currentCharacter == null)
        {
            return new List<CharacterSceneData>();
        }
        return _currentCharacter.GetUnlockedScenes(currentAffectionLevel);
    }

    /// <summary>
    /// 全シーン一覧を取得（ロック状態含む）
    /// </summary>
    public List<CharacterSceneData> GetAllScenes()
    {
        if (_currentCharacter == null)
        {
            return new List<CharacterSceneData>();
        }
        return _currentCharacter.scenes;
    }

    /// <summary>
    /// 指定シーンがアンロック済みか確認
    /// </summary>
    public bool IsSceneUnlocked(string sceneId, int currentAffectionLevel = 999)
    {
        if (_currentCharacter == null) return false;

        var scene = _currentCharacter.GetScene(sceneId);
        if (scene == null) return false;

        return scene.IsUnlocked(currentAffectionLevel);
    }

    /// <summary>
    /// 状態をクリア
    /// </summary>
    public void Clear()
    {
        _currentCharacter = null;
        _currentScene = null;
        _currentSceneId = null;
    }
}
