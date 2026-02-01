using Photon.Pun;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using static TMPro.SpriteAssetUtilities.TexturePacker_JsonArray;

public class LogUI : MonoBehaviourPunCallbacks
{
    [Header("Settings")]
    [SerializeField] private GameObject logPrefab; // LogUIの下に生成するプレハブ
    [SerializeField] private Transform logParent; // LogUI Transform
    [SerializeField] private float displayDuration = 5f; // 表示時間（秒）
    [SerializeField] private float spacing = 10f; // ログ間のスペース
    [SerializeField] private float fadeOutDuration = 0.5f; // フェードアウト時間

    [Header("Fonts")]
    [SerializeField] private TMP_SpriteAsset RocketGreenFont;
    [SerializeField] private TMP_SpriteAsset RocketRedFont;
    [SerializeField] private TMP_SpriteAsset BalloonFont;
    [SerializeField] private TMP_SpriteAsset BalloonTrapFont;

    private List<GameObject> activeHitLogs = new List<GameObject>();

    private string pairName;

    void Update()
    {
    }

    [PunRPC]
    public void RPC_SetPairName(string myname , string pName)
    {
        Debug.Log($"{myname} , {pName}");
        Debug.Log($"{PlayerPrefs.GetString("PlayerName")}");

        if (PlayerPrefs.GetString("PlayerName") != myname) return;
        pairName = pName;

        Debug.Log($"SET PAIR NAME LOG UI: {pairName}");
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
        if (rectTransform == null)
        {
            yield break;
        }

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

        if (rectTransform == null)
        {
            yield break;
        }

        rectTransform.anchoredPosition = targetPosition;
    }

    [PunRPC]
    public void RPC_AddHitLog(string AttackerName, string victimName, string weaponIcon = "")
    {
        AddHitLog(AttackerName, victimName, weaponIcon);
    }

    // 外部から呼び出せるメソッド
    public void AddHitLog(string AttackerName, string victimName, string weaponIcon = "")
    {
        SpawnHitLog();

        // プレハブ内のテキストを更新する場合はここで処理
        if (activeHitLogs.Count > 0)
        {
            GameObject latestLog = activeHitLogs[activeHitLogs.Count - 1];

            // 自身または子オブジェクトからTextMeshProUGUIを探す
            TextMeshProUGUI textComponent = latestLog.GetComponentInChildren<TextMeshProUGUI>();
            Image img = latestLog.GetComponentInChildren<Image>();

            if (textComponent != null)
            {
                //フォント画像の設定
                switch (weaponIcon)
                {
                    case "RocketGreen":
                        textComponent.spriteAsset = RocketGreenFont;
                        break;
                    case "RocketRed":
                        textComponent.spriteAsset = RocketRedFont;
                        break;
                    case "WaterBalloonExplosion":
                        textComponent.spriteAsset = BalloonFont;
                        break;
                    case "WaterBalloonTrap":
                        textComponent.spriteAsset = BalloonTrapFont;
                        break;
                    default:
                        Debug.LogError("無効なweaponIconが指定されました: " + weaponIcon);
                        break;
                }

                //画像位置と大きさの調整
                textComponent.text = $"{AttackerName.PadRight(8)} → {victimName.PadRight(8)} : <voffset=0.3fem><size=150%><sprite=0></size></voffset>";

                float brightness = 200f / 255f;

                Debug.Log($"{PlayerPrefs.GetString("PlayerName")} , {pairName}");

                //色の変更
                if(AttackerName == victimName || AttackerName == pairName || victimName == pairName)
                {
                    //自滅は黄色
                    img.color = new Color(150f / 255f, 150f / 255f, 0, brightness);
                }
                else if (AttackerName == PlayerPrefs.GetString("PlayerName") || AttackerName == pairName)
                {
                    //攻撃成功は緑色
                    img.color = new Color(0, 150f / 255f, 0 , brightness);
                }
                else if (victimName == PlayerPrefs.GetString("PlayerName") || victimName == pairName)
                {
                    //攻撃されたら赤色
                    img.color = new Color(150f / 255f , 0 , 0, brightness);
                }
                else
                {
                    img.color = new Color(0, 0, 0, brightness);
                }
            }
            else
            {
                Debug.LogWarning("TextMeshProUGUIコンポーネントが見つかりません！");
            }
        }
    }
}