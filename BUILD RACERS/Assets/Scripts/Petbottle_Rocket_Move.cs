using UnityEngine;

public class RocketMove : MonoBehaviour
{
    // Inspectorで速度を設定できるように public にします
    public float speed = 20f;

    // ロケットが発射されてから自動的に消滅するまでの時間
    public float lifeTime = 5f;

    // ★ 新しく追加 ★
    [Header("Reflect Settings")]
    [Tooltip("ロケットが反射できる最大回数")]
    public int maxReflectCount = 3;

    private int currentReflectCount;
    private Vector3 currentDirection; // 現在の移動方向を格納する変数

    void Start()
    {
        // 破壊タイマーを開始
        Destroy(gameObject, lifeTime);

        // 初期の反射回数を設定
        currentReflectCount = maxReflectCount;

        // 初期の移動方向をローカルの上方向（Y軸）に設定
        currentDirection = transform.up;
    }

    void Update()
    {
        // 毎フレーム、currentDirectionに基づいて移動します
        transform.Translate(currentDirection * speed * Time.deltaTime, Space.World);

        // ★ 修正: 水平姿勢を維持しつつ、進行方向を向く ★
        if (currentDirection != Vector3.zero)
        {
            // 進行方向（currentDirection）を前方（Z軸）に、
            // ワールドの真上（Vector3.up）をロケットの上（Y軸）に指定します。
            transform.rotation = Quaternion.LookRotation(currentDirection, Vector3.up);
        }
    }

    // ★ 衝突処理 ★
    void OnCollisionEnter(Collision collision)
    {
        // 反射回数が残っているか確認
        if (currentReflectCount > 0)
        {
            Vector3 incomingVector = currentDirection;
            Vector3 surfaceNormal = collision.contacts[0].normal;

            // 新しい方向を計算
            currentDirection = Vector3.Reflect(incomingVector, surfaceNormal).normalized;

            // ★ 修正: 水平姿勢を維持しつつ、新しい進行方向を向く ★
            // 衝突時に即座に回転を更新
            transform.rotation = Quaternion.LookRotation(currentDirection, Vector3.up);

            // 反射回数を減らす
            currentReflectCount--;
        }
        else
        {
            // 反射回数が残っていない場合、ロケットを破壊する
            Destroy(gameObject);
        }
    }
}