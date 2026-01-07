using UnityEngine;
using System.Collections;

public class WaterBalloonExplosion : MonoBehaviour
{
    [Header("爆発設定")]
    [SerializeField] private float flashDuration = 2f;
    [SerializeField] private float flashSpeed = 0.1f;
    [SerializeField] private float expandScale = 1.3f;
    [SerializeField] private float expandDuration = 0.3f;

    [Header("参照")]
    [SerializeField] private Renderer balloonRenderer;
    [SerializeField] private GameObject explosionEffect;
    [SerializeField] private Material flashMaterial; // ★点滅用の白いマテリアル

    [Header("エフェクト設定")]
    [SerializeField] private float explosionEffectScale = 1.5f; // エフェクトの大きさ
    [SerializeField] private float explosionEffectDuration = 3f; // エフェクトが消えるまでの時間

    [Header("スタン判定")]
    [SerializeField] private SphereCollider explosionCollider;
    [SerializeField] private StunType stunType = StunType.Heavy;

    private Color originalColor;
    private Vector3 originalScale;
    private bool isExploding = false;
    private Material materialInstance;
    private Material originalMaterial; // ★元のマテリアル保存用

    void Start()
    {
        if (balloonRenderer == null)
        {
            balloonRenderer = GetComponent<Renderer>();
            if (balloonRenderer == null)
            {
                balloonRenderer = GetComponentInChildren<Renderer>();
            }
        }

        if (balloonRenderer == null)
        {
            Debug.LogError("Rendererが見つかりません!");
            return;
        }

        // 元のマテリアルを保存
        originalMaterial = balloonRenderer.material;
        materialInstance = originalMaterial;

        try
        {
            if (materialInstance.HasProperty("_Color"))
            {
                originalColor = materialInstance.GetColor("_Color");
            }
            else
            {
                originalColor = Color.white;
            }
        }
        catch
        {
            originalColor = Color.white;
        }

        originalScale = transform.localScale;

        if (explosionCollider != null)
        {
            explosionCollider.enabled = false;
        }

        Invoke("StartExplosion", 0.5f);
    }

    public void StartExplosion()
    {
        if (!isExploding && materialInstance != null)
        {
            StartCoroutine(ExplosionSequence());
        }
    }

    private IEnumerator ExplosionSequence()
    {
        isExploding = true;
        yield return StartCoroutine(FlashWhite());
        yield return StartCoroutine(ExpandBalloon());
        Explode();
    }

    private IEnumerator FlashWhite()
    {
        float elapsed = 0f;
        bool isWhite = false;
        float nextToggle = 0f;

        while (elapsed < flashDuration)
        {
            // 一定間隔でマテリアルを切り替え
            if (elapsed >= nextToggle)
            {
                if (isWhite)
                {
                    // 元のマテリアルに戻す
                    balloonRenderer.material = originalMaterial;
                }
                else
                {
                    // 白いマテリアルに変更
                    if (flashMaterial != null)
                    {
                        balloonRenderer.material = flashMaterial;
                    }
                    else
                    {
                        Debug.LogWarning("Flash Materialが設定されていません!");
                    }
                }

                isWhite = !isWhite;
                nextToggle = elapsed + flashSpeed;
            }

            elapsed += Time.deltaTime;
            yield return null;
        }

        // 最後は白にする
        if (flashMaterial != null)
        {
            balloonRenderer.material = flashMaterial;
        }
    }

    private IEnumerator ExpandBalloon()
    {
        float elapsed = 0f;
        Vector3 targetScale = originalScale * expandScale;

        while (elapsed < expandDuration)
        {
            transform.localScale = Vector3.Lerp(originalScale, targetScale,
                elapsed / expandDuration);

            elapsed += Time.deltaTime;
            yield return null;
        }

        transform.localScale = targetScale;
    }

    private void Explode()
    {
        if (explosionCollider != null)
        {
            explosionCollider.enabled = true;
        }

        if (explosionEffect != null)
        {
            GameObject effect = Instantiate(explosionEffect, transform.position, Quaternion.identity);

            // エフェクトのサイズを変更
            effect.transform.localScale = Vector3.one * explosionEffectScale;

            // 一定時間後にエフェクトを削除
            Destroy(effect, explosionEffectDuration);
        }

        // 少し待ってから削除（Trigger判定のため）
        Destroy(transform.parent.gameObject, 0.2f);
    }

    // ----------------------------
    // Player 判定
    // ----------------------------
    private void OnTriggerEnter(Collider other)
    {
        if (!isExploding) return;

        // ヒットしたのがPlayerだった時
        if (other.gameObject.CompareTag("Player"))
        {
            var car = other.gameObject.GetComponentInParent<CarController>();

            if (car != null)
            {
                // ヒットしたPlayerに中程度のスタン状態を設定
                car.SetStun(stunType);

                Debug.Log("[Balloon]:player stuned");
            }
        }
    }


    void OnDestroy()
    {
        if (materialInstance != null && materialInstance != originalMaterial)
        {
            Destroy(materialInstance);
        }
    }
}