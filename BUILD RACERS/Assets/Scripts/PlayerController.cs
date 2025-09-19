using Photon.Pun;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerController : MonoBehaviourPunCallbacks
{
    private Rigidbody rb;

    public float moveSpeed;

    public float jumpPower;

    void Start()
    {
        rb = GetComponent<Rigidbody>();
    }

    void Update()
    {
        //自分でなければ処理しない
        if (photonView.IsMine == false) return;

        Vector3 move = Vector3.zero;

        //キーボード処理
        if(Keyboard.current != null)
        {
            if (Keyboard.current.wKey.isPressed) {move.z += moveSpeed;Debug.Log("input W Key");}
            if (Keyboard.current.sKey.isPressed) {move.z -= moveSpeed;Debug.Log("input S Key");}
            if (Keyboard.current.aKey.isPressed) {move.x -= moveSpeed;Debug.Log("input A Key");}
            if (Keyboard.current.dKey.isPressed) {move.x += moveSpeed;Debug.Log("input D Key");}
        }

        //コントローラー処理
        if (Gamepad.current != null)
        {
            if (Gamepad.current.buttonNorth.isPressed) {move.z += moveSpeed;Debug.Log("input Y Button");}
            if (Gamepad.current.buttonSouth.isPressed) {move.z -= moveSpeed;Debug.Log("input A Button");}
            if (Gamepad.current.buttonWest.isPressed)  {move.x -= moveSpeed;Debug.Log("input X Button");}
            if (Gamepad.current.buttonEast.isPressed)  {move.x += moveSpeed;Debug.Log("input B Button");}
        }

        if (move != Vector3.zero) rb.AddForce(move);

        // ジャンプ
        if (Keyboard.current.spaceKey.isPressed && this.transform.position.y < 1)
        {
            rb.AddForce(new Vector3(0, jumpPower, 0));
        }
    }
}
