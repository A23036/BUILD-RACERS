// HumanDriver.cs
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// HumanDriver: IDriver 実装（入力を返す）
/// - InputAction とジョイスティックを使って入力を取得
/// - UpdateState は受け取るが現時点では no-op（将来拡張用）
/// </summary>
public class HumanDriver : IDriver
{
    private Joystick joystick;
    private InputAction throttleAction;
    private InputAction brakeAction;
    private InputAction steerAction;

    public HumanDriver()
    {
        joystick = GameObject.FindObjectOfType<Joystick>();
        SetupInputActions();
    }

    private void SetupInputActions()
    {
        throttleAction = new InputAction(type: InputActionType.Button);
        throttleAction.AddBinding("<Keyboard>/w");
        throttleAction.AddBinding("<Keyboard>/upArrow");
        throttleAction.AddBinding("<Gamepad>/buttonEast");
        throttleAction.Enable();

        brakeAction = new InputAction(type: InputActionType.Button);
        brakeAction.AddBinding("<Keyboard>/s");
        brakeAction.AddBinding("<Keyboard>/downArrow");
        brakeAction.AddBinding("<Gamepad>/buttonSouth");
        brakeAction.Enable();

        steerAction = new InputAction(type: InputActionType.Value);
        steerAction.AddCompositeBinding("1DAxis").With("Negative", "<Keyboard>/a").With("Positive", "<Keyboard>/d");
        steerAction.AddCompositeBinding("1DAxis").With("Negative", "<Keyboard>/leftArrow").With("Positive", "<Keyboard>/rightArrow");
        steerAction.AddBinding("<Gamepad>/leftStick/x");
        steerAction.Enable();
    }

    public void GetInputs(out float throttle, out float brake, out float steer)
    {
        float t = 0f, b = 0f, s = 0f;
        if (throttleAction != null) t = throttleAction.ReadValue<float>();
        if (brakeAction != null) b = brakeAction.ReadValue<float>();
        if (steerAction != null) s = steerAction.ReadValue<float>();

        // マウス長押しでアクセル（モバイル互換）
        if (UnityEngine.Input.GetMouseButton(0)) t = 1f;

        // ジョイスティック優先
        if (joystick != null && joystick.Direction != Vector2.zero)
        {
            s = Mathf.Clamp(joystick.Direction.x / 0.9f, -1f, 1f);
        }

        throttle = t;
        brake = b;
        steer = s;
    }

    public bool RequestBoost() => false;

    public void SetWaypointContainer(WaypointContainer container)
    {

    }
}
