using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// AIDriver ウェイポイントを巡回するAI
/// </summary>
[RequireComponent(typeof(Rigidbody))]
public class AIDriver : MonoBehaviour, IDriver
{
    [Header("参照")]
    [SerializeField] protected WaypointContainer waypointContainer = null;
    [SerializeField] protected int startIndex = 0;

    [Header("ウェイポイント設定")]
    [SerializeField] protected float MaxWpRadius = 12f;   // 目的地の最大半径
    [SerializeField] protected float MinWpRadius = 4f;    // 目的地の最小半径
    [SerializeField] protected bool loopPath = true;

    [Header("ステアリング調整")]
    [SerializeField] protected float steerP = 2.0f;
    [SerializeField] protected float steerD = 0.1f;         // 小さくしてオーバー反応を防ぐ
    [SerializeField] protected float maxSteerAngle = 45f;

    [Header("速度制御")]
    [SerializeField] protected float targetMaxSpeedKmh = 73f;
    [SerializeField] protected float cornerMinSpeedKmh = 16f;

    [Header("挙動調整")]
    [SerializeField] protected float reactionTime = 0.05f;
    [SerializeField] protected float noiseAmount = 1f;

    protected Rigidbody rb;
    protected Transform tf;
    protected List<Transform> waypoints = new List<Transform>();
    protected int currentIndex = 0;

    protected float lastError = 0f;
    protected float lastSteer = 0f;
    protected float lastThrottle = 0f;
    protected float lastBrake = 0f;

    protected float targetSpeedMps;

    protected bool isKiller = false;

    //デバッグ用
    float minSteer = 1e6f;
    float maxSteer = 0f;

    protected void Awake()
    {
        rb = GetComponent<Rigidbody>();
        tf = transform;

        //入力ノイズをランダムで設定
        noiseAmount = Random.Range(0.5f,2.5f);
    }

    protected void Start()
    {
        RefreshWaypoints();
        if (waypoints.Count == 0)
        {
            Debug.LogError("[AIDriver] Waypointが設定されていません。");
            return;
        }

        currentIndex = Mathf.Clamp(startIndex, 0, waypoints.Count - 1);
        targetSpeedMps = targetMaxSpeedKmh / 3.6f;
    }

    public void GetInputs(out float throttle, out float brake, out float steer)
    {
        throttle = brake = steer = 0f;
        if (waypoints.Count == 0)
            return;

        Transform curr = waypoints[currentIndex];

        //目的地点が近づくほど、到着判定を広くする
        int preIdx = currentIndex - 1;
        if(preIdx < 0) preIdx = waypoints.Count - 1;
        float betDist = (waypoints[preIdx].position - curr.position).magnitude;
        float nowDist = (tf.position - curr.position).magnitude;
        float rate = 1f -   nowDist / betDist;
        float waypointRadius = Mathf.Lerp(MinWpRadius, MaxWpRadius,rate);

        // --- 到達判定（進行方向ベース） ---
        Vector3 toWp = curr.position - tf.position;
        float dist = toWp.magnitude;
        float forwardDot = Vector3.Dot(tf.forward, toWp.normalized);
        // 近ければ次へ
        if (dist < waypointRadius)
        {
            AdvanceWaypoint();
            curr = waypoints[currentIndex];
        }

        // --- 速度に応じたルックアヘッド距離 ---
        float speed = rb.linearVelocity.magnitude;
        float dynamicLookAhead = Mathf.Lerp(6f, 12f, Mathf.InverseLerp(0f, 10f, speed));

        // --- ルックアヘッド点 ---
        Vector3 target = GetLookAheadPoint(dynamicLookAhead);
        Vector3 toTarget = target - tf.position;
        Vector3 localDir = tf.InverseTransformDirection(toTarget.normalized);

        // --- ステア角計算 ---
        float desiredAngleDeg = Mathf.Atan2(localDir.x, localDir.z) * Mathf.Rad2Deg;
        float error = desiredAngleDeg;
        float d = (error - lastError) / Mathf.Max(Time.fixedDeltaTime, 1e-5f);
        float steerCmd = steerP * error + steerD * d;
        lastError = error;
        float rawSteer = Mathf.Clamp(steerCmd / maxSteerAngle, -1f, 1f);

        // --- 速度制御 ---
        float angleAbs = Mathf.Abs(desiredAngleDeg);
        float speedFactor = Mathf.InverseLerp(70f, 0f, angleAbs);
        float targetKmh = Mathf.Lerp(cornerMinSpeedKmh, targetMaxSpeedKmh, speedFactor);
        targetSpeedMps = targetKmh / 3.6f;

        float speedDiff = targetSpeedMps - speed;
        float desiredThrottle = (speedDiff > 0.1f) ? 1.0f : 0.4f;
        float desiredBrake = (speedDiff < -0.2f) ? 0.2f : 0f;

        // --- ノイズ追加 ---　アクセルのみノイズを適用
        rawSteer += Random.Range(-noiseAmount, noiseAmount);
        //desiredThrottle += Random.Range(-noiseAmount, noiseAmount);

        // --- スムージング ---
        float alpha = Mathf.Clamp01(Time.fixedDeltaTime / Mathf.Max(reactionTime, 1e-5f));
        steer = Mathf.Lerp(lastSteer, rawSteer, alpha);
        throttle = Mathf.Lerp(lastThrottle, desiredThrottle, alpha);
        throttle = 1;
        brake = Mathf.Lerp(lastBrake, desiredBrake, alpha);

        minSteer = Mathf.Min(minSteer, steer);
        maxSteer = Mathf.Max(maxSteer, steer);

        lastSteer = steer;
        lastThrottle = throttle;
        lastBrake = brake;

        var cc = GetComponent<CarController>();
        if(cc.isMine) Debug.Log($"[AIDriver] Throttle: {throttle:F2}, Brake: {brake:F2}, Steer: {steer:F2}");
        Debug.Log($"[AIDriver] Throttle: {throttle:F2}, Brake: {brake:F2}, Steer: {steer:F2}");
        Debug.Log($"[AIDriver] Steer Range: Min={minSteer:F2}, Max={maxSteer:F2}");
    }

    // --- ウェイポイント移行 ---
    protected void AdvanceWaypoint()
    {
        if (waypoints.Count == 0) return;
        currentIndex++;
        if (currentIndex >= waypoints.Count)
            currentIndex = loopPath ? 0 : waypoints.Count - 1;

        //分岐なら分岐処理
        if (waypoints[currentIndex].transform.gameObject.tag == "branch")
        {
            //仮で子オブジェクトからランダムに決定
            int childCount = waypoints[currentIndex].transform.childCount;
            int rnd = Random.Range(0, childCount);
            waypoints[currentIndex] = waypoints[currentIndex].transform.GetChild(rnd);

            Debug.Log("DECIDE BRANCH : " + (rnd + 1));
        }
    }

    // --- ルックアヘッド点計算 ---
    protected Vector3 GetLookAheadPoint(float lookDist)
    {
        if (waypoints.Count == 0)
            return tf.position;

        int searchIdx = currentIndex;
        Vector3 last = tf.position;
        Vector3 next = waypoints[searchIdx].position;
        float remaining = lookDist;

        while (true)
        {
            float segLen = Vector3.Distance(last, next);
            if (segLen >= remaining)
                return last + (next - last).normalized * remaining;

            remaining -= segLen;
            last = next;

            if (++searchIdx >= waypoints.Count)
            {
                if (loopPath) searchIdx = 0;
                else return waypoints[waypoints.Count - 1].position;
            }
            next = waypoints[searchIdx].position;
        }
    }

    protected void RefreshWaypoints()
    {
        waypoints.Clear();
        if (waypointContainer != null)
        {
            foreach (var wp in waypointContainer.Waypoints)
                if (wp != null) waypoints.Add(wp);
        }
    }

    public void SetWaypointContainer(WaypointContainer container)
    {
        waypointContainer = container;
        RefreshWaypoints();
        currentIndex = Mathf.Clamp(startIndex, 0, Mathf.Max(0, waypoints.Count - 1));
    }

    public bool ItemUseDecision()
    {
        return true;
    }

    public bool IsKiller()
    {
        return isKiller;
    }
}
