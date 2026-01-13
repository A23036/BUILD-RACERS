using UnityEngine;
using Photon.Pun;

public class StaticGimmick : MonoBehaviourPun
{
    private enum State
    {
        Rising,
        Alive,
        Disappearing
    }

    [Header("出現設定")]
    [SerializeField] private float spawnOffsetY = -2f;
    [SerializeField] private float riseDuration = 0.5f;

    [Header("消滅設定")]
    [SerializeField] private float lifeTime = 3f;
    [SerializeField] private GameObject destroyEffectPrefab;

    private State state;
    private Vector3 startPos;
    private Vector3 goalPos;
    private float timer;

    void Start()
    {
        goalPos = transform.position;
        startPos = goalPos + Vector3.up * spawnOffsetY;
        transform.position = startPos;

        state = State.Rising;
        timer = 0f;
    }

    void Update()
    {
        switch (state)
        {
            case State.Rising:
                UpdateRising();
                break;

            case State.Alive:
                UpdateAlive();
                break;

            case State.Disappearing:
                UpdateDisappearing();
                break;
        }
    }

    // ----------------------------
    // 上昇処理
    // ----------------------------
    private void UpdateRising()
    {
        timer += Time.deltaTime;
        float t = Mathf.Clamp01(timer / riseDuration);

        // EaseOut
        t = 1f - Mathf.Pow(1f - t, 2f);

        transform.position = Vector3.Lerp(startPos, goalPos, t);

        if (t >= 1f)
        {
            transform.position = goalPos;
            timer = 0f;
            state = State.Alive;
        }
    }

    // ----------------------------
    // 停止中
    // ----------------------------
    private void UpdateAlive()
    {
        timer += Time.deltaTime;

        if (timer >= lifeTime)
        {
            timer = 0f;
            state = State.Disappearing;
            SpawnDestroyEffect();
        }
    }

    // ----------------------------
    // 消滅
    // ----------------------------
    private void UpdateDisappearing()
    {
        DestroySelf();
    }

    // ----------------------------
    // エフェクト生成（ローカルのみ・子にする）
    // ----------------------------
    private void SpawnDestroyEffect()
    {
        if (destroyEffectPrefab == null) return;

        GameObject effectObj = Instantiate(
            destroyEffectPrefab,
            transform.position,
            Quaternion.identity,
            transform   // ★ 親にする
        );

        ParticleSystem ps = effectObj.GetComponent<ParticleSystem>();
        if (ps != null)
        {
            float duration =
                ps.main.duration + ps.main.startLifetime.constantMax;
            Destroy(effectObj, duration);
        }
        else
        {
            Destroy(effectObj, 3f);
        }
    }

    // ----------------------------
    // 自身削除（Photon対応）
    // ----------------------------
    private void DestroySelf()
    {
        if (PhotonNetwork.IsConnected)
        {
             PhotonNetwork.Destroy(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }
}
