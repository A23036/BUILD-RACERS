using UnityEngine;

public class LapManager : MonoBehaviour
{
    private Vector3 CenterPos;
    private Vector3 GoalPos;
    private float offset;

    private bool[] flags;

    //時計回りか
    [SerializeField] private bool isClockwise;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        //中心位置を取得
        CenterPos = transform.position;
        //ゴール位置を取得
        GoalPos = transform.Find("GoalPoint").gameObject.transform.position;
        
        flags = new bool[3];

        //0度の基準をゴール位置に合わせる
        offset = NowAngle(GoalPos , out bool isLapClear);
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    // 現在のオブジェクト位置から角度を取得
    public float NowAngle(Vector3 objPos , out bool isLapClear)
    {
        isLapClear = false;

        Vector3 dir = objPos - CenterPos;
        float angle = Mathf.Atan2(dir.z, dir.x) * Mathf.Rad2Deg;
        if(angle < 0)
        {
            angle += 360;
        }

        //90度ごとにフラグを立てる
        int sector = Mathf.FloorToInt(angle / 100f);
        if(sector > 0)
        {
            //逆走防止
            for (int i = 0;i < sector;i++)
            {
                if (flags[i] == false && i != sector-1)
                {
                    break;
                }
                else if(flags[i] == false && i == sector - 1)
                {
                    flags[i] = true;
                }
            }
        }

        int throughFlagCnt = 0;
        //一周したか
        for (int i = 0;i < flags.Length;i++)
        {
            if(flags[i] == false)
            {
                break;
            }
            throughFlagCnt++;
        }

        if(throughFlagCnt == flags.Length && (int)angle == 0)
        {
            isLapClear = true;
            //フラグリセット
            for (int i = 0;i < flags.Length;i++)
            {
                flags[i] = false;
            }
        }

        return angle;
    }
}
