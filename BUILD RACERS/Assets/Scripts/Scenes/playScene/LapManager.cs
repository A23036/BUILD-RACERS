using UnityEngine;

public class LapManager : MonoBehaviour
{
    private Vector3 CenterPos;
    private Vector3 GoalPos;
    private float offset;

    //時計回りか
    [SerializeField] private bool isClockwise;

    //ゴールまでの周回数
    [SerializeField] private int maxLaps;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        //中心位置を取得
        CenterPos = transform.position;
        //ゴール位置を取得
        GoalPos = transform.Find("GoalPoint").gameObject.transform.position;

        //0度の基準をゴール位置に合わせる
        offset = NowAngle(GoalPos);
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    // 現在のオブジェクト位置から角度を取得
    public float NowAngle(Vector3 objPos)
    {
        Vector3 dir = objPos - CenterPos;
        float angle = Mathf.Atan2(dir.z, dir.x) * Mathf.Rad2Deg;
        if(angle < 0)
        {
            angle += 360;
        }

        return angle;
    }

    public int GetMaxLaps()
    {
        return maxLaps;
    }
}
