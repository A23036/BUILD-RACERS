using UnityEngine;

[ExecuteAlways]
public class AspectKeeper : MonoBehaviour
{
    [SerializeField]
    private Camera targetCamera;   // 固定比率を適用するカメラ（例：Main Camera）

    [SerializeField]
    private Vector2 aspectVec = new Vector2(9, 16);  // 目的比率（例：9:16、16:9、4:3など）

    private void Update()
    {
        if (targetCamera == null)
        {
            Debug.LogWarning("AspectKeeper: targetCamera が未設定です。");
            return;
        }

        float screenAspect = Screen.width / (float)Screen.height;     // 実際の画面比率
        float targetAspect = aspectVec.x / aspectVec.y;               // 目的とする比率
        float magRate = targetAspect / screenAspect;

        Rect viewportRect = new Rect(0, 0, 1, 1);

        if (magRate < 1f)
        {
            // 横幅を狭める
            viewportRect.width = magRate;
            viewportRect.x = 0.5f - viewportRect.width * 0.5f;
        }
        else
        {
            // 高さを狭める
            viewportRect.height = 1f / magRate;
            viewportRect.y = 0.5f - viewportRect.height * 0.5f;
        }

        targetCamera.rect = viewportRect;
    }
}
