using UnityEngine;
using TMPro;

/// <summary>
/// 噴水のように飛び散る演出に改造したFloating Text。
/// 出現位置の固定オフセット機能を追加しました。
/// </summary>
public class FloatingTextController : MonoBehaviour
{
    [Header("UI参照")]
    public TextMeshProUGUI damageText;

    [Header("動きの設定 (Physics風)")]
    public float lifeTime = 0.8f;      
    public Vector2 minVelocity = new Vector2(-100f, 100f); 
    public Vector2 maxVelocity = new Vector2(100f, 300f);  
    public float gravity = 500f;       

    [Header("見た目の設定")]
    [Tooltip("クリック位置からどれくらいずらして出現させるか (Yを増やすと上にでる)")]
    public Vector3 spawnOffset = new Vector3(0, 50, 0); // ■ 追加: デフォルトでY+50 (少し上)

    [Tooltip("出現位置のランダムなバラつき")]
    public Vector3 randomOffsetRange = new Vector3(50, 50, 0); 
    
    public Color normalColor = Color.white;
    public Color criticalColor = new Color(1f, 0.2f, 0.2f);
    public float criticalScale = 1.5f;

    // 内部変数
    private Vector2 _currentVelocity;

    void Awake()
    {
        if (damageText == null) damageText = GetComponent<TextMeshProUGUI>();
    }

    public void Setup(double amount, bool isCritical)
    {
        if (lifeTime <= 0) lifeTime = 0.1f;

        // 1. テキスト設定
        damageText.text = "+" + amount.ToString("N0");
        if (isCritical) damageText.text += "!";

        // 2. 色と大きさ
        if (isCritical)
        {
            damageText.color = criticalColor;
            transform.localScale = Vector3.one * criticalScale;
        }
        else
        {
            damageText.color = normalColor;
            transform.localScale = Vector3.one;
        }

        // 3. ■ 位置調整
        // まず、固定オフセットを足す（クリックした場所より少し上にする）
        transform.position += spawnOffset;

        // 次に、ランダムに散らす
        float offsetX = Random.Range(-randomOffsetRange.x, randomOffsetRange.x);
        float offsetY = Random.Range(-randomOffsetRange.y, randomOffsetRange.y);
        transform.position += new Vector3(offsetX, offsetY, 0);

        // 4. ランダムな初速
        float velX = Random.Range(minVelocity.x, maxVelocity.x);
        float velY = Random.Range(minVelocity.y, maxVelocity.y);
        _currentVelocity = new Vector2(velX, velY);

        // 5. 削除予約
        Destroy(gameObject, lifeTime);
    }

    void Update()
    {
        // 物理演算っぽい動き
        _currentVelocity.y -= gravity * Time.deltaTime;
        transform.Translate(_currentVelocity * Time.deltaTime);

        // フェードアウト
        if (damageText != null && lifeTime > 0)
        {
            float alpha = damageText.color.a;
            alpha -= Time.deltaTime / (lifeTime * 0.8f); 
            
            Color newColor = damageText.color;
            newColor.a = Mathf.Clamp01(alpha);
            damageText.color = newColor;
        }
    }
}