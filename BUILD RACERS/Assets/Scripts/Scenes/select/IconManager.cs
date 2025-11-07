using UnityEngine;
using System.Collections.Generic;

public class IconManager : MonoBehaviour
{
    [SerializeField] private List<Transform> driverIcons;
    [SerializeField] private List<Transform> builderIcons;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }
    public List<Transform> GetDriverIconsList()
    {
        return driverIcons;
    }

    public List<Transform> GetBuilderIconsList()
    {
        return builderIcons;
    }
}
