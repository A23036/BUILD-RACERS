using TMPro;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections;

#if UNITY_EDITOR
using UnityEditor.Presets;
#endif

public enum dTutorialState
{
    WelcomeText,
    EnjoyDrive,
    Driving,
    ItemInfo,
    ItemUsing,
    ItemInfo2,
    FinishInfo,
    Quit,
}

public class driverTutorial : baseScene
{
    [SerializeField] private TextMeshProUGUI infoText;
    [SerializeField] private float waitBlinkInterval = 0.5f;

    [Header("Sound")]
    [SerializeField] private AudioClip tutorialBGM; // チュートリアルBGM
    [SerializeField] private AudioClip textSe;      // テキスト音

    [Header("Fade")]
    [SerializeField] private Fade fade;
    [SerializeField] private float fadeInDuration = 0.8f;
    [SerializeField] private float fadeOutDuration = 0.8f;

    private Coroutine waitBlinkCo;
    private string waitBlinkBaseText;

    private TextTyper typer;

    private CarController carController;
    private CarController botPlayer;
    private dTutorialState state;

    // 入力待ちフラグ
    private bool isWaitingInput = false;
    private dTutorialState pendingNextState;

    // 初回アイテム入手フラグ監視用
    private ItemManager playerItemManager;
    private bool gotFirstItem = false;
    private bool useFirstItem = false;

    private Coroutine afterUseCo;
    private bool isTransitionScheduled = false;

    private void Awake()
    {
        //ドライバーの生成
        var player = Instantiate(Resources.Load("tutorial player"), new Vector3(0, 0, -5), Quaternion.identity);
        player.GetComponent<CarController>().SetCamera();
        carController = player.GetComponent<CarController>();
        carController.isMine = true;
        carController.SetIsTutorial();
        playerItemManager = carController.GetComponent<ItemManager>();
        playerItemManager.OnFirstItemAcquired += FirstItemGet;
        playerItemManager.OnFirstItemUsed += FirstItemUse;

        // 初期状態では停止
        carController.SetState(State.Stop);

        // 状態の初期化
        PlayerPrefs.SetInt("driverNum", 1);
        PlayerPrefs.SetInt("engineerNum", -1);
        PlayerPrefs.SetInt("isMonitor", 0);

        // bot生成
        GenerateBotDrivers();

        SoundManager.Instance.PlayBGM(tutorialBGM, 0f);
    }

    void Start()
    {
        preSceneName = "tutorial";

        typer = infoText.GetComponent<TextTyper>();

        if (fade != null)
        {
            fade.SetStartRange();
            fade.FadeOut(fadeOutDuration, () =>
            {
                EnterState(dTutorialState.WelcomeText);
            });
        }
    }

    private void OnDisable()
    {
        if (playerItemManager != null)
        {
            playerItemManager.OnFirstItemAcquired -= FirstItemGet;
        }
    }

    void Update()
    {
        // 入力処理（文字送り中はスキップ、完了後は次へ）
        HandleAdvanceInput();

        base.Update();
    }

    private void HandleAdvanceInput()
    {
        bool pressed = false;

        // Enter
        if (Keyboard.current != null && Keyboard.current.enterKey.wasPressedThisFrame)
            pressed = true;

        // マウスクリック
        if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
            pressed = true;

        // Gamepad A or B
        if (Gamepad.current != null &&
            (Gamepad.current.buttonSouth.wasPressedThisFrame || Gamepad.current.buttonEast.wasPressedThisFrame))
            pressed = true;

        // スマホタップ
        if (Touchscreen.current != null &&
            Touchscreen.current.primaryTouch.press.wasPressedThisFrame)
            pressed = true;

        if (!pressed) return;

        // 文字送り中ならスキップ（全文表示）して終わらせる
        if (typer != null && typer.IsTyping)
        {
            typer.SkipToEnd();
            return;
        }

        // 文字送り完了後で、入力待ち中なら次へ
        if (isWaitingInput)
        {
            SoundManager.Instance.PlaySE(textSe);

            EndWaitForAdvance();
            isWaitingInput = false;

            // 「次の状態に行く前の処理」を行う
            OnAdvanceFromState(state);

            EnterState(pendingNextState);
        }
    }

    private void WaitForInputThenNext(dTutorialState next)
    {
        pendingNextState = next;
        isWaitingInput = true;
    }

    public void GenerateBotDrivers()
    {
        var wpContainer = FindObjectOfType<WaypointContainer>();
        
        var bot = Instantiate(Resources.Load("Player"), new Vector3(-80f, 0, -5f), Quaternion.identity);
        botPlayer = bot.GetComponent<CarController>();
        botPlayer.SetAI<AIDriver>(wpContainer);
        botPlayer.SetName("CPU");
        botPlayer.SetIsTutorial();
    }

    private void EnterState(dTutorialState next)
    {
        state = next;

        // 状態に入ったら入力待ちはいったん解除
        isWaitingInput = false;

        switch (state)
        {
            case dTutorialState.WelcomeText:
                typer.Play("BUILD RACERSへようこそ！\nここでは「ドライバー」のおためしができます|\n", () =>
                {
                    // 文字送り完了後、入力待ち→次へ
                    WaitForInputThenNext(dTutorialState.EnjoyDrive);
                    BeginWaitForAdvance();
                });
                break;

            case dTutorialState.EnjoyDrive:
                typer.Play("まずはカートを走らせてみましょう|", () =>
                {
                    WaitForInputThenNext(dTutorialState.Driving);
                    BeginWaitForAdvance();
                });
                break;

            case dTutorialState.Driving:
                break;
            case dTutorialState.ItemInfo:
                // アイテム説明テキスト
                typer.Play("アイテムを入手しましたね！\n早速使ってみましょう|", () =>
                {
                    WaitForInputThenNext(dTutorialState.ItemUsing);
                    BeginWaitForAdvance();
                });
                break;
            case dTutorialState.ItemUsing:
                break;
            case dTutorialState.ItemInfo2:
                // アイテム説明テキスト
                typer.Play("アイテムには様々な種類があります\nチュートリアルで情報を確認できますよ|", () =>
                {
                    WaitForInputThenNext(dTutorialState.FinishInfo);
                    BeginWaitForAdvance();
                });
                break;
            case dTutorialState.FinishInfo:
                // アイテム説明テキスト
                typer.Play("チュートリアルはESCで終わることができます\n自由に走ってみましょう|", () =>
                {
                    WaitForInputThenNext(dTutorialState.Quit);
                    BeginWaitForAdvance();
                });
                break;
            case dTutorialState.Quit:
                // ここでメニュー戻る等
                // WaitForInputThenNext(...) でもOK
                break;
        }
    }

    // 入力が進んだ次にしたい処理をここに
    private void OnAdvanceFromState(dTutorialState current)
    {
        switch (current)
        {
            case dTutorialState.EnjoyDrive:
                // テキスト消す
                typer.ResetTyper(true);
                // 走行開始
                carController.SetState(State.Drive);
                break;

            case dTutorialState.ItemInfo:
                // テキスト消す
                typer.ResetTyper(true);
                // 走行開始
                carController.SetState(State.Drive);
                botPlayer.SetState(State.Drive);
                break;

            case dTutorialState.FinishInfo:
                // テキスト消す
                typer.ResetTyper(true);
                // 走行開始
                carController.SetState(State.Drive);
                botPlayer.SetState(State.Drive);
                break;
        }
    }

    private void FirstItemGet()
    {
        if (gotFirstItem) return; // 念のため
        gotFirstItem = true;

        // もう使わないので購読解除
        if (playerItemManager != null)
        {
            playerItemManager.OnFirstItemAcquired -= FirstItemGet;
        }

        // 今Driving中じゃなければ無視
        if (state != dTutorialState.Driving) return;

        // state更新 停止→説明
        carController.SetState(State.Stop);
        botPlayer.SetState(State.Stop);
        EnterState(dTutorialState.ItemInfo);
    }

    private void FirstItemUse()
    {
        if (useFirstItem) return;
        useFirstItem = true;

        if (playerItemManager != null)
        {
            playerItemManager.OnFirstItemUsed -= FirstItemUse;
        }

        // 「アイテム使用を促している状態の時だけ」反応させる
        if (state != dTutorialState.ItemUsing) return;

        if (isTransitionScheduled) return;
        isTransitionScheduled = true;

        afterUseCo = StartCoroutine(Co_AfterFirstUseAdvance());
    }

    private IEnumerator Co_AfterFirstUseAdvance()
    {
        yield return new WaitForSeconds(2.0f);

        // もしこの間に別の状態へ行ってたら中止（保険）
        if (state != dTutorialState.ItemUsing) yield break;

        // 画面上の入力待ち点滅などがあれば止める
        EndWaitForAdvance();

        // 次の説明へ
        carController.SetState(State.Stop);
        botPlayer.SetState(State.Stop);
        EnterState(dTutorialState.ItemInfo2);

        afterUseCo = null;
    }


    private void BeginWaitForAdvance()
    {
        // 文字送りが終わってから入力待ちに入る想定
        isWaitingInput = true;

        // すでに点滅してたら止めてやり直し
        if (waitBlinkCo != null)
        {
            StopCoroutine(waitBlinkCo);
            waitBlinkCo = null;
        }

        waitBlinkBaseText = infoText.text; // 今表示している本文を保存
        waitBlinkCo = StartCoroutine(Co_BlinkLastVisibleChar());
    }

    private void EndWaitForAdvance()
    {
        isWaitingInput = false;

        if (waitBlinkCo != null)
        {
            StopCoroutine(waitBlinkCo);
            waitBlinkCo = null;
        }

        // 必ず元のテキストに戻す
        if (!string.IsNullOrEmpty(waitBlinkBaseText))
        {
            infoText.text = waitBlinkBaseText;
        }
    }

    private IEnumerator Co_BlinkLastVisibleChar()
    {
        // 末尾の「|」を探す（無ければ最後の可視文字）
        int idx = waitBlinkBaseText.LastIndexOf('|');
        if (idx < 0)
        {
            idx = FindLastVisibleCharIndex(waitBlinkBaseText);
            if (idx < 0) yield break;
        }

        char c = waitBlinkBaseText[idx];

        // ここが重要：文字列を「増やさない」ように、同じ場所の1文字だけを置換する
        string onText = waitBlinkBaseText;
        string offText = waitBlinkBaseText.Remove(idx, 1).Insert(idx, "<alpha=#00>" + c + "</alpha>");

        bool visible = true;

        while (isWaitingInput)
        {
            infoText.text = visible ? onText : offText;
            visible = !visible;
            yield return new WaitForSeconds(waitBlinkInterval);
        }

        infoText.text = onText;
    }


    private int FindLastVisibleCharIndex(string s)
    {
        if (string.IsNullOrEmpty(s)) return -1;

        for (int i = s.Length - 1; i >= 0; i--)
        {
            char c = s[i];
            // 改行/復帰/タブ/スペースだけで終わる場合を避ける
            if (c != '\n' && c != '\r' && c != '\t' && c != ' ')
                return i;
        }
        return -1;
    }

}
