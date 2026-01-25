using UnityEngine;

public class FrameScaler : MonoBehaviour
{
    [SerializeField] private float speed = 1f;
    [SerializeField] private float targetMultiplier = 1.5f;

    private Vector3 initialScale;
    private float timer;

    void Awake()
    {
        initialScale = transform.localScale;
    }

    void Update()
    {
        timer += Time.deltaTime * speed;
        float t = Mathf.PingPong(timer, 1f);

        float multiplier = Mathf.Lerp(1f, targetMultiplier, t);

        transform.localScale = new Vector3(
            initialScale.x * multiplier,
            initialScale.y * multiplier,
            initialScale.z
        );
    }
}

