using UnityEngine;
public interface IDriver
{
    // 0..1 のアクセル、0..1 のブレーキ、-1..1 の横方向ステア
    void GetInputs(out float throttle, out float brake, out float steer);

    //ウェイポイントの設定
    public void SetWaypointContainer(WaypointContainer container);
}
