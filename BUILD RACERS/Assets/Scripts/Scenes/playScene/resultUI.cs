using Photon.Pun;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using static TMPro.SpriteAssetUtilities.TexturePacker_JsonArray;

public class resultUI : MonoBehaviour
{
    //ゴール時の画像
    [SerializeField] private GameObject resultImageObj;
    private Image resultImage;

    [SerializeField] float scaleDuration = 0.5f;
    [SerializeField] float fadeDuration = 0.4f;
    [SerializeField] float stayTime = 0.3f;
    [SerializeField] Vector3 startScale = Vector3.zero;
    [SerializeField] Vector3 endScale = Vector3.one;

    private bool isResultInitCamera = false;

    //順位表の画像
    [SerializeField] private GameObject[] rankingUIObjects = new GameObject[8]; // 8つのUIプレハブ
    [SerializeField] private float moveInterval = 0.2f; // 各UIの表示間隔(秒)
    [SerializeField] private float moveDuration = 0.5f; // 移動時間
    [SerializeField] private Vector3 rankUIstartOffset = new Vector3(-1000, 0, 0); // 初期オフセット位置
    [SerializeField] private Vector3 menuUIstartOffset = new Vector3(-1000, 0, 0); // 初期オフセット位置

    //プレイシーンから遷移するボタン
    [SerializeField] private GameObject[] menuUIObjects;

    //上からランクUIが更新されるように
    private bool[] rankUIupdateFlags = new bool[8];

    private int pairDriverID = 0;
    private int pairEngineerID = 0;

    private void Awake()
    {
        resultImage = resultImageObj.GetComponent<Image>();
    }

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
    }

    void OnEnable()
    {

    }

    // Update is called once per frame
    void Update()
    {
    }

    public void SetPairDriverID(int id)
    {
        pairDriverID = id;
    }

    public void SetPairEngineerID(int id)
    {
        pairEngineerID = id;
    }

    //ランキングの文字の色を指定
    public void SetTextColor(Color color)
    {
        foreach(var obj in rankingUIObjects)
        {
            var text = obj.GetComponent<TextMeshProUGUI>();
            text.color = color;
        }
    }

    public void SetOutLine(float f , Color color)
    {
        foreach (var obj in rankingUIObjects)
        {
            var text = obj.GetComponent<TextMeshProUGUI>();

            // マテリアルを個別化
            text.fontMaterial = new Material(text.fontMaterial);

            var mat = text.fontMaterial;

            // ★ Outline キーワードを有効化（重要）
            mat.EnableKeyword("OUTLINE_ON");

            // アウトライン色
            mat.SetColor(ShaderUtilities.ID_OutlineColor, color);

            // アウトライン太さ
            mat.SetFloat(ShaderUtilities.ID_OutlineWidth, f);

            // ★ メッシュ更新（重要）
            text.ForceMeshUpdate();
        }
    }

    //コルーチンの開始
    public void StartCoroutines()
    {
        StartCoroutine(PlayResultSequence());
    }

    // メインのシーケンス制御コルーチン
    IEnumerator PlayResultSequence()
    {
        // 最初にPlayResultImageを実行
        yield return StartCoroutine(PlayResultImage());

        // 完了後に8つのUIを表示
        yield return StartCoroutine(ShowRankingUI());

        //シーン遷移UI表示
        yield return StartCoroutine(ShowMenuUI());
    }

    IEnumerator PlayResultImage()
    {
        //transform.localScale = startScale;
        SetTransform(startScale, resultImage);
        SetAlpha(0, resultImage);

        // フェードイン + 拡大
        yield return Animate(0, 1, startScale, endScale, scaleDuration);

        yield return new WaitForSeconds(stayTime);

        // フェードアウト
        yield return Animate(1, 0, endScale, endScale * 1.1f, fadeDuration);

        //初回のみ実行
        if (!isResultInitCamera)
        {
            //カメラワークをゴール後に変更
            var cc = Camera.main.GetComponent<CameraController>();
            if (cc != null)
            {
                cc.SetIsResult(true);
            }

            isResultInitCamera = true;
        }

        //ドライバーのUIを非表示に
        var cars = FindObjectsOfType<CarController>();
        foreach(var car in cars)
        {
            if(car.isMine == false) continue;
            car.HiddenUI();
        }
    }

    IEnumerator ShowRankingUI()
    {
        for (int i = 0; i < rankingUIObjects.Length; i++)
        {
            if (rankingUIObjects[i] != null)
            {
                var obj = rankingUIObjects[i];
                var nowPos = obj.GetComponent<RectTransform>().anchoredPosition;
                StartCoroutine(MoveUIElement(obj, nowPos, (Vector3)nowPos + rankUIstartOffset));
                yield return new WaitForSeconds(moveInterval);
            }
        }

        //クリック or タッチでUIを画面外へ
        while (true)
        {
            if ((Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame) ||
                (Touchscreen.current != null && Touchscreen.current.primaryTouch.press.wasPressedThisFrame))
            {
                break; // 入力されたらループを抜ける
            }

            yield return null;
        }

        for (int i = 0; i < rankingUIObjects.Length; i++)
        {
            if (rankingUIObjects[i] != null)
            {
                var obj = rankingUIObjects[i];
                var nowPos = obj.GetComponent<RectTransform>().anchoredPosition;
                StartCoroutine(MoveUIElement(obj, nowPos, (Vector3)nowPos - rankUIstartOffset));
                yield return new WaitForSeconds(moveInterval);
            }
        }
    }

    IEnumerator ShowMenuUI()
    {
        for (int i = 0; i < menuUIObjects.Length; i++)
        {
            if (menuUIObjects[i] != null)
            {
                var obj = menuUIObjects[i];
                var nowPos = obj.GetComponent<RectTransform>().anchoredPosition;
                StartCoroutine(MoveUIElement(obj, nowPos, (Vector3)nowPos + menuUIstartOffset));
                yield return new WaitForSeconds(moveInterval);
            }
        }
    }

    // 個別のUI要素を移動させるコルーチン
    IEnumerator MoveUIElement(GameObject uiObj, Vector3 start, Vector3 end)
    {
        RectTransform rt = uiObj.GetComponent<RectTransform>();
        if (rt == null) yield break;

        // Vector3をVector2に変換（Z座標を無視）
        Vector2 startPos = new Vector2(start.x, start.y);
        Vector2 endPos = new Vector2(end.x, end.y);

        // 初期位置を設定
        rt.anchoredPosition = startPos;
        uiObj.SetActive(true);

        // 移動アニメーション
        float t = 0;
        while (t < moveDuration)
        {
            t += Time.deltaTime;
            float eased = 1 - Mathf.Pow(1 - t, 5);
            rt.anchoredPosition = Vector2.Lerp(startPos, endPos, eased);
            yield return null;
        }

        rt.anchoredPosition = endPos;
    }

    IEnumerator Animate(float fromA, float toA, Vector3 fromS, Vector3 toS, float time)
    {
        float t = 0;
        while (t < time)
        {
            t += Time.deltaTime;
            float r = t / time;

            //transform.localScale = Vector3.Lerp(fromS, toS, r);
            SetTransform(Vector3.Lerp(fromS, toS, r), resultImage);
            SetAlpha(Mathf.Lerp(fromA, toA, r), resultImage);

            yield return null;
        }
    }

    void SetAlpha(float a, Image image)
    {
        Color c = image.color;
        c.a = a;
        image.color = c;
    }

    void SetTransform(Vector3 v, Image image)
    {
        RectTransform rt = image.GetComponent<RectTransform>();
        rt.localScale = v;
    }

    public void PushToMenuButton()
    {
        //メニューシーンへ遷移
        SceneManager.LoadScene("menu");
    }

    //ランキング表の更新
    public void UpdateRankUI(string name , float time , int d_id = -1 , int e_id = -1)
    {
        int rank = 0;
        for (int i = 0; i < rankUIupdateFlags.Length; i++)
        {
            if (rankUIupdateFlags[i]) continue;
            else
            {
                rankUIupdateFlags[i] = true;
                rank = i + 1;
                break;
            }
        }

        var Text = rankingUIObjects[rank - 1].GetComponent<TextMeshProUGUI>();
        if (Text == null) return;
        
        string rankStr = rank.ToString();
        if (rank == 1) rankStr += " st";
        else if (rank == 2) rankStr += " nd";
        else if (rank == 3) rankStr += " rd";
        else rankStr += " th";

        int minutes = (int)(time / 60);
        int seconds = (int)(time % 60);
        int milliseconds = (int)((time * 1000) % 1000);
        string timeStr = string.Format("{0:00}:{1:00}:{2:000}", minutes, seconds, milliseconds);

        //エンジニアのペアネーム設定
        string pairName = "--------";
        if (d_id == pairDriverID)
        {
            //エンジニア側処理
            pairName = PlayerPrefs.GetString("PlayerName");
        }
        else if(e_id == pairEngineerID)
        {
            //ペアのドライバー側処理
            PhotonView pv = PhotonView.Find(e_id);
            if (pv != null) 
            {
                pairName = pv.Owner.NickName;
            }
        }

        string format = $"<mspace=0.7em>{rankStr} , {name.PadRight(8)} & {pairName.PadRight(8)} , {timeStr}</mspace>";
        Text.text = format;

        //誤差の修正　自分より長いタイムがあれば交換する
        for (int i = rankUIupdateFlags.Length - 1; i >= 0; i--)
        {
            //未登録 or 自分自身なら処理なし
            if (rankUIupdateFlags[i] == false || i == rank - 1) continue;

            var text = rankingUIObjects[i].GetComponent<TextMeshProUGUI>();
            Debug.Log(text.text);
            string uiTimeStr = text.text.Substring(text.text.Length - 9 - 9);
            int uiMinute = int.Parse(uiTimeStr.Substring(0, 2));
            int uiSecond = int.Parse(uiTimeStr.Substring(3, 2));
            float uiMiriSec = int.Parse(uiTimeStr.Substring(6, 3));

            float uiSumTime = uiMinute * 60 + uiSecond + uiMiriSec / 1000;

            Debug.Log($"UI:{uiSumTime} <= NEW:{time}");
            if(uiSumTime <= time) continue;
            Debug.Log(" *** SWAP RANK *** ");

            //登録済みのタイムより短ければ入れ替え　順位以降を入れ替え
            var tempUIStr = rankingUIObjects[i].GetComponent<TextMeshProUGUI>().text;
            var tempCurStr = rankingUIObjects[rank - 1].GetComponent<TextMeshProUGUI>().text;
            //文字列を切る場所の添え字
            int cutIdx = 4;
            var preUI = tempUIStr.Substring(0, cutIdx + 14);
            var preCur = tempCurStr.Substring(0, cutIdx + 14);
            var suffUI = tempUIStr.Substring(cutIdx + 14);
            var suffCur = tempCurStr.Substring(cutIdx + 14);
            rankingUIObjects[rank - 1].GetComponent<TextMeshProUGUI>().text = preCur + suffUI;
            rankingUIObjects[i].GetComponent<TextMeshProUGUI>().text = preUI + suffCur;

            //文字の色も交換が必要なら行う
            if(rankingUIObjects[rank - 1].GetComponent<TextMeshProUGUI>().color == Color.yellow)
            {
                rankingUIObjects[rank - 1].GetComponent<TextMeshProUGUI>().color = Color.white;
                rankingUIObjects[i].GetComponent<TextMeshProUGUI>().color = Color.yellow;
            }
            else if(rankingUIObjects[i].GetComponent<TextMeshProUGUI>().color == Color.yellow)
            {
                rankingUIObjects[rank - 1].GetComponent<TextMeshProUGUI>().color = Color.yellow;
                rankingUIObjects[i].GetComponent<TextMeshProUGUI>().color = Color.white;
            }
            
            rank = i + 1;
        }

        //プレイヤーなら黄色に
        Text = rankingUIObjects[rank - 1].GetComponent<TextMeshProUGUI>();
        if (name == PlayerPrefs.GetString("PlayerName") || pairName == PlayerPrefs.GetString("PlayerName"))
        {
            Debug.Log($"YELLOW NAME = {name}");
            Text.color = Color.yellow;
        }
        //if (name == PhotonNetwork.LocalPlayer.NickName) Text.color = Color.yellow;
    }
}
