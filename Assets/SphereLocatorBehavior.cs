using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SphereLocatorBehavior : MonoBehaviour
{
    public Transform point;
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public Vector3 GetRotation( Vector2 rotation)
    {

        transform.localEulerAngles = new Vector3(0f, -rotation.x, rotation.y);

        return point.position;
    }

    public List<Vector3> GetPositionList(List<Vector3> list)
    {
        List<Vector3> pointList = new List<Vector3>();

        foreach (var gps in list)
        {
            pointList.Add(GetRotation(gps));
        }

        return pointList;
    }
}
