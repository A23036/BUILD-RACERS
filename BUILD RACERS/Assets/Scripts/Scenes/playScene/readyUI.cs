using Photon.Pun;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using static TMPro.SpriteAssetUtilities.TexturePacker_JsonArray;

public class readyUI : MonoBehaviour
{
    //ローディングの画像
    [SerializeField] private GameObject loadingImageObj;
    private Image loadingImage;

    //レディーの画像
    [SerializeField] private GameObject readyImageObj;
    private Image readyImage;

    //ゴーの画像
    [SerializeField] private GameObject goImageObj;
    private Image goImage;

    [SerializeField] float scaleDuration = 0.5f;
    [SerializeField] float fadeDuration = 0.4f;
    [SerializeField] float stayTime = 0.3f;
    [SerializeField] Vector3 startScale = Vector3.zero;
    [SerializeField] Vector3 endScale = Vector3.one;

    [Header("Sound")]
    [SerializeField] private AudioClip gameBgm; // ゲームBGM

    private bool isPlayReady = false;
    private bool isPlayGo = false;

    private void Awake()
    {
        loadingImage = loadingImageObj.GetComponent<Image>();
        readyImage = readyImageObj.GetComponent<Image>();
        goImage = goImageObj.GetComponent<Image>();
    }

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        //観戦者なら読み込みを非表示に
        if(PlayerPrefs.GetInt("isMonitor") == 1)
        {
            Color temp = loadingImage.color;
            temp.a = 0f;
            loadingImage.color = temp;
        }
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
        StartCoroutine(PlayReadySequence());
    }

    // メインのシーケンス制御コルーチン
    IEnumerator PlayReadySequence()
    {
        // レディー
        yield return StartCoroutine(PlayReadyImage());

        // ゴー
        yield return StartCoroutine(PlayGoImage());
    }

    public void StartReadyImage()
    {
        if (isPlayReady) return;
        loadingImage.enabled = false;
        StartCoroutine(PlayReadyImage());
        isPlayReady = true;
    }

    public IEnumerator PlayReadyImage()
    {
        if (isPlayReady) yield break;

        //transform.localScale = startScale;
        SetTransform(startScale, readyImage);
        SetAlpha(0, readyImage);

        // フェードイン + 拡大
        yield return Animate(0, 1, startScale, endScale, scaleDuration, readyImage);

        yield return new WaitForSeconds(stayTime);

        // フェードアウト
        yield return Animate(1, 0, endScale, endScale * 1.1f, fadeDuration , readyImage);

        isPlayReady = true;
    }

    public void StartGoImage()
    {
        if(isPlayGo) return;
        StartCoroutine(PlayGoImage());
        isPlayGo = true;
    }

    public IEnumerator PlayGoImage()
    {
        if(isPlayGo) yield break;

        SetTransform(startScale, goImage);
        SetAlpha(0, goImage);

        // フェードイン + 拡大
        yield return Animate(0, 1, startScale, endScale, scaleDuration,goImage);

        yield return new WaitForSeconds(stayTime);

        SoundManager.Instance.PlayBGM(gameBgm);

        // フェードアウト
        yield return Animate(1, 0, endScale, endScale * 1.1f, fadeDuration,goImage);

        isPlayGo = true;
    }

    IEnumerator Animate(float fromA, float toA, Vector3 fromS, Vector3 toS, float time , Image image)
    {
        float t = 0;
        while (t < time)
        {
            t += Time.deltaTime;
            float r = t / time;

            //transform.localScale = Vector3.Lerp(fromS, toS, r);
            SetTransform(Vector3.Lerp(fromS, toS, r), image);
            SetAlpha(Mathf.Lerp(fromA, toA, r), image);

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
}
