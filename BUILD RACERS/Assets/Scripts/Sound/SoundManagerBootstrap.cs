using UnityEngine;

public static class SoundManagerBootstrap
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Boot()
    {
        // 既に存在するなら何もしない（シーンに置いててもOK）
        if (Object.FindAnyObjectByType<SoundManager>() != null) return;

        // Resources からPrefabをロードして生成
        var prefab = Resources.Load<GameObject>("SoundManager");
        if (prefab == null)
        {
            Debug.LogWarning("SoundManager prefab not found in Resources/SoundManager.prefab");
            return;
        }

        Object.Instantiate(prefab);
        // Prefab側のAwakeでDontDestroyOnLoadされる想定
    }
}
