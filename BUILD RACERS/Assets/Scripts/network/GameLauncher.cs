using System;
using System.Collections.Generic;
using Fusion;
using Fusion.Sockets;
using UnityEngine;

public class GameLauncher : MonoBehaviour, INetworkRunnerCallbacks
{
    [SerializeField]
    private NetworkRunner networkRunnerPrefab;

    [SerializeField]
    private NetworkPrefabRef playerAvatarPrefab;
    private async void Start()
    {
        // NetworkRunnerを生成する
        var networkRunner = Instantiate(networkRunnerPrefab);

        // GameLauncherを、NetworkRunnerのコールバック対象に追加する
        networkRunner.AddCallbacks(this);

        // 共有モードのセッションに参加する
        var result = await networkRunner.StartGame(new StartGameArgs
        {
            GameMode = GameMode.Shared
        });
        // 結果をコンソールに出力する
        Debug.Log(result);
    }

    //各種コールバック

    /// <summary>
    /// プレイヤーのAOIからNetworkObjectが外れた時
    /// </summary>
    /// <param name="runner"></param>
    /// <param name="obj"></param>
    /// <param name="player"></param>
    void INetworkRunnerCallbacks.OnObjectExitAOI(NetworkRunner runner, NetworkObject obj, PlayerRef player) { }

    /// <summary>
    /// プレイヤーのAOIにNetworkObjectが入った時
    /// </summary>
    /// <param name="runner"></param>
    /// <param name="obj"></param>
    /// <param name="player"></param>
    void INetworkRunnerCallbacks.OnObjectEnterAOI(NetworkRunner runner, NetworkObject obj, PlayerRef player) { }

    /// <summary>
    /// 新しいプレイヤーがセッションに参加したとき
    /// </summary>
    /// <param name="runner"></param>
    /// <param name="player"></param>
    void INetworkRunnerCallbacks.OnPlayerJoined(NetworkRunner runner, PlayerRef player)
    {
        // セッションへ参加したプレイヤーが自分自身かどうかを判定する
        if (player == runner.LocalPlayer)
        {
            // アバターの初期位置を計算する（半径5の円の内部のランダムな点）
            var rand = UnityEngine.Random.insideUnitCircle * 5f;
            var spawnPosition = new Vector3(rand.x, 2f, rand.y);
            // 自分自身のアバターをスポーンする
            runner.Spawn(playerAvatarPrefab, spawnPosition, Quaternion.identity);
            Debug.Log("Spawn");

            //カメラの追従
            /*
            var view = GetComponent<PlayerView>();
            view.MakeCameraTarget();
            */
        }
    }

    /// <summary>
    /// プレイヤーが切断または退出したとき
    /// </summary>
    /// <param name="runner"></param>
    /// <param name="player"></param>
    void INetworkRunnerCallbacks.OnPlayerLeft(NetworkRunner runner, PlayerRef player) { }

    /// <summary>
    /// クライアントから入力情報を送るタイミングで呼ばれる
    /// </summary>
    /// <param name="runner"></param>
    /// <param name="input"></param>
    void INetworkRunnerCallbacks.OnInput(NetworkRunner runner, NetworkInput input) { }

    /// <summary>
    /// 特定プレイヤーから入力データが届かなかったフレーム
    /// </summary>
    /// <param name="runner"></param>
    /// <param name="player"></param>
    /// <param name="input"></param>
    void INetworkRunnerCallbacks.OnInputMissing(NetworkRunner runner, PlayerRef player, NetworkInput input) { }

    /// <summary>
    /// Runner がシャットダウンするとき
    /// </summary>
    /// <param name="runner"></param>
    /// <param name="shutdownReason"></param>
    void INetworkRunnerCallbacks.OnShutdown(NetworkRunner runner, ShutdownReason shutdownReason) { }

    /// <summary>
    /// サーバーへの接続に成功した直後
    /// </summary>
    /// <param name="runner"></param>
    void INetworkRunnerCallbacks.OnConnectedToServer(NetworkRunner runner) { }

    /// <summary>
    /// サーバーから切断されたとき
    /// </summary>
    /// <param name="runner"></param>
    /// <param name="reason"></param>
    void INetworkRunnerCallbacks.OnDisconnectedFromServer(NetworkRunner runner, NetDisconnectReason reason) { }

    /// <summary>
    /// クライアントから接続リクエストが来たとき
    /// </summary>
    /// <param name="runner"></param>
    /// <param name="request"></param>
    /// <param name="token"></param>
    void INetworkRunnerCallbacks.OnConnectRequest(NetworkRunner runner, NetworkRunnerCallbackArgs.ConnectRequest request, byte[] token) { }

    /// <summary>
    /// サーバーへの接続が失敗したとき
    /// </summary>
    /// <param name="runner"></param>
    /// <param name="remoteAddress"></param>
    /// <param name="reason"></param>
    void INetworkRunnerCallbacks.OnConnectFailed(NetworkRunner runner, NetAddress remoteAddress, NetConnectFailedReason reason) { }

    /// <summary>
    /// ユーザー定義のシミュレーションメッセージを受け取ったとき
    /// </summary>
    /// <param name="runner"></param>
    /// <param name="message"></param>
    void INetworkRunnerCallbacks.OnUserSimulationMessage(NetworkRunner runner, SimulationMessagePtr message) { }

    /// <summary>
    /// ロビー検索の結果、利用可能なセッションリストが更新されたとき
    /// </summary>
    /// <param name="runner"></param>
    /// <param name="sessionList"></param>
    void INetworkRunnerCallbacks.OnSessionListUpdated(NetworkRunner runner, List<SessionInfo> sessionList) { }

    /// <summary>
    /// カスタム認証を使ってログインした後、サーバーから応答を受け取ったとき
    /// </summary>
    /// <param name="runner"></param>
    /// <param name="data"></param>
    void INetworkRunnerCallbacks.OnCustomAuthenticationResponse(NetworkRunner runner, Dictionary<string, object> data) { }

    /// <summary>
    /// ホストが落ちて、新しいホストに移行するとき
    /// </summary>
    /// <param name="runner"></param>
    /// <param name="hostMigrationToken"></param>
    void INetworkRunnerCallbacks.OnHostMigration(NetworkRunner runner, HostMigrationToken hostMigrationToken) { }

    /// <summary>
    /// Runner.SendReliableData で送信したデータを受信したとき
    /// </summary>
    /// <param name="runner"></param>
    /// <param name="player"></param>
    /// <param name="key"></param>
    /// <param name="data"></param>
    void INetworkRunnerCallbacks.OnReliableDataReceived(NetworkRunner runner, PlayerRef player, ReliableKey key, ArraySegment<byte> data) { }

    /// <summary>
    /// 大きなデータを分割送信したときの進行状況が通知される
    /// </summary>
    /// <param name="runner"></param>
    /// <param name="player"></param>
    /// <param name="key"></param>
    /// <param name="progress"></param>
    void INetworkRunnerCallbacks.OnReliableDataProgress(NetworkRunner runner, PlayerRef player, ReliableKey key, float progress) { }

    /// <summary>
    /// Runner.SetActiveScene() などでシーンのロードが完了したとき
    /// </summary>
    /// <param name="runner"></param>
    void INetworkRunnerCallbacks.OnSceneLoadDone(NetworkRunner runner) { }

    /// <summary>
    /// シーンロードが始まったとき
    /// </summary>
    /// <param name="runner"></param>
    void INetworkRunnerCallbacks.OnSceneLoadStart(NetworkRunner runner) { }
}