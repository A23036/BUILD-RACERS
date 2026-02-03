using TMPro;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections;

#if UNITY_EDITOR
using UnityEditor.Presets;
#endif

public enum eTutorialState
{
    WelcomeText,
    WaitForParts,
    PassivePartsWait,
    PassivePartsInfo,
    ItemPartsWait,
    ItemPartsInfo,
    GimmickPartsWait,
    GimmickPartsInfo,
    GimmickPartsInfo2,
    FinishWait,
    FinishInfo,
    Quit,
}

public class engineerTutorial : baseScene
{
    [SerializeField] private TextMeshProUGUI infoText;
    [SerializeField] private float waitBlinkInterval = 0.5f;

    [Header("Fade")]
    [SerializeField] private Fade fade;
    [SerializeField] private float fadeInDuration = 0.8f;
    [SerializeField] private float fadeOutDuration = 0.8f;

    [Header("Sound")]
    [SerializeField] private AudioClip tutorialBGM; // チュートリアルBGM
    [SerializeField] private AudioClip textSe;      // テキスト送り音
    [SerializeField] private AudioClip textLoopSe;  // テキスト表示音
    private bool wasTyping = false;

    private Coroutine waitBlinkCo;
    private string waitBlinkBaseText;

    private TextTyper typer;

    private CarController carController;
    private Engineer engineer;
    private eTutorialState state;

    // 入力待ちフラグ
    private bool isWaitingInput = false;
    private eTutorialState pendingNextState;

    // 初回パーツ入手フラグ監視用
    private ItemManager cpuItemManager;

    private Coroutine afterUseCo;
    private bool isTransitionScheduled = false;

    private void Awake()
    {
        // 状態の初期化
        PlayerPrefs.SetInt("driverNum", -1);
        PlayerPrefs.SetInt("engineerNum", 1);
        PlayerPrefs.SetInt("isMonitor", 0);

        //相方ドライバーの生成（CPU）
        var cpu = Instantiate(Resources.Load("tutorial player"), new Vector3(0, 0, -5), Quaternion.identity);
        carController = cpu.GetComponent<CarController>();
        carController.SetName(PlayerPrefs.GetString("PlayerName"));
        var cpuCc = cpu.GetComponent<CarController>();
        var wpContainer = FindObjectOfType<WaypointContainer>();
        cpuCc.SetAI<AIDriver>(wpContainer);
        carController.isMine = true;
        carController.SetIsTutorial();

        cpuItemManager = cpuCc.GetComponent<ItemManager>();
        if (cpuItemManager != null)
        {
            cpuItemManager.OnFirstPartsTypeAcquired += OnFirstPartsTypeAcquired;
        }


        //エンジニアの生成
        var player = Instantiate(Resources.Load("Engineer"));
        engineer = player.GetComponent<Engineer>();
        engineer.SetPairDriver(cpuCc);
        engineer.SetCamera();

        // 初期状態では停止
        carController.SetState(State.Stop);

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
                EnterState(eTutorialState.WelcomeText);
            });
        }
    }

    void Update()
    {
        // 入力処理（文字送り中はスキップ、完了後は次へ）
        HandleAdvanceInput();

        // 文字送りSE制御
        if (typer != null)
        {
            if (typer.IsTyping && !wasTyping)
            {
                SoundManager.Instance.PlayLoopSE(textLoopSe);
            }
            else if (!typer.IsTyping && wasTyping)
            {
                SoundManager.Instance.StopLoopSE();
            }

            wasTyping = typer.IsTyping;
        }

        base.Update();
    }

    private void HandleAdvanceInput()
    {
        bool pressed = false;

        // マウスクリック
        if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
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

    private void WaitForInputThenNext(eTutorialState next)
    {
        pendingNextState = next;
        isWaitingInput = true;
    }

    private void EnterState(eTutorialState next)
    {
        state = next;

        // 状態に入ったら入力待ちはいったん解除
        isWaitingInput = false;

        switch (state)
        {
            case eTutorialState.WelcomeText:
                typer.Play("BUILD RACERSへようこそ！\nここでは「エンジニア」のおためしができます|\n", () =>
                {
                    // 文字送り完了後、入力待ち→次へ
                    WaitForInputThenNext(eTutorialState.WaitForParts);
                    BeginWaitForAdvance();
                });
                break;

            case eTutorialState.WaitForParts:
                typer.Play("ペアのドライバーが「パーツ」を取得します\nそれまで待ってみましょう|", () =>
                {
                    WaitForInputThenNext(eTutorialState.PassivePartsWait);
                    BeginWaitForAdvance();
                });
                break;

            case eTutorialState.PassivePartsWait:
                // ここでは「CPUがパッシブを初回取得するのを待つ」
                // 取得通知は OnFirstPartsTypeAcquired で受け取って PassivePartsInfo に遷移する
                break;

            case eTutorialState.PassivePartsInfo:
                typer.Play("今入手したのは「パッシブ」です！\nパネルに配置するとドライバーを一定時間強化できます|", () =>
                {
                    WaitForInputThenNext(eTutorialState.ItemPartsWait);
                    BeginWaitForAdvance();
                });
                break;

            case eTutorialState.ItemPartsWait:
                // ここでは「CPUがアイテムを初回取得するのを待つ」
                break;

            case eTutorialState.ItemPartsInfo:
                typer.Play("今入手したのは「アイテム」です！\nパネルに配置するとドライバーにアイテムを送れます|", () =>
                {
                    WaitForInputThenNext(eTutorialState.GimmickPartsWait);
                    BeginWaitForAdvance();
                });
                break;

            case eTutorialState.GimmickPartsWait:
                // ここでは「CPUがギミックを初回取得するのを待つ」
                break;

            case eTutorialState.GimmickPartsInfo:
                typer.Play("今入手したのは「ギミック」です！\nミニマップに配置してライバルをジャマできます|", () =>
                {
                    WaitForInputThenNext(eTutorialState.GimmickPartsInfo2);
                    BeginWaitForAdvance();
                });
                break;

            case eTutorialState.GimmickPartsInfo2:
                typer.Play("赤い方向矢印が付いているものはクリックで回転させることもできますよ|", () =>
                {
                    WaitForInputThenNext(eTutorialState.FinishWait);
                    BeginWaitForAdvance();
                });
                break;
            
            case eTutorialState.FinishWait:
                break;

            case eTutorialState.FinishInfo:
                typer.Play("チュートリアルはESCで終わることができます\nパーツ配置を試してみましょう|", () =>
                {
                    WaitForInputThenNext(eTutorialState.Quit);
                    BeginWaitForAdvance();
                });
                break;

            case eTutorialState.Quit:
                break;
        }
    }

    // 入力が進んだ次にしたい処理をここに
    private void OnAdvanceFromState(eTutorialState current)
    {
        switch (current)
        {
            case eTutorialState.WaitForParts:
                // テキスト消す
                typer.ResetTyper(true);
                // 走行開始
                carController.SetState(State.Drive);
                break;

            case eTutorialState.PassivePartsInfo:
                typer.ResetTyper(true);
                carController.SetState(State.Drive);
                // 次の待機へ（ItemPartsWait）に入るので、走行は継続でOK
                break;

            case eTutorialState.ItemPartsInfo:
                typer.ResetTyper(true);
                carController.SetState(State.Drive);
                // 次の待機へ（GimmickPartsWait）
                break;

            case eTutorialState.GimmickPartsInfo:
                typer.ResetTyper(true);
                break;

            case eTutorialState.GimmickPartsInfo2:
                typer.ResetTyper(true);
                carController.SetState(State.Drive);
                StartCoroutine(Co_Wait());
                break;

            case eTutorialState.FinishInfo:
                typer.ResetTyper(true);
                carController.SetState(State.Drive);
                break;
        }
    }

    private void OnFirstPartsTypeAcquired(PartsType type)
    {
        // 「待機中のときだけ」受け付ける（順番固定の前提でOK）
        // 文字送り/点滅/入力待ちが残っている可能性があるので、状態遷移前に止める
        switch (type)
        {
            case PartsType.Passive:
                if (state != eTutorialState.PassivePartsWait) return;
                carController.SetState(State.Stop);
                EndWaitForAdvance();
                typer.ResetTyper(true);
                EnterState(eTutorialState.PassivePartsInfo);
                break;

            case PartsType.Item:
                if (state != eTutorialState.ItemPartsWait) return;
                carController.SetState(State.Stop);
                EndWaitForAdvance();
                typer.ResetTyper(true);
                EnterState(eTutorialState.ItemPartsInfo);
                break;

            case PartsType.Gimmick:
                if (state != eTutorialState.GimmickPartsWait) return;
                carController.SetState(State.Stop);
                EndWaitForAdvance();
                typer.ResetTyper(true);
                EnterState(eTutorialState.GimmickPartsInfo);
                break;
            default:
                break;

        }
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

    private IEnumerator Co_Wait()
    {
        yield return new WaitForSeconds(3f);

        // 念のため入力待ち/点滅が走ってたら止める
        EndWaitForAdvance();

        typer.ResetTyper(true);
        carController.SetState(State.Stop);
        EnterState(eTutorialState.FinishInfo);
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
