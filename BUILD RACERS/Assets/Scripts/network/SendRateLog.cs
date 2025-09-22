using UnityEngine;
using Photon.Pun;

// Attach to any active GameObject
public class PhotonSendRateMonitor : MonoBehaviour
{
    int prevOpCount = 0;
    int prevPacketBytes = 0;
    float sampleTimer = 0f;
    public float sampleInterval = 1f; // 1秒ごとにサンプリング

    // Optional thresholds for adaptive control:
    public int bytesPerSecondThreshold = 10000; // 好みに合わせて調整
    public int minSendRate = 5;
    public int maxSendRate = 60;

    public int serializationRate = 10;

    void Start()
    {
        //同期フレームの設定　デフォルト10
        PhotonNetwork.SerializationRate = serializationRate;

        // Traffic stats を有効にする（接続済みで peer が存在する前提）
        var client = PhotonNetwork.NetworkingClient;
        if (client != null && client.LoadBalancingPeer != null)
        {
            client.LoadBalancingPeer.TrafficStatsEnabled = true;
            Debug.Log("Photon TrafficStats enabled");
        }
    }

    void Update()
    {
        sampleTimer += Time.deltaTime;
        if (sampleTimer < sampleInterval) return;
        sampleTimer = 0f;

        var client = PhotonNetwork.NetworkingClient;
        if (client == null || client.LoadBalancingPeer == null) return;

        var peer = client.LoadBalancingPeer;
        var gameLevel = peer.TrafficStatsGameLevel;    // OperationCount など
        var outgoing = peer.TrafficStatsOutgoing;     // TotalPacketBytes 等

        // 実測値（差分）を計算
        int opCount = gameLevel != null ? gameLevel.OperationCount : 0;
        int opDelta = opCount - prevOpCount;
        prevOpCount = opCount;

        int packetBytes = outgoing != null ? outgoing.TotalPacketBytes : 0;
        int bytesDelta = packetBytes - prevPacketBytes;
        prevPacketBytes = packetBytes;

        // ログ出力
        Debug.Log($"[PhotonStats] ops/s ≈ {opDelta}/{sampleInterval}s, bytes/s ≈ {bytesDelta}/{sampleInterval}s, SendRate={PhotonNetwork.SendRate}, SerializationRate={PhotonNetwork.SerializationRate}");

        // 簡易アダプティブ制御の例（任意）
        if (bytesDelta > bytesPerSecondThreshold)
        {
            int newRate = Mathf.Max(minSendRate, PhotonNetwork.SendRate - 5);
            if (newRate != PhotonNetwork.SendRate)
            {
                PhotonNetwork.SendRate = newRate;
                PhotonNetwork.SerializationRate = Mathf.Min(PhotonNetwork.SerializationRate, newRate); // 両方揃える
                Debug.Log($"[PhotonStats] High traffic detected. Lowering SendRate -> {newRate}");
            }
        }
        else
        {
            // 十分余裕があればレートを徐々に戻す（安全策：ゆっくり）
            if (PhotonNetwork.SendRate < maxSendRate)
            {
                PhotonNetwork.SendRate = Mathf.Min(maxSendRate, PhotonNetwork.SendRate + 1);
                PhotonNetwork.SerializationRate = Mathf.Min(PhotonNetwork.SerializationRate, PhotonNetwork.SendRate);
            }
        }

        // 追加チェック: 送信が滞っているかを見る指標
        int longestDeltaMs = gameLevel != null ? gameLevel.LongestDeltaBetweenSending : 0;
        if (longestDeltaMs > 200) // 例: 200ms を超えたらローカルで送信が滞っている可能性
            Debug.LogWarning($"LongestDeltaBetweenSending = {longestDeltaMs} ms (注: SendOutgoingCommands 呼び出しが滞っているかも).");
    }
}
