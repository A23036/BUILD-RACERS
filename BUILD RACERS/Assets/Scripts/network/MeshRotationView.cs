using Photon.Pun;
using UnityEngine;
using UnityEngine.InputSystem;

public class MeshRotationView : MonoBehaviourPunCallbacks, IPunObservable
{
    [Tooltip("補間バッファ時間（ミリ秒）- ネットワークジッター対策　回線良ければ低く、ゴミなら高く")]
    [SerializeField] private float interpolationBackTime = 100f;

    [Tooltip("補間バッファ時間の自動最適化")]
    [SerializeField] private bool autoOptBackTime = true;
    [Tooltip("最適化　最低、最大時間")]
    [SerializeField] private float minBackTime = 10f;
    [SerializeField] private float maxBackTime = 2000f;

    //ジッター測定用
    private float[] receiveDeltaTimes = new float[10];
    private int receiveIndex = 0;
    private double lastReceiveTime = 0;

    // 同期フレーム
    [SerializeField] private int serializationRate = 30;
    
    // 補間用の状態バッファ
    private struct State
    {
        public Vector3 position;
        public Quaternion rotation;
        public double timestamp;
    }
    
    private State[] stateBuffer = new State[20];
    private int stateCount = 0;
    
    // フォールバック用（バッファが足りない時）
    private Vector3 lastPosition;
    private Quaternion lastRotation;

    [Tooltip("同期メッシュオブジェクト")]
    [SerializeField] private GameObject bodyMesh;

    void Awake()
    {
        bodyMesh = transform.Find("BodyMesh").gameObject;
    }

    void Start()
    {
        PhotonNetwork.SendRate = serializationRate;
        PhotonNetwork.SerializationRate = serializationRate;
        
        lastRotation = bodyMesh.transform.rotation;
    }
    
    void Update()
    {
        if (!PhotonNetwork.IsConnected || photonView.IsMine) return;
        
        // 現在のレンダリング時刻（少し過去）
        double renderTime = PhotonNetwork.Time - interpolationBackTime / 1000.0;
        
        //補間時間の自動最適化
        if(autoOptBackTime)
        {
            AdjustInteroplationBackTime();
        }

        //線形補完
        InterpolatePosition(renderTime);
    }

    private void AdjustInteroplationBackTime()
    {
        // 受信間隔のジッター（ばらつき）を計算
        if (receiveIndex < receiveDeltaTimes.Length - 1) return; // データが溜まるまで待つ

        float sum = 0f;
        float max = 0f;
        float min = float.MaxValue;

        for (int i = 0; i < receiveDeltaTimes.Length; i++)
        {
            float dt = receiveDeltaTimes[i];
            if (dt > 0)
            {
                sum += dt;
                max = Mathf.Max(max, dt);
                min = Mathf.Min(min, dt);
            }
        }

        float avg = (max + min) / 2;
        float jitter = max - min; // ジッター（最大と最小の差）
        float maxLate = max;

        // ジッターが大きいほど、バッファ時間を増やす
        // 平均受信間隔 + ジッターの2倍 を目安にする 計算方法模索中
        float targetBackTime = ((avg + jitter) * 10f) * 1000f; // 秒→ミリ秒

        // 滑らかに調整（急激に変化させない）
        float adjustSpeed = 50f * Time.deltaTime; // 1秒あたり50ms変化
        interpolationBackTime = Mathf.MoveTowards(
            interpolationBackTime,
            Mathf.Clamp(targetBackTime, minBackTime, maxBackTime),
            adjustSpeed
        );

        // デバッグ表示（必要に応じて）
        Debug.Log($"Jitter: {jitter*1000:F1}ms, BackTime: {interpolationBackTime:F1}ms , targetBackTime: {targetBackTime:F1}");
    }

    private void InterpolatePosition(double renderTime)
    {
        // バッファから適切な2つの状態を見つけて補間
        if (stateCount < 2) return;
        
        // renderTimeを挟む2つの状態を探す
        State from = stateBuffer[0];
        State to = stateBuffer[0];
        
        for (int i = 0; i < stateCount; i++)
        {
            if (stateBuffer[i].timestamp <= renderTime)
            {
                from = stateBuffer[i];
            }
            if (stateBuffer[i].timestamp >= renderTime)
            {
                to = stateBuffer[i];
                break;
            }
        }
        
        // 同じ状態の場合はそのまま使用
        if (from.timestamp == to.timestamp)
        {
            bodyMesh.transform.rotation = to.rotation;
            return;
        }
        
        // 補間率を計算
        double length = to.timestamp - from.timestamp;
        float t = (length > 0) ? (float)((renderTime - from.timestamp) / length) : 0f;
        t = Mathf.Clamp01(t);

        // 補間実行
        bodyMesh.transform.rotation = Quaternion.Slerp(from.rotation, to.rotation, t);
    }
    
    void IPunObservable.OnPhotonSerializeView(PhotonStream stream, PhotonMessageInfo info)
    {
        if (!PhotonNetwork.IsConnected) return;
        
        if (photonView.IsMine)
        {
            // 自分の状態を送信
            stream.SendNext(bodyMesh.transform.rotation);
        }
        else
        {
            // 他プレイヤーの状態を受信
            Quaternion rot = (Quaternion)stream.ReceiveNext();
            
            // タイムスタンプ付きでバッファに追加
            State newState = new State
            {
                rotation = rot,
                timestamp = info.SentServerTime
            };
            
            AddState(newState);
            
            lastRotation = rot;
        }

        //ジッターを測定
        if (lastReceiveTime > 0)
        {
            float deltaTime = (float)(info.SentServerTime - lastReceiveTime);
            receiveDeltaTimes[receiveIndex] = deltaTime;
            receiveIndex = (receiveIndex + 1) % receiveDeltaTimes.Length;
        }
        lastReceiveTime = info.SentServerTime;
    }
    
    private void AddState(State state)
    {
        // バッファに状態を追加（古い状態は削除）
        if (stateCount >= stateBuffer.Length)
        {
            // バッファがいっぱいなら最古のデータを削除
            for (int i = 1; i < stateBuffer.Length; i++)
            {
                stateBuffer[i - 1] = stateBuffer[i];
            }
            stateCount = stateBuffer.Length - 1;
        }
        
        stateBuffer[stateCount] = state;
        stateCount++;
        
        // 古すぎる状態を削除（1秒以上前）
        double currentTime = PhotonNetwork.Time;
        int validStartIndex = 0;
        for (int i = 0; i < stateCount; i++)
        {
            if (currentTime - stateBuffer[i].timestamp > 1.0)
            {
                validStartIndex = i + 1;
            }
            else
            {
                break;
            }
        }
        
        if (validStartIndex > 0)
        {
            for (int i = validStartIndex; i < stateCount; i++)
            {
                stateBuffer[i - validStartIndex] = stateBuffer[i];
            }
            stateCount -= validStartIndex;
        }
    }
}