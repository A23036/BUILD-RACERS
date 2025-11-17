using Photon.Pun;
using UnityEngine;
using UnityEngine.SceneManagement;

public class playScene : baseScene
{
    [Tooltip("BOTの生成数")]
    [SerializeField] int GenerateBotsNum = 0;

    [SerializeField] private GameObject DriverUI;
    [SerializeField] private GameObject EngineerUI;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        preSceneName = "select";

        GenerateKarts();
        //Debug.Log("THROUGH START");
    }

    // Update is called once per frame
    void Update()
    {
        if (Input.GetKey(KeyCode.R))
        {
            ToResult();
        }

        base.Update();
    }

    public void ToResult()
    {
        SceneManager.LoadScene("result");
    }

    public override void OnJoinedRoom()
    {
        //カートの生成
        GenerateKarts();

        Debug.Log("接続成功");
    }

    public void GenerateKarts()
    {
        //オフラインなら処理なし
        if (!PhotonNetwork.IsConnected) return;

        if(PlayerPrefs.GetInt("driver") != -1)
        {
            // プレイヤー生成（自分）
            var position = new Vector3(Random.Range(-3f, 3f), 3, Random.Range(-3f, 3f));
            var player = PhotonNetwork.Instantiate("Player", position, Quaternion.identity);
            var playerCc = player.GetComponent<CarController>();
            playerCc.SetCamera();

            float geneX = 0, geneZ = 0;
            for (int i = 0; i < GenerateBotsNum; i++)
            {
                // CPUの生成　テスト
                position = new Vector3(geneX, 3, -i * geneZ);
                var cpu = PhotonNetwork.Instantiate("Player", position, Quaternion.identity);
                var cpuCc = cpu.GetComponent<CarController>();

                // cpu に AI を設定する。WaypointContainer を渡す（シーンに複数ある場合は適切に選択）
                var wpContainer = FindObjectOfType<WaypointContainer>(); // 単一ならこれでOK

                //AIをクラス指定で選択して生成　引数はウェイポイント
                cpuCc.SetAI<AIDriver>(wpContainer);
                //cpuCc.SetAI<AIDriver_v2>();

                geneX++;
                if (geneX >= 3)
                {
                    geneX = 0;
                    geneZ += .1f;
                }
            }

            //UIの表示・非表示
            DriverUI.SetActive(true);
            EngineerUI.SetActive(false);
        }
        else if(PlayerPrefs.GetInt("engineer") != -1)
        {
            //UIの表示・非表示
            DriverUI.SetActive(false);
            EngineerUI.SetActive(true);
        }
        else
        {
            Debug.Log("セレクトされていません");
        }
    }
}
