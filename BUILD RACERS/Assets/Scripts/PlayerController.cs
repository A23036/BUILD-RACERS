using Fusion;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerController : NetworkBehaviour
{
    private NetworkCharacterController characterController;

    public override void Spawned()
    {
        characterController = GetComponent<NetworkCharacterController>();
    }

    public override void FixedUpdateNetwork()
    {
        // ローカル入力を直接読む（新InputSystem）
        Vector2 move = Vector2.zero;

        if (Keyboard.current.wKey.isPressed) move.y += 1;
        if (Keyboard.current.sKey.isPressed) move.y -= 1;
        if (Keyboard.current.aKey.isPressed) move.x -= 1;
        if (Keyboard.current.dKey.isPressed) move.x += 1;

        var inputDirection = new Vector3(move.x, 0f, move.y);

        // 移動
        characterController.Move(inputDirection);

        // ジャンプ
        if (Keyboard.current.spaceKey.isPressed)
        {
            characterController.Jump();
        }
    }
}
