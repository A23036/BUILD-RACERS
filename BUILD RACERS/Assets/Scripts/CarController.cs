using ExitGames.Client.Photon;
using Photon.Pun;
using Photon.Realtime;
using PW;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using static Fusion.Sockets.NetBitBuffer;
using static Unity.Burst.Intrinsics.X86;

public enum State // カートの状態
{
    Drive,  // 通常走行
    Stun,   // 気絶中
    Auto,   // 自動走行（未実装）
    Stop,   // 停止中
}

public enum StunType // スタンの重さ
{
    Light,
    Midium,
    Heavy,
}

public enum BoostType // ブースとの長さ
{
    Short,
    Long,
}
public enum LastInputDevice
{ 
    KeyboardWASD, 
    KeyboardArrow, 
    Gamepad 
}

public class CarController : MonoBehaviourPunCallbacks
{
    // 新InputSystem用
    private float inputMotor;
    private float inputSteer;
    private bool inputUseItem;

    //ジョイスティック
    private Joystick variableJoystick;

    //CPU
    private IDriver driver = null;

    [System.Serializable]
    public class WheelVisual
    {
        public Transform leftWheel;
        public Transform rightWheel;
        public bool steering;  // 前輪なら true
    }

    public List<WheelVisual> wheelVisuals;

    [Header("基本パラメータ")]
    [SerializeField] private float MotorForce = 10f;
    [SerializeField] private float SteerAngle = 60f;
    [SerializeField] private float TurnSensitivity = 2f;
    [SerializeField] private float MaxSpeed = 20f;
    [SerializeField] private float LightStunTime = 0.5f;
    [SerializeField] private float MidiumStunTime = 1.0f;
    [SerializeField] private float HeavyStunTime = 2.0f;
    [SerializeField] private float ShortBoostTime = 0.5f;
    [SerializeField] private float LongBoostTime = 1.0f;
    [SerializeField] private float stunBrakeFactor = 0.92f; // 毎FixedUpdateで減衰

    [Header("重力補正")]
    [SerializeField] private float extraGravity = 20f;

    [Header("地面関連")]
    [SerializeField] private float raycastLength = 1.2f;  // 地面判定距離
    [SerializeField] private LayerMask groundMask;        // 地面レイヤー
    //ダート
    [SerializeField] private float dirtSpeedMultiplier = 0.6f;  // ダート上の速度倍率
    [SerializeField] private float dirtAccelMultiplier = 1.0f;  // ダート上の加速倍率
    //ブースト
    [SerializeField] private float boostSpeedMultiplier = 1.8f;   // ブースト時の速度倍率
    [SerializeField] private float boostAccelMultiplier = 2.5f;   // ブースト時の加速倍率
    [SerializeField] private float boostDuration = 2.0f;          // 効果時間（秒）
    private float boostTimer = 0f;  // 残りブースト時間

    [Header("ブースト演出")]
    [SerializeField] private GameObject boostEffectPrefab;
    [SerializeField] private Vector3 boostEffectLocalPosition = new Vector3(0f, 0f, 1.2f);
    [SerializeField] private Vector3 boostEffectLocalRotation = Vector3.zero;
    [SerializeField] private Vector3 boostEffectLocalScale = Vector3.one;
    private GameObject boostEffectInstance;
    private ParticleSystem boostEffectParticle;

    [Header("Fire Effect")]
    [SerializeField] private ParticleSystem fireEffectPrefab; // ループでも単発でもOK
    [SerializeField] private float fireEffectDuration = 1.5f;  // 出す時間
    [SerializeField] private Vector3 FireFxLocalPos = new Vector3(-0.345f, 0.678f, -1.011f);

    private ParticleSystem fireFxL;
    private ParticleSystem fireFxR;
    private Coroutine fireEffectCo;

    [Header("UI")]
    [SerializeField] private TextMeshProUGUI speedText;  // 速度表示テキスト
    //[SerializeField] private TextMeshProUGUI coinText;  // コイン枚数表示テキスト
    [SerializeField] private TextMeshProUGUI lapText;  // 周回数表示テキスト
    [SerializeField] private TextMeshProUGUI rankText;  // 順位表示テキスト
    [SerializeField] private TextMeshProUGUI timerText;  // タイム表示テキスト
    [SerializeField] private GameObject miniMapFrame;   // 園児に画面での強調表示用UI
    private GameObject hidenCanvas;

    [Header("保持なアイテムの数")]
    [SerializeField] private int MAXITEMNUM = 5;

    private CameraController cameraController;
    private Engineer engineer;
    private Rigidbody rb;
    private InputAction throttleAction;
    private InputAction brakeAction;
    private InputAction steerAction;
    private InputAction useItemAction;

    private float currentSteer = 0f;
    private int coinCnt = 0;
    private string currentGroundTag = "Default";

    private int driverNum = -1;
    private Player pairPlayer = null;
    private int pairViewID = -1;

    private ItemManager itemManager;

    private int partsNum = 0;

    private State state;
    private float stunTime = 0;

    private float stunElapsed = 0f;
    private Quaternion stunStartRotation;
    private Quaternion stunStartLocalRotation;
    private GameObject bodyMesh;
    private GameObject speedMeter;

    [Header("パッシブアイテム用パラメータ")]
    [SerializeField] private float accelerationPower = 0.5f;
    [SerializeField] private float speedPower = 0.05f;
    [SerializeField] private float antiStunPower = 0.2f;

    private int[] passiveNumList = { 0, 0, 0 }; // パッシブ強化状態

    [Header("回転演出用パラメータ")]
    [SerializeField] private AnimationCurve stunEaseCurve;
    [SerializeField] private float stunMinSpeed = 2.0f;
    [SerializeField] private GameObject stunEffect;
    [SerializeField] private Vector3 stunEffectOffset = new Vector3(0f, 1.0f, 0f);
    private float stunSpinAngle = 360f; // 回転量
    private GameObject stunEffectInstance;

    private bool isSetStartPos = false;

    //周回判定用
    private LapManager lapManager;
    private int lapCount = -1;
    private int maxLaps = 0;
    private float nowAngle = 0f;
    private int flagsCount = 0;
    private bool[] flags;
    private bool isLapClear = false;

    public bool isRaceClear = false;

    //オフライン用のフラグ　BOTとの区別用
    public bool isMine = false;

    [Header("UI")]
    [SerializeField] private PassiveUIManager passiveUI;
    private bool shouldUpdatePassiveUI;

    //リザルトUI ゴールしたら有効化
    private GameObject resultUI;

    //現在の順位
    private int currentRank = 1;

    //スタートからの経過秒数
    private double startTime;
    private double timer;

    //ラップタイムが点滅する時間
    [SerializeField] private float lapBlinkTime = 3f;
    private float blinkTimer = 0f;
    private float nextBlinkTime = 0f;
    [SerializeField] private float lapBlinkInterval = .25f;

    //開始までの準備段階かフラグ
    private bool isLoading = true;

    private bool isNotifyDriverConnected = false;

    //検索時間　計測用とタイムアウト制限時間
    [Tooltip("検索時間の制限(秒)")]
    [SerializeField] private float searchLimitTime = 20f;
    private float searchTimer = 0f;

    // ガイドUI
    [Tooltip("ガイドUIが自動で消えるまでの時間")]
    [SerializeField] private float guideResetDelay = 2.0f;
    private GameObject WASDGuide;
    private GameObject arrowGuide;
    private GameObject controllerGuide;
    private GameObject keyboardItemGuide;
    private GameObject controllerItemGuide;

    private LastInputDevice lastDevice = LastInputDevice.KeyboardWASD;
    private float lastInputTime = -999f;

    private float killerTimer = 0f;
    List<(Vector3, Vector3)> debugDrawLineList = new List<(Vector3, Vector3)>();

    [SerializeField] GameObject killerEffect;

    private WaypointContainer wpc;

    private CarController[] cars;
    private bool isGetCars = false;

    private CarController playerCar;

    //順位計算を何秒ごとに行うか
    private float rankTimer = 0f;
    private float rankUpdateInterval = 0.5f;

    //ヒット通知用
    private LogUI logUI;
    private PhotonView logUIpv;

    private bool isTutorial = false;
    private RigidbodyConstraints savedConstraints;

    [Header("Sound")]
    [SerializeField] private AudioClip stunSe; // 爆発音用AudioSource

    public void SetIsTutorial()
    {
        isTutorial = true;
    }
    public bool GetIsTutorial() => isTutorial;

    public void SetState(State st)
    {
        state = st;
        if (!isTutorial) return;

        if (state == State.Stop)
        {
            HardStop();
        }
        if (state == State.Drive)
        {
            ResumeFromStop();
        }
    }

    // 物理挙動を停止
    private void HardStop()
    {
        rb.linearVelocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;
        savedConstraints = rb.constraints;
        rb.constraints = RigidbodyConstraints.FreezeAll;
    }

    // 元に戻す
    private void ResumeFromStop()
    {
        rb.constraints = savedConstraints;
        rb.WakeUp();
    }

    public void AddPartsNum()
    {
        partsNum++;
    }

    public void SubstractPartsNum()
    {
        if (partsNum > 0)
        {
            partsNum--;
        }
    }

    public void SetMapFrame()
    {
        miniMapFrame.SetActive(true);
    }

    public void SetPassiveState(PartsID id, bool isAdd)
    {
        switch (id)
        {
            case PartsID.Acceleration:
                passiveNumList[0] += isAdd ? 1 : -1;
                break;
            case PartsID.Speed:
                passiveNumList[1] += isAdd ? 1 : -1;
                break;
            case PartsID.AntiStun:
                passiveNumList[2] += isAdd ? 1 : -1;
                break;
            default:
                break;
        }

        Debug.Log("PassiveState: Acceleration: " + passiveNumList[0] + " Speed: " + passiveNumList[1] + " AntiStun: " + passiveNumList[2]);

        //パッシブのUI更新
        if (isMine && isRaceClear == false)
        {
            UpdatePassiveUI();
        }
    }

    private void UpdatePassiveUI()
    {
        if (!shouldUpdatePassiveUI || passiveUI == null) return;
        passiveUI.RefreshFromCounts(passiveNumList[0], passiveNumList[1], passiveNumList[2]);
    }

    public void SetBoost(BoostType boostType)
    {
        switch (boostType)
        {
            case BoostType.Short:
                boostTimer = ShortBoostTime;
                break;
            case BoostType.Long:
                boostTimer = LongBoostTime;
                break;
        }

        // ここで火エフェクト再生
        PlayFireEffect();
    }

    public void SetStun(StunType type , string attacekrName, string weaponName)
    {
        if (state == State.Stun) return;
        if (isMine) SoundManager.Instance.PlaySE(stunSe);
        if (killerTimer > 0f) return;

        state = State.Stun;
        ClearBoostEffect();

        if (stunEffectInstance != null)
        {
            Destroy(stunEffectInstance);
        }

        stunEffectInstance = Instantiate(stunEffect);
        UpdateStunEffectTransform();

        switch (type)
        {
            case StunType.Light:
                stunTime = LightStunTime * (1 - passiveNumList[2] * antiStunPower);
                stunSpinAngle = 360f;
                break;
            case StunType.Midium:
                stunTime = MidiumStunTime * (1 - passiveNumList[2] * antiStunPower);
                stunSpinAngle = 360f;
                break;
            case StunType.Heavy:
                stunTime = HeavyStunTime * (1 - passiveNumList[2] * antiStunPower);
                stunSpinAngle = 720f;
                break;
        }

        Destroy(stunEffectInstance, stunTime + 1.0f);

        stunElapsed = 0f;
        stunStartLocalRotation = bodyMesh.transform.localRotation;

        //ヒット通知
        string myName;
        
        if (PhotonNetwork.InRoom)
        {
            myName = photonView.Owner.NickName;

            if (logUI != null)
            {
                if(isMine) logUIpv.RPC("RPC_AddHitLog", RpcTarget.All, attacekrName, myName, weaponName);
            }
        }
        else
        {
            if (isMine)
            {
                myName = PlayerPrefs.GetString("PlayerName");
            }
            else
            {
                myName = GetName();
            }

            if (logUI != null)
            {
                logUI.AddHitLog(attacekrName, myName, weaponName);
            }
        }

        Debug.Log($"SET STAN : {GetName()}");
    }

    private void Awake()
    {
        Debug.Log("=== AWAKE ===");

        string nowSceneName = SceneManager.GetActiveScene().name;
        switch (nowSceneName)
        {
            case "driver tutorial":
                state = State.Drive;
                break;
            default:
                state = State.Stop;
                break;
        }

        driverNum = PlayerPrefs.GetInt("driverNum");
        Debug.Log(driverNum);

        var joystick = GameObject.Find("Floating Joystick");
        if (joystick != null) variableJoystick = joystick.GetComponent<Joystick>();

        speedText = InitText(speedText, "SpeedText");
        //coinText = InitText(coinText, "coinText");
        lapText = InitText(lapText, "LapText");
        rankText = InitText(rankText, "RankText");
        timerText = InitText(timerText, "TimerText");

        hidenCanvas = GameObject.FindGameObjectWithTag("HideCanvas");
        miniMapFrame.SetActive(false);

        rb = GetComponent<Rigidbody>();
        rb.centerOfMass = new Vector3(0f, -1.0f, 0f);
        rb.interpolation = RigidbodyInterpolation.Interpolate;

        bodyMesh = gameObject.transform.Find("BodyMesh").gameObject;
        speedMeter = GameObject.Find("SpeedMeter");

        itemManager = GetComponent<ItemManager>();

        shouldUpdatePassiveUI = !PhotonNetwork.IsConnected || photonView.IsMine;
        if (shouldUpdatePassiveUI && passiveUI == null)
        {
            GameObject passiveRoot = GameObject.Find("PassiveSlotRoot");
            if (passiveRoot != null) passiveUI = passiveRoot.GetComponent<PassiveUIManager>();
        }
        UpdatePassiveUI();

        lapManager = GameObject.Find("LapManager").GetComponent<LapManager>();

        flags = new bool[19];
        for (int i = 0; i < flags.Length; i++) flags[i] = true;


        switch (SceneManager.GetActiveScene().name)
        {
            case "Map1":
                var smm1 = FindObjectOfType<map1>();
                if (smm1 != null)
                {
                    resultUI = smm1.GetResultUI();
                    resultUI.SetActive(false);
                }
                break;
            case "Map2":
                var smm2 = FindObjectOfType<map2>();
                if (smm2 != null)
                {
                    resultUI = smm2.GetResultUI();
                    resultUI.SetActive(false);
                }
                break;
            case "Map3":
                var smm3 = FindObjectOfType<map3>();
                if (smm3 != null)
                {
                    resultUI = smm3.GetResultUI();
                    resultUI.SetActive(false);
                }
                break;
            case "driver tutorial":
                var smm4 = FindObjectOfType<driverTutorial>();
                if (smm4 != null)
                {
                    resultUI = null;
                }
                break;
        }

        if (!PhotonNetwork.IsConnected)
        {
            maxLaps = PlayerPrefs.GetInt("lapCnt");
        }

        startTime = 0;
        timer = 0;

        // ガイド初期表示
        ApplyGuideUI(LastInputDevice.KeyboardWASD);

        WASDGuide = GameObject.Find("WASDGuideImage");
        arrowGuide = GameObject.Find("AllowGuideImage");
        controllerGuide = GameObject.Find("ControllerGuideImage");
        keyboardItemGuide = GameObject.Find("ItemGuideKeyboardImage");
        controllerItemGuide = GameObject.Find("ItemGuideControllerImage");
        
        if (WASDGuide != null) WASDGuide.SetActive(false);
        if (arrowGuide != null) arrowGuide.SetActive(false);
        if (controllerGuide != null) controllerGuide.SetActive(false);
        if (keyboardItemGuide != null) keyboardItemGuide.SetActive(false);
        if (controllerItemGuide != null) controllerItemGuide.SetActive(false);

        // ガイド表示フラグの初期化（無ければON）
        if (!PlayerPrefs.HasKey(OptionPrefs.GUIDE_ENABLED))
        {
            PlayerPrefs.SetInt(OptionPrefs.GUIDE_ENABLED, 1); // デフォルトON
            PlayerPrefs.Save();
            WASDGuide.SetActive(true);
        }
        else if(PlayerPrefs.GetInt(OptionPrefs.GUIDE_ENABLED) == 1　&& driverNum != -1 && driver == null　&& !isTutorial)
        {
            WASDGuide.SetActive(true);
        }

        // fireFx は必要になったタイミングで生成(遅延生成)する

        //キラーエフェクト　最初は非表示
        if(killerEffect != null)
        {
            killerEffect.SetActive(false);
        }

        wpc = FindObjectOfType<WaypointContainer>();

        playerCar = null;

        logUI = FindObjectOfType<LogUI>();
        if(logUI != null)
        {
            logUIpv = logUI.GetComponent<PhotonView>();
        }
    }

    private TextMeshProUGUI InitText(TextMeshProUGUI tmpro, string tag)
    {
        if (tmpro == null)
        {
            var text = GameObject.FindWithTag(tag);
            if (text != null) tmpro = text.GetComponent<TextMeshProUGUI>();
            else tmpro = FindObjectOfType<TextMeshProUGUI>();
        }
        return tmpro;
    }

    private void TryPairPlayers()
    {
        // ペアを発見済みの場合、処理を行わない
        if (!photonView.IsMine) return;
        if (pairViewID != -1)
        {
            Debug.Log($"ペア発見済み：{pairViewID}");
            return;
        }

        Engineer[] engineers = FindObjectsOfType<Engineer>();

        Debug.Log($"{engineers.Length}人の中からペアを検索");

        foreach (var eng in engineers)
        {
            var engPv = eng.GetComponent<PhotonView>();
            Player searchPlayer = engPv.Owner;
            if (searchPlayer.CustomProperties.TryGetValue("engineerNum", out var propEn) && propEn is int)
            {
                //チーム番号の照合
                if ((int)propEn == PlayerPrefs.GetInt("driverNum"))
                {
                    pairViewID = engPv.ViewID;
                    pairPlayer = searchPlayer;

                    Debug.Log("FOUND PAIR! pairID:" + pairViewID);

                    //PhotonViewの有効性を確認
                    PhotonView pairPhotonView = PhotonView.Find(pairViewID);
                    if (pairPhotonView == null)
                    {
                        Debug.Log($"無効なID：{pairViewID}");
                        pairViewID = -1;
                        return;
                    }
                    else
                    {
                        Debug.Log($"有効なID：{pairViewID} , {engineers.Length}人の中からペアを発見");
                    }

                    //カメラの追従
                    SetCamera();

                    //ペアの検索が完了で通知をする　１回のみ実行
                    if (!isNotifyDriverConnected && PlayerPrefs.GetInt("driverNum") != -1 && photonView != null)
                    {
                        //マスタークライアントへエンジニアの生成を通知する
                        PhotonView startPosPv = GameObject.Find("StartPos").GetComponent<PhotonView>();

                        startPosPv.RPC("RPC_NotifyDriverConnected", RpcTarget.AllBuffered , photonView.ViewID);

                        isNotifyDriverConnected = true;

                        logUIpv.RPC("RPC_SetPairName", RpcTarget.All, pairPhotonView.Owner.NickName,photonView.Owner.NickName);
                    }
                }
            }
        }

        if (pairPlayer == null && driver == null) Debug.Log("Pair is null");
    }

    private void OnEnable()
    {
        throttleAction = new InputAction(type: InputActionType.Value);
        throttleAction.AddBinding("<Keyboard>/w");
        throttleAction.AddBinding("<Keyboard>/upArrow");
        throttleAction.AddBinding("<Gamepad>/buttonEast");
        throttleAction.AddBinding("<Touchscreen>/primaryTouch/press");
        throttleAction.Enable();

        brakeAction = new InputAction(type: InputActionType.Button);
        brakeAction.AddBinding("<Keyboard>/s");
        brakeAction.AddBinding("<Keyboard>/downArrow");
        brakeAction.AddBinding("<Gamepad>/buttonSouth");
        brakeAction.Enable();

        steerAction = new InputAction(type: InputActionType.Value);
        steerAction.AddCompositeBinding("1DAxis")
            .With("Negative", "<Keyboard>/a")
            .With("Positive", "<Keyboard>/d");
        steerAction.AddCompositeBinding("1DAxis")
            .With("Negative", "<Keyboard>/leftArrow")
            .With("Positive", "<Keyboard>/rightArrow");
        steerAction.AddBinding("<Gamepad>/leftStick/x");
        steerAction.Enable();

        useItemAction = new InputAction(type: InputActionType.Button);
        useItemAction.AddBinding("<Keyboard>/space");
        useItemAction.AddBinding("<Gamepad>/leftShoulder");
        useItemAction.AddBinding("<Gamepad>/leftTrigger");
        useItemAction.Enable();

        base.OnEnable();
    }

    private void OnDisable()
    {
        throttleAction?.Disable();
        brakeAction?.Disable();
        steerAction?.Disable();
        useItemAction?.Disable();
        ClearBoostEffect();

        // 念のためFireも停止＆非表示（シーン抜け/無効化時の残り対策）
        StopFireEffectImmediate();

        base.OnDisable();
    }

    private void Update()
    {
        //読み込み中ならペア検索
        if(isLoading && photonView.IsMine)
        {
            Debug.Log("ドライバー：ペア検索中！");
            TryPairPlayers();

            //タイムアウト処理
            searchTimer += Time.deltaTime;
            if(searchTimer >= searchLimitTime)
            {
                Debug.Log("タイムアウト：ペア検索に時間がかかりすぎています");
                PhotonNetwork.Disconnect();
                SceneManager.LoadScene("menu");
            }
        }
        else if(!isLoading && photonView.IsMine)
        {
            Debug.Log("ドライバー：ペア発見済み！");
        }

        if (state != State.Drive) return;
        if (PhotonNetwork.IsConnected && !photonView.IsMine) return;
        if (driver != null) return;

        float throttle = throttleAction.ReadValue<float>();
        float brake = brakeAction.ReadValue<float>();
        inputMotor = throttle - brake;

        inputSteer = steerAction.ReadValue<float>();

        if (variableJoystick != null && variableJoystick.Direction != Vector2.zero)
        {
            inputSteer = Mathf.Clamp(variableJoystick.Direction.x / 0.9f, -1f, 1f);
        }

        if (useItemAction.WasPressedThisFrame())
        {
            inputUseItem = true;
            Debug.Log("[INPUT] Use Item");
        }
 
        // スマホでのアイテム使用
        if (!inputUseItem && Touchscreen.current != null)
        {
            foreach (var touch in Touchscreen.current.touches)
            {
                if (!touch.press.wasPressedThisFrame)
                {
                    continue;
                }

                Vector2 touchPosition = touch.position.ReadValue();
                if (touchPosition.x >= Screen.width * 0.5f)
                {
                    inputUseItem = true;
                    Debug.Log("[INPUT] Use Item (Right Tap)");
                    break;
                }
            }
        }
        
        // ガイドUI表示
        if(PlayerPrefs.GetInt(OptionPrefs.GUIDE_ENABLED,1) == 1) DetectAndUpdateGuideUI();

        //Debug.Log("パーツ数:" + partsNum);
    }


    private void FixedUpdate()
    {
        //時間計測
        if (state == State.Drive)
        {
            //タイム計測
            UpdateTimer();
        }

        if (PhotonNetwork.IsConnected && !isMine) return;

        //停止状態なら処理しない
        if (state == State.Stop)
        {
            return;
        }

        if (state == State.Stun)
        {
            UpdateStun();
            return;
        }

        UpdateGroundType();

        //周回角度更新
        UpdateAngle();

        //デバッグ用
        /*
        foreach(var linePos in debugDrawLineList)
        {
            Debug.DrawLine(linePos.Item1, linePos.Item2, Color.blue);
        }
        */

        //キラー更新
        if (killerTimer > 0f) UpdateKiller();

        //周回判定
        if (!isTutorial && isLapClear)
        {
            lapCount++;
            nowAngle = 0f;

            //ラップ数を同期
            if(PhotonNetwork.IsConnected) 
            photonView.RPC("RPC_SyncLapCount", RpcTarget.All, lapCount, photonView.ViewID); 

            //タイマーを点滅　スタート直後を除いて実行
            if(lapCount > 0) blinkTimer = lapBlinkTime;

            isLapClear = false;

            Debug.Log($"Lap Clear! Current Lap: {lapCount}");
        }

        //順位更新
        if(!isTutorial && rankTimer >= rankUpdateInterval)
        {
            UpdateRank();
            if (isMine) UpdateRankUI();

            rankTimer = 0f;
        }
        else
        {
            rankTimer += Time.fixedDeltaTime;
        }

        //ゴール判定
        if (!isTutorial && (lapCount == maxLaps))
        {
            isRaceClear = true;

            //リザルトUIを有効化
            if (resultUI.activeSelf == false) resultUI.SetActive(true);

            //ランキングUIを更新
            var result = resultUI.GetComponent<resultUI>();
            if (result != null)
            {
                if (PhotonNetwork.IsConnected)
                {
                    //ペアのエンジニアにゴールを通知
                    PhotonView target = PhotonView.Find(pairViewID);
                    if (target != null) target.RPC("RPC_ReceiveGoalNotif", RpcTarget.All, photonView.ViewID);

                    result.SetPairEngineerID(pairViewID);

                    Debug.Log($"GOAL TIME : {timer}");
                    if (photonView.IsMine)
                    {
                        photonView.RPC("RPC_UpdateRankUI", RpcTarget.All, photonView.Owner.NickName, timer, photonView.ViewID, pairViewID);

                        //ゴール済みフラグをプロパティに登録
                        Hashtable hash = new Hashtable();
                        hash["isRaceClear"] = true;
                        PhotonNetwork.LocalPlayer.SetCustomProperties(hash);
                    }

                    //ゴールしたドライバー数を送信して各クライアントで記録
                    PhotonView startPosPv = GameObject.Find("StartPos").GetComponent<PhotonView>();
                    startPosPv.RPC("RPC_NotifyDriverGoal", RpcTarget.All);
                }
                else
                {
                    if (isMine)
                    {
                        result.UpdateRankUI(PlayerPrefs.GetString("PlayerName"), timer);
                    }
                    else result.UpdateRankUI(GetName(), timer);
                }
            }

            //リザルトUIを表示開始
            if(isMine) result.StartCoroutines();

            //ゴール後自動走行のAIに切り替え
            ChangeToAutoDriver();

            //ゴール後に表示されるように
            if (isMine && PlayerPrefs.GetInt("driverNum") != -1)
            {
                UpdateRankUI();

                //ゴール後にずれが生じないように
                int minutes = (int)(timer / 60);
                int seconds = (int)(timer % 60);
                int milliseconds = (int)((timer * 1000) % 1000);
                timerText.text = string.Format("{0:00}:{1:00}:{2:000}", minutes, seconds, milliseconds);
                timerText.enabled = true;

                //ラップUIを更新
                lapText.text = $"Lap:{Mathf.Clamp(lapCount + 1, 1, maxLaps)}/{maxLaps}";

                //タイマー黄色に変更
                timerText.color = Color.yellow;
            }

            //ゴール判定が一度のみ実行されるように
            maxLaps = -1;
        }

        // 入力取得
        float motorInput = 0f;
        float brakeInput = 0f;
        float steerInput = 0f;

        // AIがいればそちらから取得
        if (driver != null)
        {
            driver.GetInputs(out float throttle, out float brake, out float steer);
            motorInput = throttle;
            brakeInput = brake;
            steerInput = steer;

            //CPUアイテム使用
            if (driver is AIDriver && itemManager.GetItemNum() > 0 && driver.ItemUseDecision())
            {
               RemoveUsedItem();
            }
        }
        else
        {
            //　プレイヤー入力
            motorInput = throttleAction.ReadValue<float>() - brakeAction.ReadValue<float>();
            steerInput = steerAction.ReadValue<float>();
            //if (Input.GetMouseButton(0)) motorInput = 1;
            if (variableJoystick != null && variableJoystick.Direction != Vector2.zero)
            {
                //右タッチは反応しないように
                if (Touchscreen.current != null)
                {
                    foreach (var touch in Touchscreen.current.touches)
                    {
                        if (!touch.press.wasPressedThisFrame)
                        {
                            continue;
                        }

                        //右ならアクセルいれない
                        Vector2 touchPosition = touch.position.ReadValue();
                        if (touchPosition.x >= Screen.width * 0.5f)
                        {
                            motorInput = 0f;
                            break;
                        }
                    }
                }
            }

            //　プレイヤー入力:Update()で取得した入力を使用
            motorInput = inputMotor;
            steerInput = inputSteer;
        }

        //UI更新
        if(!isTutorial && isMine && PlayerPrefs.GetInt("driverNum") != -1)
        {
            //周回数をUIに反映
            lapText.text = $"Lap:{Mathf.Clamp(lapCount + 1, 1, maxLaps)}/{maxLaps}";

            //順位更新
            UpdateRank();

            //タイムをUIに反映
            UpdateTimerUI();
        }

        // combine motor & brake: motorInput 0..1, brakeInput 0..1 -> netMotor (-1..1) or separate
        float netMotor = motorInput - brakeInput; // keep existing behavior if you like

        currentSteer = steerInput * SteerAngle;

        // 見た目タイヤ回転
        foreach (var w in wheelVisuals)
        {
            float visualSteer = currentSteer * 0.5f;
            if (w.steering)
            {
                if (w.leftWheel != null)
                    w.leftWheel.localRotation = Quaternion.Euler(-90f, visualSteer, 90f);
                if (w.rightWheel != null)
                    w.rightWheel.localRotation = Quaternion.Euler(-90f, visualSteer, 90f);
            }
        }

        // --- Boostタグを検出 ---
        if (currentGroundTag == "Boost")
        {
            boostTimer = boostDuration; // 効果をリセット
        }

        // --- 地面別・ブースト補正(同じ) ---
        float accelMultiplier = 1f;
        float speedMultiplier = 1f;
        bool isKiller = false;
        if(driver != null) isKiller = driver.IsKiller();
        if (currentGroundTag == "Dirt" && boostTimer <= 0f && !isKiller)
        {
            accelMultiplier = dirtAccelMultiplier;
            speedMultiplier = dirtSpeedMultiplier;
        }

        if (boostTimer > 0f || killerTimer > 0f)
        {
            accelMultiplier *= boostAccelMultiplier;
            speedMultiplier *= boostSpeedMultiplier;
            boostTimer -= Time.fixedDeltaTime;
        }

        float maxAllowedSpeed = MaxSpeed * (1 + passiveNumList[1] * speedPower) * speedMultiplier;

        if (rb.linearVelocity.magnitude < maxAllowedSpeed)
        {
            Quaternion steerRotation = Quaternion.Euler(0f, currentSteer, 0f);

            Vector3 forwardFlat = transform.forward;
            forwardFlat.y = 0f;
            forwardFlat.Normalize();

            Vector3 forwardDir = steerRotation * forwardFlat;

            float motorPower =
                (netMotor < 0 ? MotorForce * (1 + passiveNumList[0] * accelerationPower) * 0.6f : MotorForce * (1 + passiveNumList[0] * accelerationPower)) * accelMultiplier;

            rb.AddForce(forwardDir * netMotor * motorPower, ForceMode.Acceleration);
        }

        // 速度表示など残す（rb.linearVelocity -> rb.velocity）
        float speed = rb.linearVelocity.magnitude * 3.6f;
        if (speedText != null && isMine && PlayerPrefs.GetInt("driverNum") != -1)
        {
            speedText.text = $"{speed:F1}";

            float angle = 43.6f - speed;
            speedMeter.transform.localRotation = Quaternion.Euler(0f, 0f, angle);
        }

        UpdateBoostEffect(speed);

        // 横滑り防止
        Vector3 localVel = transform.InverseTransformDirection(rb.linearVelocity);
        localVel.x *= 0.85f;
        rb.linearVelocity = transform.TransformDirection(localVel);

        // 車体回転
        if (rb.linearVelocity.magnitude > 0.1f)
        {
            float rotationSign = netMotor < 0 ? -1f : 1f;
            float turnAmount = steerInput * TurnSensitivity * rotationSign;
            Quaternion deltaRotation = Quaternion.Euler(0f, turnAmount, 0f);
            rb.MoveRotation(rb.rotation * deltaRotation);
        }

        if (inputUseItem)
        {
            inputUseItem = false;

            if (itemManager.GetItemNum() > 0)
            {
                RemoveUsedItem();
            }
        }

        // 重力補正
        if(killerTimer <= 0) rb.AddForce(Vector3.down * extraGravity, ForceMode.Acceleration);
    }

    //ゴール後に自動走行AIに切り替え
    public void ChangeToAutoDriver()
    {
        var result = resultUI.GetComponent<resultUI>();

        bool isChange = false;

        if (driver == null)
        {
            isChange = true;
        }
        else if(driver != null && driver.IsKiller())
        {
            isChange = true;
        }

        if(isChange)
        {
            //AIに切り替え
            var wpContainer = FindObjectOfType<WaypointContainer>();
            SetAI<AIDriver>(wpContainer);
        }
    }

    //中心点からの角度計算　ラップ判定
    public void UpdateAngle()
    {
        //現在の周回角度を取得
        int cur = (int)lapManager.NowAngle(transform.position);
        if (Mathf.Abs(cur - nowAngle) <= 10f && nowAngle < cur)
        {
            nowAngle = cur;
        }

        //ラップ判定 12度ごとにチェックポイントを通過したか
        var startObj = GameObject.Find("StartPos");
        int sector = Mathf.FloorToInt(nowAngle / 18f);
        if(sector > 0)
        {
            for(int i = 0;i < sector;i++)
            {
                if (flags[i] == false)
                {
                    if (i == sector - 1)
                    {
                        flags[i] = true;
                    }
                    else break;
                }
            }
        }

        int throughFlags = 0;
        foreach (var f in flags)
        {
            if (f) throughFlags++;
        }

        if(isMine) Debug.Log($"現在の角度：{cur}");

        if (throughFlags == flags.Length && 0 < cur && cur < 10)
        {
            isLapClear = true;
            for (int i = 0; i < flags.Length; i++)
            {
                flags[i] = false;
            }
        }
        else if(throughFlags < flags.Length && 350 < cur && cur < 360)
        {
            Debug.Log("逆走検知");
            isLapClear = false;
            lapCount--;
            for (int i = 0; i < flags.Length; i++)
            {
                flags[i] = true;
            }
        }
        else
        {
            isLapClear = false;
        }

        throughFlags = 0;
        foreach (var f in flags)
        {
            if (f) throughFlags++;
        }
        flagsCount = throughFlags;

        if (PhotonNetwork.IsConnected) 
            photonView.RPC("RPC_SyncFlagsCount", RpcTarget.All, flagsCount, photonView.ViewID); 
    }

    private void DetectAndUpdateGuideUI()
    {
        if (!isMine) return;
        if (PhotonNetwork.IsConnected && !photonView.IsMine) return;

        bool keyboardWASDPressed =
            Keyboard.current != null &&
            (Keyboard.current.wKey.isPressed || Keyboard.current.aKey.isPressed ||
             Keyboard.current.sKey.isPressed || Keyboard.current.dKey.isPressed);

        bool keyboardArrowPressed =
            Keyboard.current != null &&
            (Keyboard.current.upArrowKey.isPressed || Keyboard.current.downArrowKey.isPressed ||
             Keyboard.current.leftArrowKey.isPressed || Keyboard.current.rightArrowKey.isPressed);

        bool gamepadUsed = false;
        if (Gamepad.current != null)
        {
            // スティック/十字キー/ボタン どれか動いたらGamepad扱い
            Vector2 ls = Gamepad.current.leftStick.ReadValue();
            Vector2 dp = Gamepad.current.dpad.ReadValue();
            gamepadUsed =
                ls.sqrMagnitude > 0.01f ||
                dp.sqrMagnitude > 0.01f ||
                Gamepad.current.buttonSouth.isPressed ||
                Gamepad.current.buttonEast.isPressed ||
                Gamepad.current.buttonWest.isPressed ||
                Gamepad.current.buttonNorth.isPressed ||
                Gamepad.current.leftShoulder.isPressed ||
                Gamepad.current.rightShoulder.isPressed ||
                Gamepad.current.leftTrigger.ReadValue() > 0.1f ||
                Gamepad.current.rightTrigger.ReadValue() > 0.1f;
        }

        // 優先順位：Gamepad > Arrow > WASD（好みで変更OK）
        if (gamepadUsed)
        {
            SetLastDevice(LastInputDevice.Gamepad);
        }
        else if (keyboardArrowPressed)
        {
            SetLastDevice(LastInputDevice.KeyboardArrow);
        }
        else if (keyboardWASDPressed)
        {
            SetLastDevice(LastInputDevice.KeyboardWASD);
        }

        // 入力が一定時間ないならデフォルトに戻す（不要なら guideResetDelay=0 に）
        if (guideResetDelay > 0f && Time.time - lastInputTime > guideResetDelay)
        {
            if (lastDevice != LastInputDevice.KeyboardWASD)
            {
                lastDevice = LastInputDevice.KeyboardWASD;
                ApplyGuideUI(lastDevice);
            }
        }
    }

    private void SetLastDevice(LastInputDevice device)
    {
        if (lastDevice == device) // 同じなら時刻更新だけ
        {
            lastInputTime = Time.time;
            // アイテムUIだけ状況で変わるので再反映してもOK
            ApplyItemGuideUI(device);
            return;
        }

        lastDevice = device;
        lastInputTime = Time.time;
        ApplyGuideUI(device);
    }

    private void ApplyGuideUI(LastInputDevice device)
    {
        // 操作ガイド
        SetActiveSafe(WASDGuide, device == LastInputDevice.KeyboardWASD);
        SetActiveSafe(arrowGuide, device == LastInputDevice.KeyboardArrow);
        SetActiveSafe(controllerGuide, device == LastInputDevice.Gamepad);

        // アイテムガイド（操作ガイド更新のたびに反映）
        ApplyItemGuideUI(device);
    }

    private void ApplyItemGuideUI(LastInputDevice device)
    {
        // ガイド表示OFFなら表示処理をしない
        if (PlayerPrefs.GetInt(OptionPrefs.GUIDE_ENABLED, 0) == 0) return;

        bool itemAvailable = itemManager != null && itemManager.GetItemNum() > 0;

        // itemが無ければ両方消す
        if (!itemAvailable)
        {
            SetActiveSafe(keyboardItemGuide, false);
            SetActiveSafe(controllerItemGuide, false);
            return;
        }

        // itemがある場合のみ表示
        bool keyboard = (device == LastInputDevice.KeyboardWASD || device == LastInputDevice.KeyboardArrow);
        bool pad = (device == LastInputDevice.Gamepad);

        SetActiveSafe(keyboardItemGuide, keyboard);
        SetActiveSafe(controllerItemGuide, pad);
    }

    private void SetActiveSafe(GameObject go, bool active)
    {
        if (go == null) return;
        if (go.activeSelf == active) return;
        go.SetActive(active);
    }

    public void DecideStartTime()
    {
        if(!PhotonNetwork.IsMasterClient) return;

        //開始時刻をマスターが決定、カスタムプロパティに登録
        startTime = PhotonNetwork.Time;
        var prop = new Hashtable();
        prop["raceStartTime"] = startTime;
        PhotonNetwork.CurrentRoom.SetCustomProperties(prop);
    }

    public void UpdateTimer()
    {
        if(isTutorial) return;
        //オンラインならサーバー基準で計測
        if (PhotonNetwork.IsConnected && PhotonNetwork.CurrentRoom.CustomProperties.TryGetValue("raceStartTime", out object startTimeObj))
        {
            double startTime = (double)startTimeObj;
            timer = PhotonNetwork.Time - startTime;
        }
        else if(!PhotonNetwork.IsConnected)
        {
            //プレイヤー基準で計測
            if(playerCar == null)
            {
                foreach(var car in cars)
                {
                    if(car.isMine) playerCar = car;
                }
            }
            
            if(isMine) timer += Time.deltaTime;
            else timer = playerCar.GetTimer();
        }

        //Debug.Log($"Timer: {timer}");
    }

    public double GetTimer()
    {
        return timer;
    }

    [PunRPC]
    public void RPC_StateToDrive()
    {
        StateToDrive();

        DecideStartTime();
    }

    //状態を運転に
    public void StateToDrive()
    {
        state = State.Drive;

        cars = FindObjectsOfType<CarController>();
        isGetCars = true;
    }

    [PunRPC]
    public void RPC_SetStartPos(Vector3 pos)
    {
        SetStartPos(pos);
    }

    //状態を運転に
    public void SetStartPos(Vector3 pos)
    {
        //一度のみ実行
        if (isSetStartPos) return;

        transform.position = pos;
        isSetStartPos = true;

        var startPosObj = GameObject.Find("StartPos");
        if (startPosObj != null)
        {
            transform.rotation = startPosObj.transform.rotation;
        }
    }

    [PunRPC]
    public void RPC_UpdateRank()
    {
        UpdateRank();
    }

    //順位更新
    public void UpdateRank()
    {
        if (cars == null) return;
        //観戦者の時に作動しないように RPCがバッファされてるのでここで処理止める
        if (PlayerPrefs.GetInt("isMonitor") == 1) return;

        if (rankText == null) return;
        if (isLapClear) return;
        if(isRaceClear) return;

        //全カートの角度とラップ数を取得　比較して順位を決定
        currentRank = 1;
        
        Debug.Log($" === {cars.Length}台のカートで順位計算 === ");
        foreach (var car in cars)
        {
            if (car == this) continue;

            string lap = "", wp = "", angle = "";

            //ラップ数計算
            if (car.GetLapCount() < GetLapCount()) continue;

            if(car.GetLapCount() > GetLapCount())
            {
                currentRank++;
                continue;
            }

            //最短ウェイポイント計算
            int otherNearIdx = car.GetNearllyWpIdx(car.transform.position);
            int myNearIdx = GetNearllyWpIdx(transform.position);

            if (otherNearIdx < myNearIdx) continue;
            if (otherNearIdx > myNearIdx)
            {
                currentRank++;
                continue;
            }

            //次点ウェイポイントまでの距離計算
            if (car.GetlenUntilNextWp(otherNearIdx) < GetlenUntilNextWp(myNearIdx))
            {
                currentRank++;
                continue;
            }

            Debug.LogError($"順位決定不可：{car.GetName()}");
            Debug.Log($"");

            continue;

            if (car.GetLapCount() < lapCount) continue;

            //ウェイポンとが同じならラップ数が多いほうが上位
            if (car.GetLapCount() > lapCount)
            {
                lap = car.GetLapCount().ToString() + " > " + lapCount.ToString();
                currentRank++;
            }
            //ウェイポイントが進んでるほうが上位
            else if (car.GetNearllyWpIdx(car.transform.position) > GetNearllyWpIdx(transform.position))
            {
                wp = car.GetNearllyWpIdx(car.transform.position).ToString() + " > " + GetNearllyWpIdx(transform.position).ToString();
                currentRank++;
            }
            //角度が大きいほうが上位
            else
            {
                if (lapManager.NowAngle(car.transform.position) > lapManager.NowAngle(transform.position))
                {
                    angle = lapManager.NowAngle(car.transform.position).ToString("F1") + " > " + lapManager.NowAngle(transform.position).ToString("F1");
                    currentRank++;
                }
            }
        }
    }

    public void UpdateRankUI()
    {
        //エンジニアは処理なし　シングルプレイで反応しないように
        if(PlayerPrefs.GetInt("engineerNum") != -1) return;

        if (isLapClear) return;
        if (isRaceClear) return;

        //UIに反映
        if (lapCount == maxLaps - 1 && lapManager.NowAngle(transform.position) >= 350f)
        {
            //ゴール直前なら表示なし
            rankText.text = "";
        }
        else
        {
            if (currentRank == 1) rankText.text = "1st";
            else if (currentRank == 2) rankText.text = "2nd";
            else if (currentRank == 3) rankText.text = "3rd";
            else rankText.text = currentRank + "th";
        }
    }

    public int GetLapCount()
    {
        return lapCount;
    }

    public void UpdateTimerUI()
    {
        //点滅
        if(blinkTimer > 0f)
        {
            blinkTimer -= Time.deltaTime;
            timerText.color = Color.yellow;

            if (Time.time >= nextBlinkTime)
            {
                timerText.enabled = !timerText.enabled;
                nextBlinkTime = Time.time + lapBlinkInterval;
            }

            //ラップタイムをしばらく点滅で表示するため処理はここで終了
            return;
        }
        else
        {
            blinkTimer = 0f;
            timerText.enabled = true;
            timerText.color = Color.white;
        }

        int minutes = (int)(timer / 60);
        int seconds = (int)(timer % 60);
        int milliseconds = (int)((timer * 1000) % 1000);

        timerText.text = string.Format("{0:00}:{1:00}:{2:000}", minutes, seconds, milliseconds);
    }

    // 地面の種類をRaycastで検出
    private void UpdateGroundType()
    {
        Ray ray = new Ray(transform.position + Vector3.up * 0.5f, Vector3.down);
        if (Physics.Raycast(ray, out RaycastHit hit, raycastLength, groundMask))
        {
            currentGroundTag = hit.collider.tag;
        }
        else
        {
            currentGroundTag = "Default";
        }

        //Debug.Log($"Ground Tag: {currentGroundTag}");
    }

    private void UpdateStun()
    {
        stunElapsed += Time.deltaTime;

        float t = Mathf.Clamp01(stunElapsed / stunTime);
        float ease = stunEaseCurve.Evaluate(t);
        float angle = ease * stunSpinAngle;

        // ローカル回転で演出
        bodyMesh.transform.localRotation = stunStartLocalRotation * Quaternion.Euler(0f, angle, 0f);

        // ---- 速度減衰 ----
        Vector3 velocity = rb.linearVelocity;
        // y軸は計算しない
        velocity.y = rb.linearVelocity.y;
        // スタン軽減によって減速率を軽減
        velocity.x *= stunBrakeFactor * (1 - passiveNumList[2] * antiStunPower);
        velocity.z *= stunBrakeFactor * (1 - passiveNumList[2] * antiStunPower);

        float speed = velocity.magnitude;

        if (speed > stunMinSpeed)
        {
            rb.linearVelocity = velocity;
        }

        stunTime -= Time.deltaTime;

        if (stunTime <= 0f)
        {
            // 見た目だけ元に戻す
            bodyMesh.transform.localRotation = stunStartLocalRotation;
            state = State.Drive;
        }
    }

    private void LateUpdate()
    {
        //テスト　ラップ数を頭上に表示
        //SetName(lapCount.ToString());

        if (stunEffectInstance == null) return;
        UpdateStunEffectTransform();
    }

    private void UpdateStunEffectTransform()
    {
        if (stunEffectInstance == null) return;

        Vector3 horizontalOffset = transform.TransformVector(new Vector3(stunEffectOffset.x, 0f, stunEffectOffset.z));
        stunEffectInstance.transform.position = transform.position + horizontalOffset + Vector3.up * stunEffectOffset.y;
        stunEffectInstance.transform.rotation = Quaternion.Euler(0f, 0f, 0f);
        stunEffectInstance.transform.localScale = Vector3.one;
    }


    //接触がコインならカウント
    private void OnTriggerEnter(Collider other)
    {
        if (tag == "Coin")
        {
            Coin coinScript = other.GetComponent<Coin>();
            if (coinScript.isCnt == false)
            {
                coinCnt++;
                coinScript.isCnt = true;

                //自分以外ならテキストの更新はしない
                if (PhotonNetwork.IsConnected && !photonView.IsMine || driver != null) return;
                //coinText.text = $"{coinCnt:D4}";
            }
        }
    }

    private void OnCollisionEnter(Collision collision)
    {
        //オンラインの当たり判定
        if (PhotonNetwork.InRoom == false) return;
        if (!photonView.IsMine) return;

        string tag = collision.gameObject.tag;

        Debug.Log($"OnCollisionEnter: {tag}");

        switch (tag)
        {
            case "RocketGreen":
            case "RocketRed":

                //削除命令
                var pv = collision.gameObject.GetComponent<PhotonView>();
                if (pv != null)
                {
                    pv.RPC("RPC_HitAndDestroy", RpcTarget.All);
                }

                //スタンタイプの設定
                StunType stunType = StunType.Light;

                //スタン状態付与
                SetStun(stunType, pv.Owner.NickName, tag);

                break;
        }
    }

    //ドライバーをAIに変更
    public void SetAI<T>(WaypointContainer waypointContainer = null)
        where T : Component , IDriver
    {
        Debug.Log("SET AI");

        if (driver == null)
        {
            var aiComp = gameObject.AddComponent<T>();
            driver = aiComp;

            if (waypointContainer != null)
            {
                aiComp.SetWaypointContainer(waypointContainer);
            }
            else
            {
                var wc = FindObjectOfType<WaypointContainer>();
                if (wc != null)
                    aiComp.SetWaypointContainer(wc);
                else
                    Debug.LogWarning("[CarController] SetAI: WaypointContainer が見つかりません。実行時に経路をセットしてください。");
            }

            if(driver is Killer killer)
            {
                killer.SetCurrentIdx(GetKillerStartIdx());
            }
        }
    }

    public void SetName(string s)
    {
        Transform labelTransform = transform.Find("NameLabel");
        if (labelTransform != null)
        {
            TextMeshPro nameLabel = labelTransform.GetComponent<TextMeshPro>();
            if (nameLabel != null)
            {
                nameLabel.text = s;
            }
        }
    }

    public string GetName()
    {
        string ret = string.Empty;

        Transform labelTransform = transform.Find("NameLabel");
        if (labelTransform != null)
        {
            TextMeshPro nameLabel = labelTransform.GetComponent<TextMeshPro>();
            if (nameLabel != null)
            {
                ret = nameLabel.text;
            }
        }

        Debug.Log($"GetName: {ret}");
        return ret;
    }

    public void SetEngineer(Engineer en)
    {
        engineer = en;
    }

    public Engineer GetEngineer()
    {
        return engineer;
    }

    public void SendParts(PartsID id)
    {
        //ゴール後は処理なし
        if(isRaceClear)
        {
            return;
        }

        //シングルプレイ時の操作
        if (!PhotonNetwork.IsConnected && isMine)
        {
            if(PlayerPrefs.GetInt("driverNum") != -1) // ドライバーの時
            {
                itemManager.SpawnItem(id);
            }
            else // エンジニアの時
            {
                engineer.RPC_SpawnParts(id); // パーツ生成
            }
                return;
        }

        if (!photonView.IsMine) return;

        PhotonView target = PhotonView.Find(pairViewID);

        if (target == null)
        {
            Debug.Log("target is null");
            TryPairPlayers();
        }
        if (pairPlayer == null) Debug.Log("pair player is null");
        if (photonView == null) Debug.Log("photon view is null");

        // ペアのエンジニア画面にアイテムパーツを生成
        target.RPC("RPC_SpawnParts", pairPlayer, id);
    }

    // 使用するアイテムを検索、キューから削除
    public void RemoveUsedItem()
    {
        // 使用するアイテムIDを取り出す
        PartsID usedId = (PartsID)itemManager.ItemDequeue(true);

        // ----------------------------
        // エンジニア側に使用したアイテムパーツ削除を通知
        // ----------------------------
        if (PhotonNetwork.IsConnected && photonView.IsMine)
        {
            PhotonView target = PhotonView.Find(pairViewID);
            if (target != null)
            {
                target.RPC("RPC_RemoveUsedItem", pairPlayer, usedId);
            }
        }
        else if(!PhotonNetwork.IsConnected)
        {
            RPC_RemoveItem(usedId);
        }

        ApplyItemGuideUI(lastDevice);
    }

    // アイテムを獲得可能か検証
    public bool CanGetItem()
    {
        if (partsNum >= MAXITEMNUM)
        {
            return false;
        }
        else
        {
            return true;
        }
    }

    //カメラの設定
    public void SetCamera()
    {
        cameraController = Camera.main.GetComponent<CameraController>();
        if (cameraController != null)
            cameraController.SetTarget(transform);
    }

    private void UpdateBoostEffect(float speed)
    {
        if (!isMine || boostEffectPrefab == null)
        {
            return;
        }

        bool shouldPlay = speed > 100f;

        if (shouldPlay)
        {
            if (boostEffectInstance == null)
            {
                Transform cameraTransform = cameraController != null ? cameraController.transform : Camera.main?.transform;
                if (cameraTransform == null)
                {
                    return;
                }

                boostEffectInstance = Instantiate(boostEffectPrefab, cameraTransform);
                boostEffectInstance.transform.localPosition = boostEffectLocalPosition;
                boostEffectInstance.transform.localRotation = Quaternion.Euler(boostEffectLocalRotation);
                boostEffectInstance.transform.localScale = new Vector3(0.3f, 0.3f, 0.3f);
                boostEffectParticle = boostEffectInstance.GetComponent<ParticleSystem>();
            }

            if (boostEffectParticle != null && !boostEffectParticle.isPlaying)
            {
                boostEffectParticle.Play();
            }
        }
        else
        {
            ClearBoostEffect();
        }
    }

    private void ClearBoostEffect()
    {
        if (boostEffectInstance != null)
        {
            Destroy(boostEffectInstance);
            boostEffectInstance = null;
            boostEffectParticle = null;
        }
    }

    // ============================
    // Fire Effect (左右2個・使い回し版)
    // ============================

    public void PlayFireEffect()
    {
        if (fireEffectPrefab == null) return;

        EnsureFireFxInstances();

        // 左右ローカル座標（右はX反転）
        Vector3 leftPos = FireFxLocalPos;
        Vector3 rightPos = new Vector3(-FireFxLocalPos.x, FireFxLocalPos.y, FireFxLocalPos.z);

        fireFxL.transform.localPosition = leftPos;
        fireFxR.transform.localPosition = rightPos;

        RestartParticle(fireFxL);
        RestartParticle(fireFxR);

        if (fireEffectCo != null) StopCoroutine(fireEffectCo);
        fireEffectCo = StartCoroutine(Co_StopFireEffect());
    }

    private void EnsureFireFxInstances()
    {
        if (fireFxL == null)
        {
            fireFxL = Instantiate(fireEffectPrefab, transform);
            fireFxL.transform.localRotation = Quaternion.identity;
            fireFxL.transform.localScale = Vector3.one;
            fireFxL.gameObject.name = "FireFx_L";
        }

        if (fireFxR == null)
        {
            fireFxR = Instantiate(fireEffectPrefab, transform);
            fireFxR.transform.localRotation = Quaternion.identity;
            fireFxR.transform.localScale = Vector3.one;
            fireFxR.gameObject.name = "FireFx_R";
        }
    }

    private void RestartParticle(ParticleSystem ps)
    {
        if (ps == null) return;

        // Rendererを必ずON（残像対策）
        var r = ps.GetComponent<ParticleSystemRenderer>();
        if (r != null) r.enabled = true;

        // 完全クリアして再生
        ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        ps.time = 0f;
        ps.Play(true);
    }

    private System.Collections.IEnumerator Co_StopFireEffect()
    {
        yield return new WaitForSeconds(fireEffectDuration);

        StopAndHideParticle(fireFxL);
        StopAndHideParticle(fireFxR);

        fireEffectCo = null;
    }

    private void StopAndHideParticle(ParticleSystem ps)
    {
        if (ps == null) return;

        // 完全停止＆クリア（「他人だけ残る」対策で最優先）
        ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

        // RendererもOFF（トレイル/サブエミッタ残りの見た目対策）
        var r = ps.GetComponent<ParticleSystemRenderer>();
        if (r != null) r.enabled = false;
    }

    private void StopFireEffectImmediate()
    {
        if (fireEffectCo != null)
        {
            StopCoroutine(fireEffectCo);
            fireEffectCo = null;
        }

        StopAndHideParticle(fireFxL);
        StopAndHideParticle(fireFxR);
    }

    public void HiddenUI()
    {
        //UIの非表示
        if(PlayerPrefs.GetInt("driverNum") != -1) hidenCanvas.SetActive(false);
    }

    public void SetKiller()
    {
        //キラーに切り替え
        var wpContainer = FindObjectOfType<WaypointContainer>();
        SetAI<Killer>(wpContainer);
        killerTimer = GetKillerTime();

        rb.useGravity = false;

        //エフェクト有効化
        killerEffect.SetActive(true);
    }

    public void UpdateKiller()
    {
        killerTimer -= Time.deltaTime;

        //高さを制限
        transform.position = new Vector3(transform.position.x, 1f, transform.position.z);

        //キラー終了処理
        if (killerTimer > 0f && currentRank == 1) killerTimer = 0f;

        if (killerTimer <= 0f)
        {
            ResetKiller();
        }
        else
        {
            Debug.Log($"Killer Time Remaining : {killerTimer:F2} sec");
        }
    }

    public void ResetKiller()
    {
        if(driver != null && driver.IsKiller())
        {
            driver = null;
            Destroy(GetComponent<Killer>());
            killerTimer = 0f;

            Debug.Log("Killer Reset");
        }
        else
        {
            Debug.Log("Killer Reset Filed");

            //ドライバーのデータ型を確認
            Debug.Log($"Driver Type : {driver.GetType()}");
        }

        rb.useGravity = true;

        //エフェクト無効化
        killerEffect.SetActive(false);
    }

    public float GetKillerTime()
    {
        float ret = 0f;

        //順位に応じて時間を調整
        if (currentRank == 1) ret = 0.5f;
        else
        {
            ret = 7f;
        }

        return ret;
    }

    //キラー用スタート地点の取得
    public int GetKillerStartIdx()
    {
        int ret = 0;

        //ウェイポイントの中から次に行きそうな場所を取得
        var wpc = FindObjectOfType<WaypointContainer>();
        
        float nowAngle = lapManager.NowAngle(transform.position);
        Vector3 offsetY = new Vector3(0,1,0);
        Vector3 nowPos = transform.position + offsetY;

        float minLen = 1e6f;
        
        // 子オブジェクトを順番に取得
        Transform wpcTransform = wpc.transform;

        for (int i = wpcTransform.childCount - 1; i >= 0; i--)
        {
            Vector3 wpPos = wpcTransform.GetChild(i).position + offsetY;
            RaycastHit[] hitList = Physics.RaycastAll(nowPos, (wpPos - nowPos).normalized, (wpPos - nowPos).magnitude);

            bool isHitWall = false;
            foreach(var hitp in hitList)
            {
                //Wallに遮られているものは除外
                if (hitp.collider.CompareTag("Wall")) isHitWall = true;
            }
            if (isHitWall) continue;

            if (lapManager.NowAngle(wpPos) < nowAngle)
            {
                debugDrawLineList.Add((wpPos, nowPos));
                return (i + 1) % wpcTransform.childCount;
            }

            //最も近いもの
            if ((nowPos - wpPos).magnitude < minLen)
            {
                minLen = (nowPos - wpPos).magnitude;
                ret = i;
            }
        }

        //角度でリターンしなければ距離ベースで計算
        debugDrawLineList.Add((wpcTransform.GetChild(ret).position + offsetY,nowPos));
        return ret;
    }

    //Wallオブジェクトに遮らていない最近点を返す
    public int GetNearllyWpIdx(Vector3 pos)
    {
        int ret = 99;

        float nowAngle = lapManager.NowAngle(transform.position);
        Vector3 offsetY = new Vector3(0, 1, 0);
        Vector3 nowPos = transform.position + offsetY;

        float minLen = 1e6f;

        // 子オブジェクトを順番に取得
        Transform wpcTransform = wpc.transform;

        for (int i = wpcTransform.childCount - 1; i >= 0; i--)
        {
            Vector3 wpPos = wpcTransform.GetChild(i).position + offsetY;
            RaycastHit[] hitList = Physics.RaycastAll(nowPos, (wpPos - nowPos).normalized, (wpPos - nowPos).magnitude);

            bool isHitWall = false;
            foreach (var hitp in hitList)
            {
                //Wallに遮られているものは除外
                if (hitp.collider.CompareTag("Wall"))
                {
                    isHitWall = true;
                    break;
                }
            }

            if (isHitWall) continue;

            //最も近いもの
            if ((nowPos - wpPos).magnitude < minLen)
            {
                minLen = (nowPos - wpPos).magnitude;
                ret = i;
            }
        }

        Debug.DrawLine(nowPos, wpcTransform.GetChild(ret).position, Color.blue);

        return ret;
    }

    public float GetlenUntilNextWp(int nearIdx)
    {
        float len = 0f;

        //壁にレイを飛ばす
        Transform wpcTransform = wpc.transform;
        Vector3 nowPos = transform.position;
        Vector3 wpPos = wpcTransform.GetChild(nearIdx).position;
        Vector3 nextWpPos = wpcTransform.GetChild((nearIdx+1) % wpcTransform.childCount).position;
        RaycastHit[] hitList = Physics.RaycastAll(nowPos, (wpPos - nowPos).normalized, (wpPos - nowPos).magnitude);

        bool isHitWall = false;
        foreach (var hitp in hitList)
        {
            //Wallに遮られているものは除外
            if (hitp.collider.CompareTag("Wall"))
            {
                isHitWall = true;
                break;
            }
        }

        //壁に遮られていれば迂回距離を計算
        if (isHitWall)
        {
            len = (nowPos - wpPos).magnitude;
            len += (wpPos - nextWpPos).magnitude;

            Debug.DrawLine(nowPos, wpPos, Color.green);
            Debug.DrawLine(wpPos, nextWpPos, Color.green);
        }
        else
        {
            len = (nowPos - nextWpPos).magnitude;

            Debug.DrawLine(nowPos, nextWpPos, Color.green);
        }

        return len;
    }

    public int GetCurrentRank() => currentRank;

    public override void OnPlayerLeftRoom(Player otherPlayer)
    {
        //切断がペアならメニューへ戻る
        if (otherPlayer.CustomProperties.TryGetValue("engineerNum", out var en) && (int)en == PlayerPrefs.GetInt("driverNum")
            && otherPlayer.CustomProperties.TryGetValue("isRaceClear", out var flag) && (bool)flag == false)
        {
            Debug.Log("ペアが切断したのでメニューへ戻ります");
            SceneManager.LoadScene("menu");
        }
    }

    public override void OnPlayerPropertiesUpdate(Player targetPlayer, ExitGames.Client.Photon.Hashtable changed)
    {
        Debug.Log("CALL BACK");
        TryPairPlayers();
    }

    public override void OnRoomPropertiesUpdate(Hashtable propertiesThatChanged)
    {
        //自身のViewIDを登録
        PhotonView pv = GetComponent<PhotonView>();
        if (pv.IsMine)
        {
            PhotonNetwork.LocalPlayer.SetCustomProperties(new ExitGames.Client.Photon.Hashtable { { "PlayerViewID", pv.ViewID } });
            Debug.Log($"ID登録：{pv.ViewID}");
        }

        var prop = PhotonNetwork.CurrentRoom.CustomProperties;
        if (maxLaps != -1 && prop.TryGetValue("lapCnt", out var lapCnt) && lapCnt is int)
        {
            lapManager.SetMaxLaps((int)lapCnt);
            maxLaps = (int)lapCnt;

            Debug.Log($"SET MAX LAP : {maxLaps}");
        }
    }

    [PunRPC]
    public void RPC_UpdateRankUI(string name, double time, int id, int pairId)
    {
        var result = resultUI.GetComponent<resultUI>();
        result.UpdateRankUI(name, time, id, pairId);
    }

    [PunRPC]
    public void RPC_EnqueueItem(PartsID id)
    {
        Debug.Log("Enqueue Item Request");
        itemManager.ItemEnqueue((int)id);
        ApplyItemGuideUI(lastDevice);
    }

    [PunRPC]
    public void RPC_RemoveItem(PartsID id)
    {
        Debug.Log("Remove Item Request");
        itemManager.Remove((int)id);
        int? nextItem = itemManager.ItemDequeue(false);
    }

    [PunRPC]
    public void RPC_UseItem(PartsID id)
    {
        Debug.Log("Remove Item Request");
        itemManager.Remove((int)id);
        int? nextItem = itemManager.ItemDequeue(false);
    }

    [PunRPC]
    public void RPC_AddPartsNum()
    {
        Debug.Log("Add PartsNum Request");
        AddPartsNum();
    }

    [PunRPC]
    public void RPC_RemovePartsNum()
    {
        Debug.Log("Substract PartsNum Request");
        SubstractPartsNum();
    }

    [PunRPC]
    public void RPC_SetPassiveState(PartsID id, bool isAdd)
    {
        Debug.Log("PassiveState Request");
        SetPassiveState(id, isAdd);
    }

    [PunRPC]
    public void RPC_NotifLoadFinish()
    {
        if(photonView.IsMine) isLoading = false;
    }

    [PunRPC]
    public void RPC_SyncFlagsCount(int flagsCnt, int id)
    {
        if (photonView.ViewID != id) return;
        flagsCount = flagsCnt;
    }

    [PunRPC]
    public void RPC_SyncLapCount(int lapCnt , int id)
    {
        if (photonView.ViewID != id) return;
        lapCount = lapCnt;
    }
}