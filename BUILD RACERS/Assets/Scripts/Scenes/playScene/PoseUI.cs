using Photon.Pun;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using static TMPro.SpriteAssetUtilities.TexturePacker_JsonArray;

public class PoseUI : MonoBehaviour
{
    [SerializeField] private GameObject poseUI;

    [Header("Sound")]
    [SerializeField] private AudioClip openSe;      // メニュー開く音
    [SerializeField] private AudioClip exitSe;      // 退出音
    private bool isClicked = false;

    Dictionary<GameObject, bool> upFlags = new Dictionary<GameObject, bool>();

    [SerializeField] private float duration = 0.2f;
    private bool isMoving = false;

    private Image backImage;

    private void Awake()
    {
        
    }

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        upFlags[poseUI] = false;

        backImage = GameObject.Find("poseRootBackImage").GetComponent<Image>();
    }

    void OnEnable()
    {

    }

    // Update is called once per frame
    void Update()
    {
        if(Keyboard.current.escapeKey.wasPressedThisFrame)
        {
            SoundManager.Instance.PlaySE(openSe);
            MoveY(poseUI);
            Debug.Log("PoseUI MoveY");
        }
    }

    public void MoveY(GameObject obj)
    {
        StartCoroutine(Move(obj));
    }

    IEnumerator Move(GameObject obj)
    {
        if(isMoving) yield break;

        isMoving = true;
        upFlags[obj] = !upFlags[obj];

        var rectTransform = obj.GetComponent<RectTransform>();

        if (rectTransform == null) yield break;

        Vector2 start = rectTransform.anchoredPosition;
        Vector2 end = start + new Vector2(0, upFlags[obj] ? backImage.rectTransform.rect.height : -backImage.rectTransform.rect.height);
        float t = 0;

        while (t < 1)
        {
            t += Time.deltaTime / duration;
            float eased = 1 - Mathf.Pow(1 - t, 5);
            rectTransform.anchoredPosition = Vector2.Lerp(start, end, eased);
            yield return null;
        }

        isMoving = false;
    }

    public void PushContinueButton()
    {
        MoveY(poseUI);
    }

    public void PushExitButton()
    {
        if (!isClicked)
        {
            SoundManager.Instance.PlaySE(exitSe);
            isClicked = true;
        }
        if (PhotonNetwork.InRoom) PhotonNetwork.LeaveRoom();
        SceneManager.LoadScene("menu");
    }

    public void PushExitButtonOnTittle()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
      Application.Quit();
#endif
    }
}
