using Photon.Pun;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using ExitGames.Client.Photon;

public class selectSystem : MonoBehaviourPunCallbacks, IPunObservable
{
    //“¯Šú‘ÎÛ‚Ì•Ï”
    private int selectDriverNum;
    private int selectEngineerNum;
    private int colorNumber;

    //ƒJƒ‰[ƒpƒŒƒbƒg
    Color[] colorPalette;
    private int playersCount = 16;

    // ƒZƒŒƒNƒ^[‚ªd‚È‚ç‚È‚¢‚æ‚¤‚É
    private string key;
    private string oldkey;
    private string pendingkey;

    private float timer;

    [SerializeField] private Vector3 offset;
    [SerializeField] private bool gamingColor;

    private IconManager im;
    private List<Transform> driverIcons;
    private List<Transform> engineerIcons;

    private TextMeshProUGUI text;

    void Start()
    {
        selectDriverNum = -1;
        selectEngineerNum = -1;

        timer = 0f;

        //ƒZƒŒƒNƒg‚Ì‰Šú‰»
        PlayerPrefs.SetInt("driverNum", 1);
        PlayerPrefs.SetInt("engineerNum", -1);
    }

    private void Awake()
    {
        text = GameObject.Find("DebugMessage").GetComponent<TextMeshProUGUI>();
        im = GameObject.Find("IconManager").GetComponent<IconManager>();
        driverIcons = im.GetDriverIconsList();
        engineerIcons = im.GetEngineerIconsList();

        //ƒLƒƒƒ“ƒoƒX‚Ìq‹Ÿ‚Éİ’è
        Canvas canvas = GameObject.FindObjectOfType<Canvas>();
        transform.SetParent(canvas.transform, false);

        //ƒZƒŒƒNƒ^[‚ÌF‚ğƒvƒŒƒCƒ„[‚Ì”‚Å•ªŠ„
        Color[] cols = new Color[playersCount];
        for (int i = 0; i < playersCount; i++)
        {
            float h = i / (float)playersCount; // 0..1
            cols[i] = Color.HSVToRGB(h, 1, 1);
        }
        colorPalette = cols;
    }

    void Update()
    {
        //F‚ÌXV
        UpdateColor();

        // •\¦i©•ª‚ÌƒIƒuƒWƒFƒNƒg‚É‚¾‚¯•`‰æ‚ğ”C‚¹‚éê‡j
        if (!photonView.IsMine) return;

        //ƒQ[ƒ~ƒ“ƒOƒJƒ‰[
        if (gamingColor)
        {
            timer += Time.deltaTime;
            if (timer >= .1f)
            {
                colorNumber++;
                timer = 0f;
            }
        }

        //Debug numbers
        if (true)
        {
            timer += Time.deltaTime;
            if (timer >= 1f)
            {
                //À•WŠm”F
                //Debug.Log(selectDriverNum + "," + selectBuilderNum);

                //ƒJƒXƒ^ƒ€ƒvƒƒpƒeƒBŠm”F
                var props = PhotonNetwork.CurrentRoom.CustomProperties;

                Debug.Log($"[RoomPropCount] {props.Count}");
                foreach (var kv in props)
                {
                    Debug.Log($"[RoomProp] {kv.Key} = {kv.Value}");
                }

                timer = 0f;
            }
        }

        if (selectDriverNum == -1 && selectEngineerNum == -1)
        {
            transform.position = new Vector3(-100, -100, -100);
            text.text = "NOW SELECT : NONE";
        }
        else
        {
            if (selectDriverNum != -1)
            {
                transform.position = driverIcons[selectDriverNum].position + offset;
                text.text = "NOW SELECT : DRIVER" + (selectDriverNum + 1);
                PlayerPrefs.SetInt("driverNum", selectDriverNum + 1);
                PlayerPrefs.SetInt("engineerNum", -1);
            }
            else
            {
                transform.position = engineerIcons[selectEngineerNum].position + offset;
                text.text = "NOW SELECT : ENGINEER" + (selectEngineerNum + 1);
                PlayerPrefs.SetInt("driverNum", -1);
                PlayerPrefs.SetInt("engineerNum", selectEngineerNum + 1);
            }
        }
    }
    public bool TryReserveSlot(string pendkey)
    {
        int actor = PhotonNetwork.LocalPlayer.ActorNumber;
        
        //©•ª‚ğ‘I‘ğ‚Ì‚Æ‚«‚ÍCAS‚ÌŠm”F‚ğ‚¹‚¸‚ÉƒZƒbƒg
        if(pendingkey != null && key == pendkey)
        {
            Debug.Log("©•ª‚ğ‘I‘ğ");
            var propsToSet = new Hashtable { { pendkey, null } };
            bool success = PhotonNetwork.CurrentRoom.SetCustomProperties(propsToSet);
            return success;
        }
        else
        {
            Debug.Log("©•ªˆÈŠO‚ğ‘I‘ğ");
            var propsToSet = new Hashtable { { pendkey, actor } };
            var expected = new Hashtable { { pendkey, null } }; // ƒL[‚ª–³‚¯‚ê‚Î—\–ñ‚Å‚«‚éiŒ´q“Ij
            bool success = PhotonNetwork.CurrentRoom.SetCustomProperties(propsToSet, expected);
            return success;
        }
    }

    public bool ReleaseSlot(string key)
    {
        int actor = PhotonNetwork.LocalPlayer.ActorNumber;
        // ©•ª‚ªè—L‚µ‚Ä‚¢‚é‚©Šm”F‚µ‚Ä‚©‚ç‰ğœ‚·‚é‚Ì‚ªˆÀ‘S
        object cur;
        if (PhotonNetwork.CurrentRoom.CustomProperties.TryGetValue(key, out cur))
        {
            if (cur is int owner && owner == actor)
            {
                /*
                var propsToSet = new Hashtable { { key, null } };
                var expected = new Hashtable { { key, actor } }; // ©•ª‚ªŠ—L‚µ‚Ä‚¢‚ê‚Î‰ğœ
                bool success = PhotonNetwork.CurrentRoom.SetCustomProperties(propsToSet, expected);
                Debug.Log("DELETE KEY");
                return success;
                */
            }
        }

        var propsToSet = new Hashtable { { key, null } };
        var expected = new Hashtable { { key, actor } }; // ©•ª‚ªŠ—L‚µ‚Ä‚¢‚ê‚Î‰ğœ
        bool success = PhotonNetwork.CurrentRoom.SetCustomProperties(propsToSet, expected);
        Debug.Log("DELETE KEY");
        return success;
    }

    public void SetNum(int driver, int engineer)
    {
        //‘—MÏ‚İ‚È‚çƒR[ƒ‹ƒoƒbƒN‚Ü‚Å‘—M‚µ‚È‚¢
        if (pendingkey != null)
        {
            Debug.Log("—\–ñ‘—MÏ‚İ");
            return;
        }

        // —\–ñ‚ğƒŠƒNƒGƒXƒg@ƒ[ƒJƒ‹‚ÌŠm’èEXV‚ÍƒR[ƒ‹ƒoƒbƒN‚Ås‚¤
        pendingkey = (driver != -1) ? $"D_{driver + 1}" : $"B_{engineer + 1}";
        
        // ƒL[‚Ì—\–ñƒŠƒNƒGƒXƒg‚Ì‘—M
        if (!TryReserveSlot(pendingkey))
        {
            //—\–ñ¸”s‚È‚çŠó–]’l‚ğƒŠƒZƒbƒg
            pendingkey = null;
            Debug.Log("—\–ñ¸”s");
        }
        else
        {
            Debug.Log("—\–ñ¬Œ÷");
        }
    }

    //ƒJƒXƒ^ƒ€ƒvƒƒpƒeƒB‚ÌƒR[ƒ‹ƒoƒbƒN
    public override void OnRoomPropertiesUpdate(ExitGames.Client.Photon.Hashtable changed)
    {
        Debug.Log("[Custom CallBack]");

        base.OnRoomPropertiesUpdate(changed);

        // Šó–]’l‚ª‚È‚¢ or Šó–]’l‚ªŠÜ‚Ü‚ê‚Ä‚¢‚È‚¢@‚È‚çˆ—‚È‚µ
        if (pendingkey == null)
        {
            Debug.Log("—\–ñ‚È‚µ");
            return;
        }

        if(!changed.ContainsKey(pendingkey))
        {
            Debug.Log("Šó–]’l‚ğŠÜ‚Ü‚È‚¢");
            return;
        }

        //Šó–]’l‚ª‚Æ‚ê‚Ä‚¢‚ê‚Îƒ[ƒJƒ‹‚ğXV
        object value = changed[pendingkey];

        Debug.Log(value);
        
        if (value is int number && number == PhotonNetwork.LocalPlayer.ActorNumber)
        {
            if (pendingkey.StartsWith("D_"))
            {
                selectDriverNum = int.Parse(pendingkey.Substring(2)) - 1;
                selectEngineerNum = -1;
            }
            else
            {
                selectEngineerNum = int.Parse(pendingkey.Substring(2)) - 1;
                selectDriverNum = -1;
            }

            //ƒL[‚ÌƒŠƒŠ[ƒX
            if(oldkey != null)
            {
                ReleaseSlot(oldkey);
            }

            oldkey = pendingkey;
            key = pendingkey;

            Debug.Log("Šl“¾¬Œ÷");
        }
        else if(pendingkey == key)
        {
            if(key != null) ReleaseSlot(key);

            selectDriverNum = -1;
            selectEngineerNum = -1;

            oldkey = null;
            key = null;

            Debug.Log("‰ğœ¬Œ÷");
        }
        else
        {
            Debug.Log("Šl“¾¸”s");
        }

        //Šó–]’l‚ğƒŠƒZƒbƒg
        pendingkey = null;
    }

    public void GetNums(out int dn, out int bn)
    {
        dn = selectDriverNum;
        bn = selectEngineerNum;
    }

    public void OnPhotonSerializeView(PhotonStream stream, PhotonMessageInfo info)
    {
        if (stream.IsWriting)
        {
            // ‚±‚ÌƒNƒ‰ƒCƒAƒ“ƒg‚ªŠ—LÒ‚È‚ç‘—‚é
            stream.SendNext(selectDriverNum);
            stream.SendNext(selectEngineerNum);
            stream.SendNext(colorNumber);
        }
        else
        {
            // ‘¼ƒNƒ‰ƒCƒAƒ“ƒg‚©‚çó‚¯æ‚é
            selectDriverNum = (int)stream.ReceiveNext();
            selectEngineerNum = (int)stream.ReceiveNext();
            colorNumber = (int)stream.ReceiveNext();
        }
    }

    //ƒZƒŒƒNƒ^[‚ÌF‚ÌŠ„‚è“–‚Ä
    public void DecideColor()
    {
        //©•ª‚Ì‚İF‚ğw’è@‘¼ƒvƒŒƒCƒ„[‚Í“¯Šú‚ÅF‚ğó‚¯æ‚é
        if (!photonView.IsMine) return;

        colorNumber = PhotonNetwork.LocalPlayer.ActorNumber;
    }

    public void UpdateColor()
    {
        //–¢İ’è‚È‚çˆ—‚È‚µ
        if (colorNumber == -1) return;

        GetComponent<Image>().color = colorPalette[colorNumber % playersCount];
    }

    public void PrintLog()
    {
        int actorNumber = photonView.Owner.ActorNumber;
        Debug.Log("No." + actorNumber + " COLOR : " + colorNumber);
    }

    void OnDestroy()
    {
        //ï¿½Lï¿½[ï¿½Ì‰ï¿½ï¿½
        if (key != null) ReleaseSlot(key);

        Debug.Log($"selectSystem OnDestroy called on {gameObject.name} instID={this.GetInstanceID()}");
    }
}