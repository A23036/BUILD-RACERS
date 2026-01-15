using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using Photon.Pun;
using TMPro;
using Photon.Realtime;

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

public enum BoostType // スタンの重さ
{
    Short,
    Long,
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

    [Header("UI")]
    [SerializeField] private TextMeshProUGUI speedText;  // 速度表示テキスト
    [SerializeField] private TextMeshProUGUI coinText;  // コイン枚数表示テキスト
    [SerializeField] private TextMeshProUGUI itemText;  // アイテム表示テキスト
    [SerializeField] private TextMeshProUGUI lapText;  // 周回数表示テキスト
    [SerializeField] private TextMeshProUGUI rankText;  // 順位表示テキスト
    [SerializeField] private TextMeshProUGUI timerText;  // タイム表示テキスト

    [Header("保持なアイテムの数")]
    [SerializeField] private int MAXITEMNUM = 5;

    private CameraController cameraController;
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

    [Header("パッシブアイテム用パラメータ")]
    [SerializeField] private float accelerationPower = 0.5f;
    [SerializeField] private float speedPower = 0.05f;
    [SerializeField] private float antiStunPower = 0.2f;

    private int[] passiveNumList = { 0, 0, 0 }; // パッシブ強化状態

    [Header("回転演出用パラメータ")]
    [SerializeField] private AnimationCurve stunEaseCurve;
    [SerializeField] private float stunMinSpeed = 2.0f;
    private float stunSpinAngle = 360f; // 回転量

    private bool isSetStartPos = false;

    //周回判定用
    private LapManager lapManager;
    private int lapCount = -1;
    private int maxLaps = 0;
    private float nowAngle = 0f;
    private bool[] flags;
    private bool isLapClear = false;

    //オフライン用のフラグ　BOTとの区別用
    public bool isMine = false;

    //リザルトUI ゴールしたら有効化
    private GameObject resultUI;

    //現在の順位
    private int currentRank = 1;

    //スタートからの経過秒数
    private float timer;

    //ラップタイムが点滅する時間
    [SerializeField] private float lapBlinkTime = 3f;
    private float blinkTimer = 0f;
    private float nextBlinkTime = 0f;
    [SerializeField]private float lapBlinkInterval = .25f;


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

    public void SetPassiveState(PartsID id, bool isAdd)
    {
        switch (id)
        {
            case PartsID.Acceleration:
                if (isAdd)
                {
                    passiveNumList[0]++;
                }
                else
                {
                    passiveNumList[0]--;
                }
                break;
            case PartsID.Speed:
                if (isAdd)
                {
                    passiveNumList[1]++;
                }
                else
                {
                    passiveNumList[1]--;
                }
                break;
            case PartsID.AntiStun:
                if (isAdd)
                {
                    passiveNumList[2]++;
                }
                else
                {
                    passiveNumList[2]--;
                }
                break;
            default:
                break;
        }

        Debug.Log("PassiveState: Acceleration: " + passiveNumList[0] + " Speed: " + passiveNumList[1] + " AntiStun: " + passiveNumList[2]);
    }
    
    public void SetBoost(BoostType boostType)
    {
        // ブーストの強さに応じてブースト時間をセット
        switch(boostType) {
            case BoostType.Short:
                boostTimer = ShortBoostTime;
                break;
            case BoostType.Long:
                boostTimer = LongBoostTime;
                break;
            default:
                break;
        }
    }

    public void SetStun(StunType type)
    {
        if(state == State.Stun) return;

        // スタン状態をセット
        state = State.Stun;

        // スタンの強さに応じてスタン時間をセット
        switch(type) {
            case StunType.Light:
                stunTime = LightStunTime * (1 - passiveNumList[2] * antiStunPower);// パッシブ量に応じて軽減
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
            default:
                break;
        }

        // ----- 回転初期化 -----
        stunElapsed = 0f;
        stunStartLocalRotation = bodyMesh.transform.localRotation;

        Debug.Log($"SET STAN : {GetName()}");
    }
    　
    private void Awake()
    {
        Debug.Log("AWAKE");

        //初期状態は停止状態
        state = State.Stop;

        driverNum = PlayerPrefs.GetInt("driverNum");

        PhotonView pv = GetComponent<PhotonView>();

        if(photonView.IsMine && PlayerPrefs.GetInt("driverNum") != -1) PhotonNetwork.LocalPlayer.SetCustomProperties(new ExitGames.Client.Photon.Hashtable { { "PlayerViewID", pv.ViewID } });

        //ジョイスティック取得
        var joystick = GameObject.Find("Floating Joystick");
        if(joystick != null) variableJoystick = joystick.GetComponent<Joystick>();

        // テキストの設定
        speedText = InitText(speedText, "SpeedText");
        coinText = InitText(coinText, "coinText");
        itemText = InitText(itemText, "ItemText");
        lapText = InitText(lapText, "LapText");
        rankText = InitText(rankText, "RankText");
        timerText = InitText(timerText, "TimerText");

        rb = GetComponent<Rigidbody>();
        rb.centerOfMass = new Vector3(0f, -1.0f, 0f);
        rb.interpolation = RigidbodyInterpolation.Interpolate;

        bodyMesh = gameObject.transform.Find("BodyMesh").gameObject;

        itemManager = GetComponent<ItemManager>();

        lapManager = GameObject.Find("LapManager").GetComponent<LapManager>();
        maxLaps = lapManager.GetMaxLaps();

        //仮想的なチェックポイント
        flags = new bool[3];
        for(int i = 0;i < flags.Length;i++)
        {
            flags[i] = true;
        }

        //シーンマネージャー取得
        if(PhotonNetwork.IsConnected)
        {
            var sceneManager = FindObjectOfType<playScene>();
            if (sceneManager != null)
            {
                resultUI = sceneManager.GetResultUI();
                resultUI.SetActive(false);
            }
        }
        else
        {
            var sceneManager = FindObjectOfType<singlePlayScene>();
            if (sceneManager != null)
            {
                resultUI = sceneManager.GetResultUI();
                resultUI.SetActive(false);
            }
        }
    }

    private TextMeshProUGUI InitText(TextMeshProUGUI tmpro, string tag)
    {
        if (tmpro == null)
        {
            var text = GameObject.FindWithTag(tag);
            if (text != null)
            {
                tmpro = text.GetComponent<TextMeshProUGUI>();
            }
            else
                tmpro = FindObjectOfType<TextMeshProUGUI>();
        }

        return tmpro;
    }

    private void TryPairPlayers()
    {
        // ペアを発見済みの場合、処理を行わない
        if (pairViewID != -1) return;

        Player[] players = PhotonNetwork.PlayerList;

        // ネットワークに接続中のplayerを一人ずつ調査
        foreach (var p in players)
        {
            // ドライバーはcontinue(エンジニアのみ探す)
            int e = p.CustomProperties["engineerNum"] is int en ? en:-1;
            if (e == -1) continue;
            
            // 自身と同番号のエンジニアを探す
            if (e == PlayerPrefs.GetInt("driverNum"))
            {
                // PlayerViewID が設定済みならpairViewIDに保存
                if (p.CustomProperties.ContainsKey("PlayerViewID"))
                {
                    pairViewID = p.CustomProperties["PlayerViewID"] is int pairViewId ? pairViewId : -1;
                    pairPlayer = p;
                    Debug.Log("FOUND PAIR! pairID:" + pairViewID);
                }
                else
                {
                    Debug.Log("FOUND PAIR BUT PlayerViewID is not set.");
                }
                break;
            }
        }

        if (pairPlayer == null && driver == null)
        {
            Debug.Log("Pair is null");
        }
    }

    // 入力設定
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
        useItemAction.AddBinding("<Touchscreen>/primaryTouch/tap");
        useItemAction.Enable();

        base.OnEnable();
    }

    private void OnDisable()
    {
        throttleAction?.Disable();
        brakeAction?.Disable();
        steerAction?.Disable();
        useItemAction?.Disable();

        base.OnDisable();
    }

    private void Update()
    {
        //時間計測
        if (state == State.Drive)
        {
            timer += Time.deltaTime;
        }

        // 操作できない状態は入力を取らない
        if (state != State.Drive) return;
        if (PhotonNetwork.IsConnected && !photonView.IsMine) return;

        // AI操作中は入力不要
        if (driver != null) return;

        // --- New Input System ---
        float throttle = throttleAction.ReadValue<float>();
        float brake = brakeAction.ReadValue<float>();
        inputMotor = throttle - brake;

        inputSteer = steerAction.ReadValue<float>();

        // ジョイスティック（スマホ）
        if (variableJoystick != null && variableJoystick.Direction != Vector2.zero)
        {
            inputSteer = Mathf.Clamp(variableJoystick.Direction.x / 0.9f, -1f, 1f);
        }

        // アイテム使用
        if (useItemAction.WasPressedThisFrame())
        {
            inputUseItem = true;
            Debug.Log("[INPUT] Use Item");
        }

        Debug.Log("パーツ数:" + partsNum);
    }


    private void FixedUpdate()
    {
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

        //周回判定
        if (isLapClear)
        {
            lapCount++;
            nowAngle = 0f;

            //タイマーを点滅　スタート直後を除いて実行
            if(lapCount > 0) blinkTimer = lapBlinkTime;
        }

        //ゴール判定
        if(lapCount == maxLaps)
        {
            //リザルトUIを有効化
            if(resultUI.activeSelf == false) resultUI.SetActive(true);

            //ランキングUIを更新
            var result = resultUI.GetComponent<resultUI>();
            if (result != null)
            {
                result.UpdateRankUI(GetName() , timer);
            }

            if (driver == null)
            {
                //AIに切り替え
                var wpContainer = FindObjectOfType<WaypointContainer>();
                SetAI<AIDriver>(wpContainer);

                //リザルトUIを表示開始
                result.StartCoroutines();
            }

            //ゴール後に表示されるように
            if(isMine) UpdateRank();

            //タイマー黄色に変更
            timerText.color = Color.yellow;

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
        }
        else
        {
            //　プレイヤー入力
            motorInput = throttleAction.ReadValue<float>() - brakeAction.ReadValue<float>();
            steerInput = steerAction.ReadValue<float>();
            //if (Input.GetMouseButton(0)) motorInput = 1;
            if (variableJoystick != null && variableJoystick.Direction != Vector2.zero)
                steerInput = Mathf.Clamp(variableJoystick.Direction.x / 0.9f, -1, 1);

            //周回数をUIに反映
            lapText.text = $"Angle : {nowAngle} , Lap : {Mathf.Max(0,lapCount)}";

            //　プレイヤー入力:Update()で取得した入力を使用
            motorInput = inputMotor;
            steerInput = inputSteer;

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
        if (currentGroundTag == "Dirt")
        {
            accelMultiplier = dirtAccelMultiplier;
            speedMultiplier = dirtSpeedMultiplier;
        }

        if (boostTimer > 0f)
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
        if (speedText != null && driver == null) speedText.text = $"{speed:F1} km/h";   

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
        rb.AddForce(Vector3.down * extraGravity, ForceMode.Acceleration);
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

        //ラップ判定 100度ごとにチェックポイントを通過したか
        int sector = Mathf.FloorToInt(nowAngle / 100f);
        if(sector > 0)
        {
            for(int i = 0;i < sector;i++)
            {
                if (flags[i] == false)
                {
                    if (i == sector - 1) flags[i] = true;
                    else break;
                }
            }
        }

        int throughFlags = 0;
        foreach(var f in flags)
        {
            if (f) throughFlags++;
        }

        if(throughFlags == flags.Length && cur == 0)
        {
            isLapClear = true;
            //フラグリセット
            for (int i = 0; i < flags.Length; i++)
            {
                flags[i] = false;
            }
        }
        else
        {
            isLapClear = false;
        }
    }

    [PunRPC]
    public void RPC_StateToDrive()
    {
        StateToDrive();
    }

    //状態を運転に
    public void StateToDrive()
    {
        state = State.Drive;
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
    }

    [PunRPC]
    public void RPC_UpdateRank()
    {
        UpdateRank();
    }

    //順位更新
    public void UpdateRank()
    {
        //観戦者の時に作動しないように RPCがバッファされてるのでここで処理止める
        if (PlayerPrefs.GetInt("isMonitor") == 1) return;

        //全カートの角度とラップ数を取得　比較して順位を決定
        CarController[] cars = FindObjectsOfType<CarController>();
        currentRank = 1;
        Debug.Log($" === {cars.Length}台のカートで順位計算 === ");
        foreach (var car in cars)
        {
            if (car == this) continue;

            //ラップ数が多いほうが上位
            if (car.GetLapCount() > lapCount)
            {
                currentRank++;
            }
            //ラップ数が同じなら角度が大きいほうが上位
            else if (car.GetLapCount() == lapCount)
            {
                if (lapManager.NowAngle(car.transform.position) > lapManager.NowAngle(transform.position))
                {
                    currentRank++;
                }
            }
        }

        //UIに反映
        if(lapCount == maxLaps - 1 && lapManager.NowAngle(transform.position) >= 340f)
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


    //接触がコインならカウント
    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Coin"))
        {
            Coin coinScript = other.GetComponent<Coin>();
            if (coinScript.isCnt == false)
            {
                coinCnt++;
                coinScript.isCnt = true;

                //自分以外ならテキストの更新はしない
                if (PhotonNetwork.IsConnected && !photonView.IsMine || driver != null) return;
                coinText.text = $"{coinCnt:D4}";
            }
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

        return ret;
    }

    public void SendParts(PartsID id)
    {
        //ゴール後は処理なし
        if(resultUI.gameObject.activeSelf)
        {
            return;
        }

        //シングルプレイ時の操作
        if (!PhotonNetwork.IsConnected)
        {
            itemManager.SpawnItem(id);
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
        PartsID usedId = (PartsID)itemManager.Dequeue(true);

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
        else if(!PhotonNetwork.IsConnected && isMine)
        {
            RPC_RemoveItem(usedId);
        }
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

    
    public override void OnPlayerPropertiesUpdate(Player targetPlayer, ExitGames.Client.Photon.Hashtable changed)
    {
        Debug.Log("CALL BACK");
        TryPairPlayers();
    }

    // アイテム表示更新
    private void DisplayItem(int? id)
    {
        // アイテム表示更新
        switch (id)
        {
            case (int)PartsID.Energy:
                itemText.text = "Item : ENERGY";
                break;
            case (int)PartsID.Rocket:
                itemText.text = "Item : ROCKET";
                break;
            case null:
                itemText.text = "Item : NULL";
                break;
        }
    }

    // ----- 通信用関数 -----

    // アイテムをキューに追加
    [PunRPC]
    public void RPC_EnqueueItem(PartsID id)
    {
        Debug.Log("Enqueue Item Request");

        itemManager.Enqueue((int)id);

        // アイテム表示更新
        if (itemText.text != "Item : NULL") return;

        DisplayItem((int)id);
    }

    // アイテムをキューから削除
    [PunRPC]
    public void RPC_RemoveItem(PartsID id)
    {
        Debug.Log("Remove Item Request");

        itemManager.Remove((int)id);

        int? nextItem = itemManager.Dequeue(false);

        // アイテム表示更新
        DisplayItem(nextItem);
    }

    // アイテムを使用、キューから削除
    [PunRPC]
    public void RPC_UseItem(PartsID id)
    {
        Debug.Log("Remove Item Request");

        itemManager.Remove((int)id);

        int? nextItem = itemManager.Dequeue(false);

        // アイテム表示更新
        DisplayItem(nextItem);
    }

    // 未設置パーツ数を増やす
    [PunRPC]
    public void RPC_AddPartsNum()
    {
        Debug.Log("Add PartsNum Request");

        AddPartsNum();
    }

    // 未設置パーツ数を減らす
    [PunRPC]
    public void RPC_RemovePartsNum()
    {
        Debug.Log("Substract PartsNum Request");

        SubstractPartsNum();
    }

    // パッシブパーツの設置通知
    [PunRPC]
    public void RPC_SetPassiveState(PartsID id,bool isAdd)
    {
        Debug.Log("PassiveState Request");

        SetPassiveState(id,isAdd);
    }
}