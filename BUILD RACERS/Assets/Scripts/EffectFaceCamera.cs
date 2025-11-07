using UnityEngine;

public class FaceCamera : MonoBehaviour
{
    void Update()
    {
        // ƒJƒƒ‰‚Ì•ûŒü‚Éí‚ÉŒü‚¯‚é
        transform.LookAt(Camera.main.transform);
    }
}
