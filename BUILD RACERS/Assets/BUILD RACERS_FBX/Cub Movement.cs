using UnityEngine;

public class FloatingObject : MonoBehaviour
{
    [Header("浮遊設定")]
    [Tooltip("上下に動く距離")]
    public float amplitude = 0.5f;

    [Tooltip("浮遊の速度")]
    public float frequency = 1f;

    [Tooltip("浮遊開始までの最大待機時間（秒）")]
    public float maxStartDelay = 3f;

    private Vector3 startPos;
    private float startTime;
    private float delayTime;

    void Awake()
    {
        // 初期位置を記録
        startPos = transform.position;

        // ランダムな待機時間を設定
        delayTime = Random.Range(0f, maxStartDelay);
        startTime = Time.time;
    }

    void Update()
    {
        // 待機時間が経過していない場合は何もしない
        float elapsedTime = Time.time - startTime;
        if (elapsedTime < delayTime)
        {
            return;
        }

        // Sin波を使って上方向のみに浮遊（0〜1の範囲に変換）
        float animationTime = elapsedTime - delayTime;
        float wave = (Mathf.Sin(animationTime * frequency) + 1f) / 2f;
        float newY = startPos.y + wave * amplitude;
        transform.position = new Vector3(startPos.x, newY, startPos.z);
    }
}