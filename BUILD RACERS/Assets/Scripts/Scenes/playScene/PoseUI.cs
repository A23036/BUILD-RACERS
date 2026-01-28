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

    Dictionary<GameObject, bool> upFlags = new Dictionary<GameObject, bool>();

    [SerializeField] private float duration = 0.2f;
    private bool isMoving = false;

    private void Awake()
    {
    }

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        upFlags[poseUI] = false;
    }

    void OnEnable()
    {

    }

    // Update is called once per frame
    void Update()
    {
        if(Keyboard.current.escapeKey.isPressed)
        {
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
        Vector2 end = start + new Vector2(0, upFlags[obj] ? 1000 : -1000);
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
        if(PhotonNetwork.InRoom) PhotonNetwork.LeaveRoom();
        SceneManager.LoadScene("menu");
    }
}
