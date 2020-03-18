using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GameManager : MonoBehaviour
{

    [SerializeField]
    private LineManager _lineManager;

    private TextureGenerator _dataContainer;
    // Start is called before the first frame update
    void Start()
    {
        _dataContainer = GetComponent<TextureGenerator>();
        /*
        Vector3[] points = new Vector3[] { new Vector3(0f,50.0f,0f),
            new Vector3(15.0f,45f,0f),
            new Vector3(30f,40f,0f),
            new Vector3(45f,35f,0f)};

    */

        //_lineManager.Points = points;
    }

    private Vector3 GPSToWorldSpace(Vector2 gps)
    {
        Vector3 worldSpace = Vector3.back*0.5f* _dataContainer.gpsScale;

        worldSpace = Quaternion.AngleAxis(-gps.x, Vector3.up) * worldSpace;
        worldSpace = Quaternion.AngleAxis(gps.y, Vector3.right) * worldSpace;

        //Vector3 basePoint = new ve


        return worldSpace;
    } 

    // Update is called once per frame
    void Update()
    {
        if (Input.GetMouseButton(0) || Input.GetMouseButtonDown(1))
        {
            if (Input.GetMouseButton(0))
            {
                _lineManager.SetEndLine(_dataContainer.cursorGPS);

            }
            else if (Input.GetMouseButtonDown(1))
            {
                _lineManager.SetStartLine(_dataContainer.cursorGPS);

            }

            List<Vector3> pointsPeters = new List<Vector3>();
            List<Vector3> pointsAEQD = new List<Vector3>();
            List<Vector3> pointsMercator = new List<Vector3>();
            List<Vector3> pointsGPS = new List<Vector3>();

            foreach (var point in _lineManager.Points)
            {
                //print(point.ToString());
                Vector3 vAEQD= _dataContainer.GetNormalizedPoint(new Vector2(point.x, point.y), ProjectionType.AEQD);
                Vector3 vPeters = _dataContainer.GetNormalizedPoint(new Vector2(point.x, point.y), ProjectionType.Peters);
                Vector3 vMercator = _dataContainer.GetNormalizedPoint(new Vector2(point.x, point.y), ProjectionType.Mercator);


                pointsPeters.Add(new Vector3(vPeters.x, vPeters.y, 0f));
                pointsAEQD.Add(new Vector3(vAEQD.x,vAEQD.y, 0f));
                pointsMercator.Add(new Vector3(vMercator.x*10f, vMercator.y*10f, 0f));

                pointsGPS.Add(GPSToWorldSpace(point));
            }

            _lineManager.SetLine(ProjectionType.Peters, pointsPeters.ToArray());
            _lineManager.SetLine(ProjectionType.AEQD, pointsAEQD.ToArray());
            _lineManager.SetLine(ProjectionType.Mercator, pointsMercator.ToArray());
            _lineManager.SetLine(ProjectionType.GPS, pointsGPS.ToArray());

        }
    }
}
