using Photon.Pun;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerTransformView : MonoBehaviourPunCallbacks, IPunObservable
{
    [SerializeField] private bool useInterpolation = true;
    [SerializeField] private bool useExtrapolation = false;

    [Tooltip("補間バッファ時間（ミリ秒）- ネットワークジッター対策　回線良ければ低く、ゴミなら高く")]
    [SerializeField] private float interpolationBackTime = 100f;
    
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
    private Vector3 velocity;
    
    void Start()
    {
        PhotonNetwork.SendRate = serializationRate;
        PhotonNetwork.SerializationRate = serializationRate;
        
        lastPosition = transform.position;
        lastRotation = transform.rotation;
    }
    
    void Update()
    {
        if (!PhotonNetwork.IsConnected || photonView.IsMine) return;
        
        // 現在のレンダリング時刻（少し過去）
        double renderTime = PhotonNetwork.Time - interpolationBackTime / 1000.0;
        
        if (useInterpolation)
        {
            InterpolatePosition(renderTime);
        }
        else if (useExtrapolation)
        {
            ExtrapolatePosition(renderTime);
        }
        
        // デバッグ用の切り替え
        if (Keyboard.current != null && Keyboard.current.tKey.wasPressedThisFrame)
        {
            useInterpolation = !useInterpolation;
            useExtrapolation = !useExtrapolation;
            Debug.Log($"Interpolation: {useInterpolation}, Extrapolation: {useExtrapolation}");
        }
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
            transform.position = to.position;
            transform.rotation = to.rotation;
            return;
        }
        
        // 補間率を計算
        double length = to.timestamp - from.timestamp;
        float t = (length > 0) ? (float)((renderTime - from.timestamp) / length) : 0f;
        t = Mathf.Clamp01(t);
        
        // 補間実行
        transform.position = Vector3.Lerp(from.position, to.position, t);
        transform.rotation = Quaternion.Slerp(from.rotation, to.rotation, t);
        
        // 速度を計算（外挿用）
        if (length > 0)
        {
            velocity = (to.position - from.position) / (float)length;
        }
    }
    
    private void ExtrapolatePosition(double renderTime)
    {
        if (stateCount < 1) return;
        
        // 最新の状態を取得
        State latest = stateBuffer[stateCount - 1];
        
        // 最新状態からの経過時間
        float timeSinceLatest = (float)(renderTime - latest.timestamp);
        
        // 外挿（速度ベース）
        transform.position = latest.position + velocity * timeSinceLatest;
        transform.rotation = latest.rotation;
        
        // 外挿時間が長すぎる場合は警告
        if (timeSinceLatest > 0.5f)
        {
            Debug.LogWarning($"Extrapolation time too long: {timeSinceLatest}s");
        }
    }
    
    void IPunObservable.OnPhotonSerializeView(PhotonStream stream, PhotonMessageInfo info)
    {
        if (!PhotonNetwork.IsConnected) return;
        
        if (photonView.IsMine)
        {
            // 自分の状態を送信
            stream.SendNext(transform.position);
            stream.SendNext(transform.rotation);
        }
        else
        {
            // 他プレイヤーの状態を受信
            Vector3 pos = (Vector3)stream.ReceiveNext();
            Quaternion rot = (Quaternion)stream.ReceiveNext();
            
            // タイムスタンプ付きでバッファに追加
            State newState = new State
            {
                position = pos,
                rotation = rot,
                timestamp = info.SentServerTime
            };
            
            AddState(newState);
            
            // 速度計算（初回または前回データがある場合）
            if (stateCount >= 2)
            {
                State prev = stateBuffer[stateCount - 2];
                double deltaTime = newState.timestamp - prev.timestamp;
                if (deltaTime > 0)
                {
                    velocity = (newState.position - prev.position) / (float)deltaTime;
                }
            }
            
            lastPosition = pos;
            lastRotation = rot;
        }
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