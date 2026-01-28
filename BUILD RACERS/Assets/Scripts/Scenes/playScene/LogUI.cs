using Photon.Pun;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using static TMPro.SpriteAssetUtilities.TexturePacker_JsonArray;

public class LogUI : MonoBehaviour
{
    [Header("Settings")]
    [SerializeField] private GameObject logPrefab; // LogUIの下に生成するプレハブ
    [SerializeField] private Transform logParent; // LogUI Transform
    [SerializeField] private float displayDuration = 5f; // 表示時間（秒）
    [SerializeField] private float spacing = 10f; // ログ間のスペース
    [SerializeField] private float fadeOutDuration = 0.5f; // フェードアウト時間

    private List<GameObject> activeHitLogs = new List<GameObject>();

    void Update()
    {
    }

    public void SpawnHitLog()
    {
        if (logPrefab == null || logParent == null)
        {
            Debug.LogError("LogPrefab または LogParent が設定されていません！");
            return;
        }

        // プレハブを生成
        GameObject newLog = Instantiate(logPrefab, logParent);
        RectTransform rectTransform = newLog.GetComponent<RectTransform>();

        if (rectTransform != null)
        {
            // 位置を計算（既存のログの下に配置）
            float yOffset = 0;
            foreach (GameObject log in activeHitLogs)
            {
                if (log != null)
                {
                    RectTransform logRect = log.GetComponent<RectTransform>();
                    yOffset -= logRect.rect.height + spacing;
                }
            }

            rectTransform.anchoredPosition = new Vector2(rectTransform.anchoredPosition.x, yOffset);
        }

        // リストに追加
        activeHitLogs.Add(newLog);

        // 一定時間後に削除
        StartCoroutine(RemoveLogAfterDelay(newLog, displayDuration));
    }

    private IEnumerator RemoveLogAfterDelay(GameObject log, float delay)
    {
        // 表示時間待機
        yield return new WaitForSeconds(delay);

        // フェードアウト
        CanvasGroup canvasGroup = log.GetComponent<CanvasGroup>();
        if (canvasGroup == null)
        {
            canvasGroup = log.AddComponent<CanvasGroup>();
        }

        float elapsed = 0f;
        while (elapsed < fadeOutDuration)
        {
            elapsed += Time.deltaTime;
            canvasGroup.alpha = Mathf.Lerp(1f, 0f, elapsed / fadeOutDuration);
            yield return null;
        }

        // リストから削除
        activeHitLogs.Remove(log);

        // オブジェクトを破棄
        Destroy(log);

        // 残りのログの位置を再計算
        RepositionLogs();
    }

    private void RepositionLogs()
    {
        float yOffset = 0;
        foreach (GameObject log in activeHitLogs)
        {
            if (log != null)
            {
                RectTransform rectTransform = log.GetComponent<RectTransform>();
                if (rectTransform != null)
                {
                    // 滑らかに移動
                    StartCoroutine(SmoothMove(rectTransform, new Vector2(rectTransform.anchoredPosition.x, yOffset)));
                    yOffset -= rectTransform.rect.height + spacing;
                }
            }
        }
    }

    private IEnumerator SmoothMove(RectTransform rectTransform, Vector2 targetPosition)
    {
        Vector2 startPosition = rectTransform.anchoredPosition;
        float elapsed = 0f;
        float duration = 0.3f;

        while (elapsed < duration)
        {
            // オブジェクトが破棄されていたら終了
            if (rectTransform == null)
            {
                yield break;
            }

            elapsed += Time.deltaTime;
            rectTransform.anchoredPosition = Vector2.Lerp(startPosition, targetPosition, elapsed / duration);
            yield return null;
        }

        rectTransform.anchoredPosition = targetPosition;
    }

    // 外部から呼び出せるメソッド（キル情報付き）
    public void AddHitLog(string AttackerName, string victimName, string weaponIcon = "")
    {
        SpawnHitLog();

        // プレハブ内のテキストを更新する場合はここで処理
        if (activeHitLogs.Count > 0)
        {
            GameObject latestLog = activeHitLogs[activeHitLogs.Count - 1];

            // 自身または子オブジェクトからTextMeshProUGUIを探す
            TextMeshProUGUI textComponent = latestLog.GetComponentInChildren<TextMeshProUGUI>();

            if (textComponent != null)
            {
                textComponent.text = $"{AttackerName} → {victimName} : {weaponIcon}";
            }
            else
            {
                Debug.LogWarning("TextMeshProUGUIコンポーネントが見つかりません！");
            }
        }

        //色の変更
        Image img = logPrefab.GetComponent<Image>();
        if (AttackerName == PlayerPrefs.GetString("PlayerName")) img.color = new Color(img.color.r , 150f / 255f , img.color.b);
        else if(victimName == PlayerPrefs.GetString("PlayerName")) img.color = new Color(150f / 255f , img.color.g , img.color.b);
    }
}
