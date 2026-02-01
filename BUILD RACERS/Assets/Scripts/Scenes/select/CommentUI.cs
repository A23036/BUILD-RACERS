using Photon.Pun;
using TMPro;
using UnityEngine;

public class CommentUI : MonoBehaviourPunCallbacks
{
    private TMP_InputField inputField;

    [SerializeField] private float coolTime = 3.0f;
    private float coolTimer = 0;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        inputField = transform.Find("CommentInput").GetComponent<TMP_InputField>();
    }

    // Update is called once per frame
    void Update()
    {
        if (coolTimer > 0f)
        {
            coolTimer -= Time.deltaTime;
            if (coolTimer < 0f)
            {
                coolTimer = 0f;
                inputField.placeholder.GetComponent<TextMeshProUGUI>().text = "コメントしよう！";
            }
            else
            {
                inputField.placeholder.GetComponent<TextMeshProUGUI>().text = $"あと<mspace=.7em>{coolTimer:0.0}</mspace>秒まってね";
            }
        }
    }

    public void InputComment()
    {
        if (coolTimer > 0f)
        {
            return;
        }

        //コメント生成
        Debug.Log($"COMMENT LOG: {inputField.text}");
        int spawnY = Random.Range(1, 10);
        var comment = PhotonNetwork.Instantiate("Comment", new Vector3(0, 520 + spawnY * -100, 0), Quaternion.identity);
        var pv = comment.GetComponent<PhotonView>();
        pv.RPC("RPC_SetMessage", RpcTarget.All, inputField.text);

        //入力欄のリセット
        inputField.text = "";

        coolTimer = coolTime;
    }
}
