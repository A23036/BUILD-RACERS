using TMPro;
using UnityEngine;
using Photon.Pun;

public class Comment : MonoBehaviourPunCallbacks
{
    private RectTransform rt;

    private float speed = 200f;

    private TextMeshProUGUI textMeshPro;

    [Header("Speed")]
    [SerializeField] private float baseSpeed = 200f;   // 20ï∂éöà»â∫
    [SerializeField] private float maxSpeed = 400f;    // 100ï∂éö
    [SerializeField] private int speedUpStartLen = 10; // Ç±Ç±Ç©ÇÁâ¡ë¨
    [SerializeField] private int maxLen = 100;         // ç≈ëÂï∂éöêî

    [SerializeField] private const float StartX = 3500f;
    [SerializeField] private const float DestroyX = -4000f;

    private void Awake()
    {
    }

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        GameObject canvas = GameObject.Find("CommentUI");
        transform.SetParent(canvas.transform, false);

        textMeshPro = GetComponentInChildren<TextMeshProUGUI>(true);

        rt = GetComponent<RectTransform>();

        rt.anchoredPosition = new Vector2(StartX, rt.anchoredPosition.y);

        //speed = baseSpeed;
    }

    // Update is called once per frame
    void Update()
    {
        rt.anchoredPosition -= Vector2.right * speed * Time.deltaTime;

        if (rt.anchoredPosition.x <= DestroyX && photonView.IsMine)
        {
            PhotonNetwork.Destroy(gameObject);
        }
    }

    [PunRPC]
    public void RPC_SetMessage(string s)
    {
        SetMessage(s);
    }

    public void SetMessage(string s)
    {
        textMeshPro = GetComponentInChildren<TextMeshProUGUI>(true);

        textMeshPro.text = s;

        int len = s.Length;

        // 20ï∂éöñ¢ñûÇÕàÍíËë¨ìx
        if (len < speedUpStartLen)
        {
            speed = baseSpeed;
            return;
        }

        // 20Å`100ï∂éöÇ≈ speed Ç baseÅ®max Ç…ëùÇ‚Ç∑
        float t = Mathf.InverseLerp(speedUpStartLen, maxLen, len); // 0Å`1
        speed = Mathf.Lerp(baseSpeed, maxSpeed, t);
    }
}
