using UnityEngine;
using UnityEngine.UIElements;
using System;
using System.Collections.Generic;

/// <summary>
/// PSBキャラ表示（RenderTexture方式）
/// 専用カメラで撮影した映像をUI Toolkitの背景に設定する
///
/// 設定手順:
/// 1. RenderTexture作成 (1024x1024, ARGB32)
/// 2. 専用カメラ作成 (Orthographic, 遠い位置に配置)
/// 3. このスクリプトにカメラとRTをアサイン
///
/// ポーズ切り替え:
/// 1. CharacterData (ScriptableObject) を作成
/// 2. LoadCharacter(characterData) でキャラ読み込み
/// 3. SetPose("poseId") でポーズ切り替え
/// </summary>
public class OverlayCharacterPresenter : MonoBehaviour
{
    public static OverlayCharacterPresenter Instance { get; private set; }

    [Header("=== RenderTexture設定 ===")]
    [Tooltip("キャラ撮影用カメラ（なければ自動生成）")]
    [SerializeField] private Camera characterCamera;

    [Tooltip("カメラが書き込むRenderTexture（なければ自動生成）")]
    [SerializeField] private RenderTexture renderTexture;

    [Tooltip("RenderTextureのサイズ（自動生成時）※PSBが3000px級なら2048以上推奨")]
    [SerializeField] private Vector2Int renderTextureSize = new Vector2Int(2048, 2048);

    [Header("=== キャラクター設定 ===")]
    [Tooltip("表示するPSBキャラのプレハブ（単体使用時）")]
    [SerializeField] private GameObject characterPrefab;

    [Tooltip("キャラを配置する位置（カメラから離れた場所）")]
    [SerializeField] private Vector3 characterSpawnPosition = new Vector3(1000f, 0f, 0f);

    [Header("=== ポーズ管理 ===")]
    [Tooltip("キャラクターデータ（ScriptableObject）")]
    [SerializeField] private CharacterData characterData;

    // ポーズ管理用内部状態
    private CharacterData _currentCharacterData;
    private string _currentPoseId;

    [Header("=== カメラ設定 ===")]
    [Tooltip("カメラのOrthographicSize（キャラの大きさ調整）")]
    [SerializeField] private float cameraOrthoSize = 5f;

    [Tooltip("カメラの背景色（透明推奨）")]
    [SerializeField] private Color cameraBackgroundColor = new Color(0, 0, 0, 0);

    // 内部状態
    private GameObject _currentInstance;
    private GameObject _cameraRig; // カメラ + キャラを束ねる親
    private VisualElement _displayElement;
    private bool _isShowing;

    // クリック処理用
    private Action<Vector2> _onClickCallback;

    // イベント
    public event Action OnCharacterReady;
#pragma warning disable CS0067 // 将来の拡張用に予約
    public event Action<CharacterInteractionZone.ZoneType, int> OnZoneTouched;
#pragma warning restore CS0067

    /// <summary>ポーズ変更時に発火（poseId）</summary>
    public event Action<string> OnPoseChanged;

    /// <summary>キャラクター読み込み時に発火</summary>
    public event Action<CharacterData> OnCharacterLoaded;

    // ========================================
    // プロパティ
    // ========================================

    public Camera CharacterCamera => characterCamera;
    public RenderTexture RenderTexture => renderTexture;
    public GameObject CurrentInstance => _currentInstance;
    public bool IsShowing => _isShowing;

    /// <summary>現在読み込まれているキャラクターデータ</summary>
    public CharacterData CurrentCharacterData => _currentCharacterData;

    /// <summary>現在表示中のポーズID</summary>
    public string CurrentPoseId => _currentPoseId;

    /// <summary>現在のポーズエントリ</summary>
    public PoseEntry CurrentPoseEntry =>
        _currentCharacterData?.GetPose(_currentPoseId);

    /// <summary>現在のレイヤーコントローラー</summary>
    public CharacterLayerController LayerController => _layerController;
    private CharacterLayerController _layerController;

    /// <summary>レンズマスクコントローラー</summary>
    public LensMaskController LensMask => _lensMaskController;
    private LensMaskController _lensMaskController;

    // ========================================
    // 初期化
    // ========================================

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        SetupRenderSystem();

        // InspectorでCharacterDataが設定されていれば自動読み込み
        if (characterData != null)
        {
            LoadCharacter(characterData, autoShow: false);
        }
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
        Cleanup();
    }

    /// <summary>
    /// カメラとRenderTextureをセットアップ
    /// </summary>
    private void SetupRenderSystem()
    {
        // カメラリグを作成（すべてをまとめる親）
        _cameraRig = new GameObject("CharacterRenderRig");
        _cameraRig.hideFlags = HideFlags.HideAndDontSave;
        _cameraRig.transform.SetParent(transform);
        _cameraRig.transform.position = characterSpawnPosition;

        // RenderTextureがなければ作成
        if (renderTexture == null)
        {
            renderTexture = new RenderTexture(
                renderTextureSize.x,
                renderTextureSize.y,
                24, // depth
                RenderTextureFormat.ARGB32
            );
            renderTexture.name = "CharacterRenderTexture";
            renderTexture.hideFlags = HideFlags.HideAndDontSave;
            renderTexture.Create();
            Debug.Log($"[Presenter] Created RenderTexture: {renderTextureSize.x}x{renderTextureSize.y}");
        }

        // カメラがなければ作成
        if (characterCamera == null)
        {
            var camObj = new GameObject("CharacterCamera");
            camObj.hideFlags = HideFlags.HideAndDontSave;
            camObj.transform.SetParent(_cameraRig.transform);
            camObj.transform.localPosition = new Vector3(0, 0, -10f);
            camObj.transform.localRotation = Quaternion.identity;

            characterCamera = camObj.AddComponent<Camera>();
            characterCamera.orthographic = true;
            characterCamera.orthographicSize = cameraOrthoSize;
            characterCamera.clearFlags = CameraClearFlags.SolidColor;
            characterCamera.backgroundColor = cameraBackgroundColor;
            characterCamera.cullingMask = -1; // Everything（後でレイヤー設定推奨）
            characterCamera.targetTexture = renderTexture;
            characterCamera.depth = -100; // メインカメラより低く

            Debug.Log("[Presenter] Created CharacterCamera");
        }
        else
        {
            // 既存カメラにRTを設定
            characterCamera.targetTexture = renderTexture;
        }

        // 初期状態は非アクティブ
        _cameraRig.SetActive(false);
    }

    // ========================================
    // 表示エリア設定
    // ========================================

    /// <summary>
    /// UI側の表示エリアを設定し、RenderTextureを背景として流し込む
    /// </summary>
    public void SetDisplayArea(VisualElement element)
    {
        _displayElement = element;

        if (_displayElement != null && renderTexture != null)
        {
            // RenderTextureをUIの背景画像として設定
            _displayElement.style.backgroundImage = new StyleBackground(
                Background.FromRenderTexture(renderTexture)
            );

            // アスペクト比を維持してフィット
            _displayElement.style.backgroundPositionX = new BackgroundPosition(BackgroundPositionKeyword.Center);
            _displayElement.style.backgroundPositionY = new BackgroundPosition(BackgroundPositionKeyword.Center);
            _displayElement.style.backgroundRepeat = new BackgroundRepeat(Repeat.NoRepeat, Repeat.NoRepeat);
            _displayElement.style.backgroundSize = new BackgroundSize(BackgroundSizeType.Contain);

            Debug.Log("[Presenter] RenderTexture assigned to VisualElement");
        }
    }

    /// <summary>
    /// クリックコールバックを設定（座標はVisualElement内のローカル座標）
    /// </summary>
    public void SetClickCallback(Action<Vector2> callback)
    {
        _onClickCallback = callback;
    }

    // ========================================
    // 表示制御
    // ========================================

    /// <summary>
    /// 準備（互換性のため）
    /// </summary>
    public void EnsureCreated()
    {
        // RenderTexture方式では不要
    }

    /// <summary>
    /// キャラクターを表示
    /// </summary>
    public void Show()
    {
        if (characterPrefab == null)
        {
            Debug.LogWarning("[Presenter] characterPrefab is NULL!");
            return;
        }

        // 既存インスタンスがあれば再利用
        if (_currentInstance != null)
        {
            _cameraRig.SetActive(true);
            _currentInstance.SetActive(true);
            _isShowing = true;
            return;
        }

        // キャラクターを生成（カメラリグの子として）
        _currentInstance = Instantiate(characterPrefab, _cameraRig.transform);
        _currentInstance.hideFlags = HideFlags.HideAndDontSave;
        _currentInstance.transform.localPosition = Vector3.zero;
        _currentInstance.transform.localRotation = Quaternion.identity;

        // カメラリグをアクティブ化
        _cameraRig.SetActive(true);
        _isShowing = true;

        // インタラクションゾーンにコライダーがあることを確認
        SetupInteractionZones();

        // レイヤーコントローラーを取得
        _layerController = _currentInstance.GetComponent<CharacterLayerController>();
        if (_layerController != null)
        {
            Debug.Log($"[Presenter] LayerController found: {_layerController.LayerCount} layers");
        }

        // カメラサイズをキャラに合わせて調整
        AdjustCameraToCharacter();

        Debug.Log("[Presenter] Character shown via RenderTexture");
        OnCharacterReady?.Invoke();
    }

    /// <summary>
    /// 指定プレハブで表示
    /// </summary>
    public void Show(GameObject prefab)
    {
        if (prefab != null)
        {
            // 現在のインスタンスを破棄
            if (_currentInstance != null)
            {
                Destroy(_currentInstance);
                _currentInstance = null;
            }
            characterPrefab = prefab;
        }
        Show();
    }

    /// <summary>
    /// 非表示
    /// </summary>
    public void Hide()
    {
        if (_cameraRig != null)
        {
            _cameraRig.SetActive(false);
        }
        _isShowing = false;
    }

    /// <summary>
    /// 破棄
    /// </summary>
    public void DestroyCharacter()
    {
        if (_currentInstance != null)
        {
            Destroy(_currentInstance);
            _currentInstance = null;
        }
        _isShowing = false;
    }

    // ========================================
    // ポーズ管理
    // ========================================

    /// <summary>
    /// キャラクターデータを読み込み（デフォルトポーズで表示）
    /// </summary>
    /// <param name="data">CharacterData (ScriptableObject)</param>
    /// <param name="autoShow">読み込み後に自動表示するか</param>
    public void LoadCharacter(CharacterData data, bool autoShow = true)
    {
        if (data == null)
        {
            Debug.LogWarning("[Presenter] CharacterData is null!");
            return;
        }

        _currentCharacterData = data;
        _currentPoseId = data.defaultPoseId;

        var defaultPose = data.GetDefaultPose();
        if (defaultPose == null)
        {
            Debug.LogWarning($"[Presenter] No poses found in CharacterData: {data.characterId}");
            return;
        }

        characterPrefab = defaultPose.prefab;

        Debug.Log($"[Presenter] Loaded character: {data.characterId}, default pose: {_currentPoseId}");
        OnCharacterLoaded?.Invoke(data);

        if (autoShow)
        {
            Show();
        }
    }

    /// <summary>
    /// ポーズを切り替え
    /// </summary>
    /// <param name="poseId">ポーズID</param>
    /// <returns>成功したか</returns>
    public bool SetPose(string poseId)
    {
        if (_currentCharacterData == null)
        {
            Debug.LogWarning("[Presenter] No character loaded. Call LoadCharacter() first.");
            return false;
        }

        var poseEntry = _currentCharacterData.GetPose(poseId);
        if (poseEntry == null)
        {
            Debug.LogWarning($"[Presenter] Pose not found: {poseId}");
            return false;
        }

        if (poseEntry.prefab == null)
        {
            Debug.LogWarning($"[Presenter] Pose prefab is null: {poseId}");
            return false;
        }

        // 同じポーズなら何もしない
        if (_currentPoseId == poseId && _currentInstance != null)
        {
            Debug.Log($"[Presenter] Already showing pose: {poseId}");
            return true;
        }

        string previousPoseId = _currentPoseId;
        _currentPoseId = poseId;

        // 現在のインスタンスを破棄
        if (_currentInstance != null)
        {
            Destroy(_currentInstance);
            _currentInstance = null;
        }

        // 新しいプレハブを設定して表示
        characterPrefab = poseEntry.prefab;

        // 推奨カメラサイズがあれば設定
        if (poseEntry.recommendedCameraSize > 0)
        {
            cameraOrthoSize = poseEntry.recommendedCameraSize;
        }

        // 表示中なら新しいポーズで再表示
        if (_isShowing)
        {
            Show();
        }

        Debug.Log($"[Presenter] Pose changed: {previousPoseId} → {poseId}");
        OnPoseChanged?.Invoke(poseId);

        return true;
    }

    /// <summary>
    /// 利用可能なポーズ一覧を取得（UI用）
    /// </summary>
    /// <param name="currentAffectionLevel">現在の好感度レベル（アンロック判定用）</param>
    /// <returns>ポーズエントリのリスト</returns>
    public List<PoseEntry> GetAvailablePoses(int currentAffectionLevel = 999)
    {
        if (_currentCharacterData == null)
        {
            return new List<PoseEntry>();
        }
        return _currentCharacterData.GetUnlockedPoses(currentAffectionLevel);
    }

    /// <summary>
    /// 全ポーズ一覧を取得（ロック状態含む）
    /// </summary>
    public List<PoseEntry> GetAllPoses()
    {
        if (_currentCharacterData == null)
        {
            return new List<PoseEntry>();
        }
        return _currentCharacterData.poses;
    }

    /// <summary>
    /// 指定ポーズがアンロック済みか確認
    /// </summary>
    public bool IsPoseUnlocked(string poseId, int currentAffectionLevel = 999)
    {
        if (_currentCharacterData == null) return false;

        var pose = _currentCharacterData.GetPose(poseId);
        if (pose == null) return false;

        return !pose.isLocked || pose.requiredAffectionLevel <= currentAffectionLevel;
    }

    // ========================================
    // カメラ調整
    // ========================================

    /// <summary>
    /// キャラクターのサイズに合わせてカメラを調整
    /// </summary>
    private void AdjustCameraToCharacter()
    {
        if (_currentInstance == null || characterCamera == null) return;

        // SpriteRendererからBoundsを計算
        var renderers = _currentInstance.GetComponentsInChildren<SpriteRenderer>();
        if (renderers.Length == 0) return;

        Bounds bounds = renderers[0].bounds;
        foreach (var sr in renderers)
        {
            bounds.Encapsulate(sr.bounds);
        }

        // カメラのorthographicSizeをBoundsに合わせる
        float boundsHeight = bounds.size.y;
        float boundsWidth = bounds.size.x;

        // RenderTextureのアスペクト比を考慮
        float rtAspect = (float)renderTexture.width / renderTexture.height;
        float boundsAspect = boundsWidth / boundsHeight;

        if (boundsAspect > rtAspect)
        {
            // 横長のキャラ → 幅に合わせる
            characterCamera.orthographicSize = (boundsWidth / rtAspect) / 2f * 1.1f;
        }
        else
        {
            // 縦長のキャラ → 高さに合わせる
            characterCamera.orthographicSize = boundsHeight / 2f * 1.1f;
        }

        // キャラをBoundsの中心に配置
        Vector3 offset = bounds.center - _currentInstance.transform.position;
        _currentInstance.transform.localPosition = -offset;

        Debug.Log($"[Presenter] Camera adjusted: orthoSize={characterCamera.orthographicSize}, bounds={bounds.size}");
    }

    /// <summary>
    /// カメラのorthographicSizeを設定（ポーズ変更時など）
    /// </summary>
    public void SetCameraSize(float size)
    {
        cameraOrthoSize = size;
        if (characterCamera != null)
        {
            characterCamera.orthographicSize = size;
        }
    }

    /// <summary>
    /// レイアウトを更新（画面サイズ変更やポーズ変更時）
    /// </summary>
    public void RefreshLayout()
    {
        AdjustCameraToCharacter();
    }

    // ========================================
    // クリック処理（UI Toolkit座標 → ワールド座標）
    // ========================================

    /// <summary>
    /// UIクリック座標からワールド座標へのRaycastを実行
    /// </summary>
    /// <param name="normalizedPos">VisualElement内の正規化座標 (0-1)</param>
    /// <returns>ヒットしたコライダー（なければnull）</returns>
    public Collider2D RaycastFromUI(Vector2 normalizedPos)
    {
        if (characterCamera == null) return null;

        // 正規化座標をビューポート座標に変換（Y軸反転）
        Vector3 viewportPos = new Vector3(normalizedPos.x, 1f - normalizedPos.y, 0f);

        // カメラからRayを飛ばす
        Ray ray = characterCamera.ViewportPointToRay(viewportPos);

        // 2D Raycast
        RaycastHit2D hit = Physics2D.Raycast(ray.origin, ray.direction);

        return hit.collider;
    }

    /// <summary>
    /// UIクリック座標からCharacterInteractionZoneを検出
    /// </summary>
    public CharacterInteractionZone GetInteractionZoneAt(Vector2 normalizedPos)
    {
        var collider = RaycastFromUI(normalizedPos);
        if (collider != null)
        {
            return collider.GetComponent<CharacterInteractionZone>();
        }
        return null;
    }

    // ========================================
    // インタラクションゾーン
    // ========================================

    /// <summary>
    /// インタラクションゾーンをセットアップ
    /// </summary>
    private void SetupInteractionZones()
    {
        var zones = GetInteractionZones();
        foreach (var zone in zones)
        {
            // コライダーがなければ追加
            if (zone.GetComponent<Collider2D>() == null)
            {
                var collider = zone.gameObject.AddComponent<BoxCollider2D>();
                collider.isTrigger = true;
                Debug.Log($"[Presenter] Added Collider2D to zone: {zone.name}");
            }
        }
    }

    /// <summary>
    /// インタラクションゾーンを取得
    /// </summary>
    public CharacterInteractionZone[] GetInteractionZones()
    {
        if (_currentInstance == null) return Array.Empty<CharacterInteractionZone>();
        return _currentInstance.GetComponentsInChildren<CharacterInteractionZone>();
    }

    // ========================================
    // レイヤー制御（透視機能）
    // ========================================

    /// <summary>
    /// 透視レベルを設定
    /// </summary>
    /// <param name="level">透視レベル（0=通常, 1〜5=透視段階）</param>
    public void SetPenetrateLevel(int level)
    {
        if (_layerController != null)
        {
            _layerController.SetPenetrateLevel(level);
        }
        else
        {
            Debug.LogWarning("[Presenter] LayerController not found on character prefab");
        }
    }

    /// <summary>
    /// 透視レベルを即座に設定（フェードなし）
    /// </summary>
    public void SetPenetrateLevelImmediate(int level)
    {
        if (_layerController != null)
        {
            _layerController.SetPenetrateLevelImmediate(level);
        }
    }

    /// <summary>
    /// 通常表示に戻す
    /// </summary>
    public void ResetPenetrateLevel()
    {
        SetPenetrateLevel(0);
    }

    /// <summary>
    /// 現在の透視レベルを取得
    /// </summary>
    public int CurrentPenetrateLevel => _layerController?.CurrentPenetrateLevel ?? 0;

    // ========================================
    // レンズマスク制御（SpriteMask方式）
    // ========================================

    /// <summary>
    /// レンズマスクを初期化
    /// </summary>
    private void SetupLensMask()
    {
        if (_lensMaskController != null) return;

        var maskObj = new GameObject("LensMaskController");
        maskObj.transform.SetParent(_cameraRig.transform);
        maskObj.transform.localPosition = Vector3.zero;

        _lensMaskController = maskObj.AddComponent<LensMaskController>();
        _lensMaskController.Initialize(characterCamera);

        Debug.Log("[Presenter] LensMask initialized");
    }

    /// <summary>
    /// レンズ透視を開始（SpriteMaskモード）
    /// </summary>
    /// <param name="penetrateLevel">透視レベル</param>
    public void EnableLensMask(int penetrateLevel)
    {
        if (_lensMaskController == null)
        {
            SetupLensMask();
        }

        // LayerControllerにマスクモードを有効化
        if (_layerController != null)
        {
            _layerController.EnableMaskMode(penetrateLevel);
        }

        // マスクを表示
        _lensMaskController?.Show();

        Debug.Log($"[Presenter] LensMask enabled - Level: {penetrateLevel}");
    }

    /// <summary>
    /// レンズ透視を終了
    /// </summary>
    public void DisableLensMask()
    {
        // LayerControllerのマスクモードを無効化
        if (_layerController != null)
        {
            _layerController.DisableMaskMode();
        }

        // マスクを非表示
        _lensMaskController?.Hide();

        Debug.Log("[Presenter] LensMask disabled");
    }

    /// <summary>
    /// レンズ位置を更新（正規化座標 0-1）
    /// </summary>
    public void UpdateLensPosition(Vector2 normalizedPos)
    {
        _lensMaskController?.UpdatePositionFromNormalized(normalizedPos);
    }

    /// <summary>
    /// レンズの形状を設定
    /// </summary>
    public void SetLensShape(LensMaskController.LensShape shape)
    {
        _lensMaskController?.SetShape(shape);
    }

    /// <summary>
    /// レンズのサイズを設定
    /// </summary>
    public void SetLensSize(float size)
    {
        _lensMaskController?.SetSize(size);
    }

    /// <summary>
    /// レンズマスクがアクティブか
    /// </summary>
    public bool IsLensMaskActive => _lensMaskController?.IsActive ?? false;

    // ========================================
    // 互換性メソッド
    // ========================================

    /// <summary>
    /// 描画順を変更（RenderTexture方式では不要だが互換性のため）
    /// </summary>
    public void SetSortingOrder(int order)
    {
        // RenderTexture方式ではUIの描画順に従うため不要
    }

    /// <summary>
    /// 毎フレーム更新コールバック
    /// </summary>
    private Action<float> _onUpdateCallback;

    public void SetUpdateCallback(Action<float> callback)
    {
        _onUpdateCallback = callback;
    }

    public void ClearUpdateCallback()
    {
        _onUpdateCallback = null;
    }

    private void Update()
    {
        if (_isShowing && _onUpdateCallback != null)
        {
            _onUpdateCallback.Invoke(Time.deltaTime);
        }
    }

    // ========================================
    // クリーンアップ
    // ========================================

    private void Cleanup()
    {
        Hide();

        if (_currentInstance != null)
        {
            Destroy(_currentInstance);
            _currentInstance = null;
        }

        if (_cameraRig != null)
        {
            Destroy(_cameraRig);
            _cameraRig = null;
        }

        // 動的に作成したRenderTextureは解放
        if (renderTexture != null && !UnityEngine.Object.ReferenceEquals(renderTexture, null))
        {
            if (Application.isPlaying)
            {
                renderTexture.Release();
            }
        }
    }
}
