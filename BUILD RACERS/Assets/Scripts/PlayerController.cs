using Photon.Pun;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerController : MonoBehaviourPunCallbacks
{
    private Rigidbody rb;

    [SerializeField]
    private float moveSpeed;

    [SerializeField]
    private float jumpPower;

    [SerializeField]
    private bool debugConsole;

    private Vector3 move;

    private Joystick joystick;

    void Start()
    {
        rb = GetComponent<Rigidbody>();

        //ジョイスティックの取得
        var canvas = GameObject.Find("CanvasForAndroid");
        joystick = canvas.transform.Find("Floating Joystick").GetComponent<Joystick>();
    }

    void Update()
    {
        //自分でなければ処理しない
        if (photonView.IsMine == false) return;

    }

    //フレームレートによる差が出ないように、物理演算はこちらで行う
    private void FixedUpdate()
    {
        //自分でなければ処理しない
        if (photonView.IsMine == false) return;

        move = Vector3.zero;

        //キーボード処理
        if (Keyboard.current != null)
        {
            if (Keyboard.current.wKey.isPressed) { move.z += moveSpeed; if(debugConsole) Debug.Log("input W Key"); }
            if (Keyboard.current.sKey.isPressed) { move.z -= moveSpeed; if(debugConsole) Debug.Log("input S Key"); }
            if (Keyboard.current.aKey.isPressed) { move.x -= moveSpeed; if(debugConsole) Debug.Log("input A Key"); }
            if (Keyboard.current.dKey.isPressed) { move.x += moveSpeed; if(debugConsole) Debug.Log("input D Key"); }
        }

        //コントローラー処理
        if (Gamepad.current != null)
        {
            if (Gamepad.current.buttonNorth.isPressed) { move.z += moveSpeed; if(debugConsole) Debug.Log("input Y Button"); }
            if (Gamepad.current.buttonSouth.isPressed) { move.z -= moveSpeed; if(debugConsole) Debug.Log("input A Button"); }
            if (Gamepad.current.buttonWest.isPressed)  { move.x -= moveSpeed; if(debugConsole) Debug.Log("input X Button"); }
            if (Gamepad.current.buttonEast.isPressed)  { move.x += moveSpeed; if(debugConsole) Debug.Log("input B Button"); }
        }

        //仮想ジョイスティック処理
        if(joystick.Direction != Vector2.zero)
        {
            var dir2 = joystick.Direction;
            float joyX, joyY, joyZ;

            //8割入力ほどで最大値となるように
            float maxRate = 0.8f;

            joyX = dir2.x / maxRate;
            joyY = 0;
            joyZ = dir2.y / maxRate;

            Vector3 dir3 = new Vector3(joyX, joyY, joyZ);
            move += dir3 * moveSpeed;
        }

        if (move != Vector3.zero) rb.AddForce(move);

        // ジャンプ
        if (Keyboard.current.spaceKey.isPressed && this.transform.position.y < 1)
        {
            rb.AddForce(new Vector3(0, jumpPower, 0));
        }
    }
}
