using UnityEngine;

/// <summary>
/// GameControllerからの依頼を受けて、指定座標にFloatingTextを生成する工場。
/// </summary>
public class FloatingTextManager : MonoBehaviour
{
    public static FloatingTextManager Instance;

    [Header("設定")]
    public GameObject textPrefab; // ここにプレハブを入れる
    public Transform container;   // 生成したテキストを入れる親（Canvas内の整理用）

    void Awake()
    {
        if (Instance == null) Instance = this;
    }

    /// <summary>
    /// 指定した位置にテキストを出す
    /// </summary>
    public void Spawn(double amount, Vector3 screenPosition, bool isCritical)
    {
        if (textPrefab == null)
        {
            Debug.LogWarning("[FloatingTextManager] textPrefab is not assigned!");
            return;
        }

        // 生成！
        // UIなので、親(container)を指定して生成するのが大事
        GameObject obj = Instantiate(textPrefab, screenPosition, Quaternion.identity, container);
        
        // データを流し込む
        FloatingTextController controller = obj.GetComponent<FloatingTextController>();
        if (controller != null)
        {
            controller.Setup(amount, isCritical);
        }
    }
}