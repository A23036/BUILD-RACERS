using Photon.Pun;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerController : MonoBehaviourPunCallbacks
{
    private Rigidbody rb;

    public float moveSpeed = 1;

    public float jumpPower = 5;

    void Start()
    {
        rb = GetComponent<Rigidbody>();
    }

    void Update()
    {
        //é©ï™Ç≈Ç»ÇØÇÍÇŒèàóùÇµÇ»Ç¢
        if (photonView.IsMine == false) return;

        Vector3 move = Vector3.zero;

        if (Keyboard.current.wKey.isPressed) move.z += moveSpeed;
        if (Keyboard.current.sKey.isPressed) move.z -= moveSpeed;
        if (Keyboard.current.aKey.isPressed) move.x -= moveSpeed;
        if (Keyboard.current.dKey.isPressed) move.x += moveSpeed;

        if(move != Vector3.zero) rb.AddForce(move);

        // ÉWÉÉÉìÉv
        if (Keyboard.current.spaceKey.isPressed && this.transform.position.y < 1)
        {
            rb.AddForce(new Vector3(0, jumpPower, 0));
        }
    }
}
