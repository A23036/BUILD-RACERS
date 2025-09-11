using UnityEngine;

public class CameraManager : MonoBehaviour
{
    private GameObject playerObj = null;

    //プレイヤーからのカメラ距離　インスペクターから設定できるように公開変数に
    public Vector3 offset = new Vector3(0,3,-5);

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        InitCamera();
    }

    // Update is called once per frame
    void Update()
    {
        //初期化関数が呼ばれていなければ処理なし
        if (playerObj == null) return;

        this.transform.position = playerObj.transform.position + offset;
    }
    /// <summary>
    /// カメラの初期化関数
    /// </summary>
    public void InitCamera()
    {
        //操作プレイヤーを取得、保持しておく
        playerObj = GameObject.Find("player");
    }
}
