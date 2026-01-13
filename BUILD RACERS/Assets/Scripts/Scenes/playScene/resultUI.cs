using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using static TMPro.SpriteAssetUtilities.TexturePacker_JsonArray;

public class resultUI : MonoBehaviour
{
    [SerializeField] private GameObject resultImageObj;
    private Image resultImage;

    [SerializeField] float scaleDuration = 0.5f;
    [SerializeField] float fadeDuration = 0.4f;
    [SerializeField] float stayTime = 0.3f;
    [SerializeField] Vector3 startScale = Vector3.zero;
    [SerializeField] Vector3 endScale = Vector3.one;

    private bool isResultInitCamera = false;

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
        StartCoroutine(PlayResultImage());
    }

    // Update is called once per frame
    void Update()
    {
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

        /*
        //カメラを固定にする
        var cameraController = Camera.main.GetComponent<CameraController>();
        if (cameraController != null)
        {
            cameraController.SetFixedPos(new Vector3(-20,7,60) , 3f);
        }
        */

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
}
