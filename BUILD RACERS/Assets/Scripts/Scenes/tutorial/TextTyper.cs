using System;
using System.Collections;
using TMPro;
using UnityEngine;

[DisallowMultipleComponent]
public class TextTyper : MonoBehaviour
{
    [Header("Type Settings")]
    [SerializeField] private float charInterval = 0.03f; // 1文字間隔
    [SerializeField] private bool useUnscaledTime = false; // Time.timeScale=0でも進めたいならtrue
    [SerializeField] private bool allowSkip = true; // Skip()を有効にする
    [SerializeField] private float lineBreakInterval = 0.4f; // 改行時の待機時間
    [SerializeField] private GameObject backImage;

    private TextMeshProUGUI tmp;
    private Coroutine typingCo;

    private bool isTyping;
    private bool skipRequested;

    // 直近の完了通知（Playのたびに差し替え）
    private Action onComplete;

    public bool IsTyping => isTyping;

    private void Awake()
    {
        tmp = GetComponent<TextMeshProUGUI>();
        if (tmp == null)
        {
            Debug.LogError("[TextTyper] TextMeshProUGUI が見つかりません。", this);
        }
    }

    /// 文字送り開始。再生中なら上書きしてやり直す。
    public void Play(string message, Action onComplete = null, bool clearBefore = true)
    {
        if (tmp == null) return;

        // テキスト開始＝背景表示
        SetBackgroundVisible(true);

        this.onComplete = onComplete;

        if (typingCo != null)
        {
            StopCoroutine(typingCo);
            typingCo = null;
        }

        skipRequested = false;
        typingCo = StartCoroutine(TypeCoroutine(message, clearBefore));
    }

    /// 文字送りを止めて、全文表示にして完了通知を発火する
    public void SkipToEnd()
    {
        if (!allowSkip) return;
        if (!isTyping) return;
        skipRequested = true;
    }

    public void ResetTyper(bool clearText = true)
    {
        if (typingCo != null)
        {
            StopCoroutine(typingCo);
            typingCo = null;
        }

        isTyping = false;
        skipRequested = false;
        onComplete = null;

        if (clearText && tmp != null)
        {
            tmp.text = "";
        }

        // 停止＝背景OFF（Resetと同じ扱い）
        SetBackgroundVisible(false);
    }

    private IEnumerator TypeCoroutine(string message, bool clearBefore)
    {
        isTyping = true;

        if (clearBefore) tmp.text = "";

        for (int i = 0; i < message.Length; i++)
        {
            if (skipRequested)
            {
                tmp.text = message;
                break;
            }

            char c = message[i];
            tmp.text += c;

            // 改行なら専用の「間」を入れる
            if (c == '\n')
            {
                if (useUnscaledTime)
                    yield return new WaitForSecondsRealtime(lineBreakInterval);
                else
                    yield return new WaitForSeconds(lineBreakInterval);
            }
            else
            {
                if (useUnscaledTime)
                    yield return new WaitForSecondsRealtime(charInterval);
                else
                    yield return new WaitForSeconds(charInterval);
            }
        }

        isTyping = false;
        typingCo = null;

        // 完了通知
        var cb = onComplete;
        onComplete = null;
        cb?.Invoke();
    }

    private void SetBackgroundVisible(bool visible)
    {
        if (backImage != null)
        {
            if (backImage.activeSelf != visible)
                backImage.SetActive(visible);
            return;
        }
    }
}
