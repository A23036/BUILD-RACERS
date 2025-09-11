using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerController : MonoBehaviour
{
    //プレイヤーの移動速度
    public float speed = 0.01f;
    public float jumpPower = 1;

    public Rigidbody rb;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        rb = GetComponent<Rigidbody>();

    }

    // Update is called once per frame
    void Update()
    {
        //キー状況を取得
        var current = Keyboard.current;
        
        //WASDで移動処理
        if(current.wKey.isPressed)
        {
            this.transform.Translate(0,0,speed);
        }
        if (current.sKey.isPressed)
        {
            this.transform.Translate(0,0,-speed);
        }
        if (current.aKey.isPressed)
        {
            this.transform.Translate(-speed, 0, 0);
        }
        if (current.dKey.isPressed)
        {
            this.transform.Translate(speed, 0, 0);
        }

        //スペースでジャンプ
        if(current.spaceKey.isPressed)
        {
            rb.AddForce(0, jumpPower, 0);
        }
    }
}
