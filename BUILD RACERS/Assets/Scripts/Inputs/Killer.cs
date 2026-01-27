using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// AIDriver ウェイポイントを巡回するAI
/// </summary>
[RequireComponent(typeof(Rigidbody))]
public class Killer : AIDriver, IDriver
{
    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        tf = transform;
    }

    private void Start()
    {
        isKiller = true;

        RefreshWaypoints();
        if (waypoints.Count == 0)
        {
            Debug.LogError("[AIDriver] Waypointが設定されていません。");
            return;
        }

        //パラメータをキラー仕様に
        maxSteerAngle = 90f;
        targetMaxSpeedKmh *= 2;

        MaxWpRadius = 8f;

        targetSpeedMps = targetMaxSpeedKmh / 3.6f;

        noiseAmount = 0f;
    }

    public void SetCurrentIdx(int idx)
    {
        currentIndex = idx;

        var wpcTransform = GameObject.Find("Waypoints").transform;
        Debug.Log($"Killer StartIdx : {wpcTransform.GetChild(idx)}");

        //空飛ばないように車体を進行方向に設定
        Vector3 targetVec = waypoints[currentIndex].position - tf.position;
        transform.rotation = Quaternion.LookRotation(targetVec.normalized, Vector3.up);
    }

    public void GetInputs(out float throttle, out float brake, out float steer)
    {
        throttle = brake = steer = 0f;
        if (waypoints.Count == 0)
            return;

        Transform curr = waypoints[currentIndex];

        //目的地点が近づくほど、到着判定を広くする
        int preIdx = currentIndex - 1;
        if (preIdx < 0) preIdx = waypoints.Count - 1;
        float betDist = (waypoints[preIdx].position - curr.position).magnitude;
        float nowDist = (tf.position - curr.position).magnitude;
        float rate = 1f - nowDist / betDist;
        brake = rate * rate * 2;
        float waypointRadius = Mathf.Lerp(MinWpRadius, MaxWpRadius, rate);

        // --- 到達判定（進行方向ベース） ---
        Vector3 toWp = curr.position - tf.position;
        float dist = toWp.magnitude;
        float forwardDot = Vector3.Dot(tf.forward, toWp.normalized);

        RaycastHit[] hits = Physics.RaycastAll(tf.position + Vector3.up * 0.5f, toWp.normalized, dist);
        bool isHitWall = false;
        foreach (var hit in hits)
        {
            if (hit.collider.tag == "Wall")
            {
                // 壁に当たるなら一つ前のウェイポイントへ戻る
                isHitWall = true;
                currentIndex--;
                if(currentIndex < 0) currentIndex = waypoints.Count - 1;
                break;
            }
        }

        // 近ければ次へ
        if (dist < waypointRadius && isHitWall == false)
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

        // --- スムージング ---
        float alpha = Mathf.Clamp01(Time.fixedDeltaTime / Mathf.Max(reactionTime, 1e-5f));
        steer = Mathf.Lerp(lastSteer, rawSteer, alpha);
        throttle = Mathf.Lerp(lastThrottle, desiredThrottle, alpha);
        
        brake = Mathf.Lerp(lastBrake, desiredBrake, alpha);

        //挙動をキラー仕様にする
        float killerSteerMagni = 0.7f;
        float killerMagni = 1.5f;

        if (steer < 0) killerSteerMagni *= -1;

        if(Mathf.Abs(steer) > 0.3f) lastSteer = steer + killerSteerMagni;
        else lastSteer = steer;
        lastThrottle = throttle * killerMagni;
        lastBrake = brake * killerMagni;

        var cc = GetComponent<CarController>();
        if (cc.isMine) Debug.Log($"[KILLER] : T={lastThrottle:F2}, B={lastBrake:F2}, S={lastSteer:F2}");
    }

    // --- ウェイポイント移行 ---
    private void AdvanceWaypoint()
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

    public bool ItemUseDecision()
    {
        return false;
    }

    public bool IsKiller()
    {
        Debug.Log("IsKiller called : " + isKiller);
        return isKiller;
    }
}
