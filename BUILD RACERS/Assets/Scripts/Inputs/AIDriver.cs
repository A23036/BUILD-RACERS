using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// AIDriver 改良版（ウェイポイント到達時の不安定挙動を改善）
///
/// - 到達判定を「進行方向による通過ベース」に変更
/// - ステアD項を弱めて安定化
/// - 速度に応じてルックアヘッド距離を可変
/// - 直線〜コーナー遷移が滑らか
/// </summary>
[RequireComponent(typeof(Rigidbody))]
public class AIDriver : MonoBehaviour, IDriver
{
    [Header("参照")]
    [SerializeField] private WaypointContainer waypointContainer = null;
    [SerializeField] private int startIndex = 0;

    [Header("ウェイポイント設定")]
    [SerializeField] private float MaxWpRadius = 12f;   // 目的地の最大半径
    [SerializeField] private float MinWpRadius = 4f;    // 目的地の最小半径
    [SerializeField] private bool loopPath = true;

    [Header("ステアリング調整")]
    [SerializeField] private float steerP = 2.0f;
    [SerializeField] private float steerD = 0.1f;         // 小さくしてオーバー反応を防ぐ
    [SerializeField] private float maxSteerAngle = 45f;

    [Header("速度制御")]
    [SerializeField] private float targetMaxSpeedKmh = 73f;
    [SerializeField] private float cornerMinSpeedKmh = 16f;

    [Header("挙動調整")]
    [SerializeField] private float reactionTime = 0.05f;
    [SerializeField] private float noiseAmount = 1f;

    private Rigidbody rb;
    private Transform tf;
    private List<Transform> waypoints = new List<Transform>();
    private int currentIndex = 0;

    private float lastError = 0f;
    private float lastSteer = 0f;
    private float lastThrottle = 0f;
    private float lastBrake = 0f;

    private float targetSpeedMps;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        tf = transform;

        //入力ノイズをランダムで設定
        noiseAmount = Random.Range(0.5f,2.5f);
    }

    private void Start()
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
        // 前方にあり、かつ近ければ次へ
        if (dist < waypointRadius/* && forwardDot > 0.0f*/)
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
        brake = Mathf.Lerp(lastBrake, desiredBrake, alpha);

        lastSteer = steer;
        lastThrottle = throttle;
        lastBrake = brake;
    }

    // --- ウェイポイント移行 ---
    private void AdvanceWaypoint()
    {
        if (waypoints.Count == 0) return;
        currentIndex++;
        if (currentIndex >= waypoints.Count)
            currentIndex = loopPath ? 0 : waypoints.Count - 1;
    }

    // --- ルックアヘッド点計算 ---
    private Vector3 GetLookAheadPoint(float lookDist)
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

    private void RefreshWaypoints()
    {
        waypoints.Clear();
        if (waypointContainer != null)
        {
            foreach (var wp in waypointContainer.Waypoints)
                if (wp != null) waypoints.Add(wp);
        }
    }

    private void RefreshWaypointsIfChanged()
    {
        if (waypointContainer == null) return;
        if (waypoints.Count != waypointContainer.Waypoints.Count)
        {
            RefreshWaypoints();
            return;
        }
        for (int i = 0; i < waypoints.Count; i++)
        {
            if (waypoints[i] != waypointContainer.Waypoints[i])
            {
                RefreshWaypoints();
                return;
            }
        }
    }

    public void SetWaypointContainer(WaypointContainer container)
    {
        waypointContainer = container;
        RefreshWaypoints();
        currentIndex = Mathf.Clamp(startIndex, 0, Mathf.Max(0, waypoints.Count - 1));
    }
}
