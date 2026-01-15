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
        SetAlpha(0 , resultImage);

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
    IEnumerator MoveUIElement(GameObject uiObj , Vector3 start , Vector3 end)
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

    void SetAlpha(float a , Image image)
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
    public void UpdateRankUI(string name , float time)
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
        int milliseconds = (int)((time * 100) % 100);
        string timeStr = string.Format("{0:00}:{1:00}:{2:00}", minutes, seconds, milliseconds);
        
        string format = $"{rankStr} , {name.PadRight(8)} & xxxxxxxx , {timeStr}";
        Text.text = format;

        //プレイヤーなら黄色に
        if (name == PlayerPrefs.GetString("PlayerName")) Text.color = Color.yellow;

        Debug.Log("UPDATE RANK UI : " + format);
    }
}
