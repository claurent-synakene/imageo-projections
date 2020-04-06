using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using System.Linq;

public class CoordinatesConverter : MonoBehaviour
{

    public struct Coordinates
    {
        // Constructor
        public Coordinates(ProjectionType type, Vector2 coord,bool UVSpace = false)
        {
            CoordinatesDictionary = new Dictionary<ProjectionType, Vector2>();
            CoordinatesDictionary.Add(type, coord);
            _UVSpace = UVSpace;
        }

        // Enums

        // Properties
        public Dictionary<ProjectionType, Vector2> CoordinatesDictionary;
        bool _UVSpace;
        // Methods
        public void CompleteDictionary()
        {
            if (CoordinatesDictionary != null)
            {
                if (CoordinatesDictionary.Count == 1)
                    CompleteDictionary(CoordinatesDictionary.First().Key);
            }
        }

        public void CompleteDictionary(ProjectionType knownCoordinate)
        {
            if (knownCoordinate == ProjectionType.GPS)
            {
                Vector2 gps = CoordinatesDictionary[ProjectionType.GPS];
                CoordinatesData accurateData = GetAccurateDataFromGPS(gps);

                foreach (var type in (ProjectionType[])Enum.GetValues(typeof(ProjectionType)))
                {
                    if (type != knownCoordinate)
                    {
                        Vector2 reconstructedAccurateCoordinate = accurateData.GetCoordinates(type);
                        CoordinatesDictionary.Add(type, reconstructedAccurateCoordinate);
                    }
                }
            }
            else
            {
                Vector2 coord = CoordinatesDictionary[knownCoordinate];

                print(coord.ToString() + _UVSpace.ToString());
                CoordinatesDictionary.Remove(knownCoordinate);


                Vector2 gps= JSonParser.INSTANCE.GetGPSFrom(knownCoordinate, coord, _UVSpace);
                CoordinatesDictionary.Add(ProjectionType.GPS, gps);

                CoordinatesData accurateData = GetAccurateDataFromGPS(gps);

                foreach (var type in (ProjectionType[])Enum.GetValues(typeof(ProjectionType)))
                {
                    if (type!= ProjectionType.GPS)
                    {
                        Vector2 reconstructedAccurateCoordinate = accurateData.GetCoordinates(type);
                        CoordinatesDictionary.Add(type, reconstructedAccurateCoordinate);
                    }
                }
            }
        }

        private CoordinatesData GetAccurateDataFromGPS(Vector2 gps)
        {
            CoordinatesData topLeft, topRight, bottomLeft, bottomRight;
            int[] topLeftGPS = new int[2];
            topLeftGPS[0] = Mathf.FloorToInt(gps.x);
            topLeftGPS[1] = Mathf.CeilToInt(gps.y);

            int[] topRightGPS = new int[2];
            topRightGPS[0] = Mathf.CeilToInt(gps.x);
            topRightGPS[1] = Mathf.CeilToInt(gps.y);

            int[] bottomLeftGPS = new int[2];
            bottomLeftGPS[0] = Mathf.FloorToInt(gps.x);
            bottomLeftGPS[1] = Mathf.FloorToInt(gps.y);

            int[] bottomRightGPS = new int[2];
            bottomRightGPS[0] = Mathf.CeilToInt(gps.x);
            bottomRightGPS[1] = Mathf.FloorToInt(gps.y);

            //print(gps.ToString());
            topLeft = JSonParser.INSTANCE.CoordinatesDictionary[new GPS(topLeftGPS[0], topLeftGPS[1])];
            topRight = JSonParser.INSTANCE.CoordinatesDictionary[new GPS(topRightGPS[0], topRightGPS[1])];
            bottomLeft = JSonParser.INSTANCE.CoordinatesDictionary[new GPS(bottomLeftGPS[0], bottomLeftGPS[1])];
            bottomRight = JSonParser.INSTANCE.CoordinatesDictionary[new GPS(bottomRightGPS[0], bottomRightGPS[1])];

            Vector2 lerpedAEQD, lerpedMercator, lerpedPeters;

            Vector2 lerpGPS = new Vector2(Mathf.Abs( gps.x) % 1f, Mathf.Abs(gps.y) % 1f);

            if (gps.x<0f)
                lerpGPS.x = 1f - lerpGPS.x;
            if (gps.y >0f)
                lerpGPS.y = 1f - lerpGPS.y;

            lerpedAEQD = QuadLerp(topLeft.GetCoordinates(ProjectionType.AEQD), topRight.GetCoordinates(ProjectionType.AEQD), bottomRight.GetCoordinates(ProjectionType.AEQD), bottomLeft.GetCoordinates(ProjectionType.AEQD), lerpGPS.x, lerpGPS.y);
            lerpedMercator = QuadLerp(topLeft.GetCoordinates(ProjectionType.Mercator), topRight.GetCoordinates(ProjectionType.Mercator), bottomRight.GetCoordinates(ProjectionType.Mercator), bottomLeft.GetCoordinates(ProjectionType.Mercator), lerpGPS.x, lerpGPS.y);
            lerpedPeters = QuadLerp(topLeft.GetCoordinates(ProjectionType.Peters), topRight.GetCoordinates(ProjectionType.Peters), bottomRight.GetCoordinates(ProjectionType.Peters), bottomLeft.GetCoordinates(ProjectionType.Peters), lerpGPS.x, lerpGPS.y);

            //print(lerpedMercator.ToString());

            CoordinatesData returnData = new CoordinatesData(lerpedAEQD, lerpedMercator, lerpedPeters, gps);
            return returnData;
        }

        public Vector2 QuadLerp(Vector2 a, Vector2 b, Vector2 c, Vector2 d, float u, float v)
        {
            Vector2 abu = Vector2.Lerp(a, b, u);
            Vector2 dcu = Vector2.Lerp(d, c, u);
            return Vector2.Lerp(abu, dcu, v);
        }
    }

    private static JSonParser _jsp;
    public List<Vector2> PointList;
    public List<string> NamesList;
    private List<Coordinates> _coordinatesList;

    public Transform gpsTransform;
    public Transform mercatorTransform;
    public Transform petersTransform;
    public Transform aeqdTransform;



    public Vector3 gpsPosition;
    public ProjectionType CurrentControl;
    private Dictionary<ProjectionType, Transform> Cursors;
    public TextMesh TextGPS, TextGPS2;

    public float scale = .01f;

    void Start()
    {
        _jsp = GetComponent<JSonParser>();

        Cursors = new Dictionary<ProjectionType, Transform>();

        Cursors.Add(ProjectionType.GPS, gpsTransform);
        Cursors.Add(ProjectionType.Mercator, mercatorTransform);
        Cursors.Add(ProjectionType.Peters, petersTransform);
        Cursors.Add(ProjectionType.AEQD, aeqdTransform);
    }

    // Update is called once per frame
    void Update()
    {
        Coordinates point;
        /*
        if (ControlGPS)
            gpsTransform.localPosition = new Vector3(Mathf.Clamp(gpsPosition.x, -179f, 179f), 0f, Mathf.Clamp(gpsPosition.z, -85f, 85f));
        else
            gpsTransform.localPosition = new Vector3(Mathf.Clamp(gpsTransform.localPosition.x, -179f, 179f), 0f, Mathf.Clamp(gpsTransform.localPosition.z, -85f, 85f));
            */

        /*
        point = CreatePoint(CurrentControl, Cursors[CurrentControl].localPosition);

        foreach (var type in (ProjectionType[])Enum.GetValues(typeof(ProjectionType)))
        {
            if (type != CurrentControl)
            {
                Vector2 coord = point.CoordinatesDictionary[type];
                Vector2 relativeCoord = JSonParser.INSTANCE.GetInverseLerpMinMax(type, coord);
                Cursors[type].localPosition = new Vector3(relativeCoord.x * scale, 0f, relativeCoord.y * scale);
            }
        }
        */
    }

    private Coordinates CreatePoint(ProjectionType type, Vector3 coord, bool UVSpace= false)
    {
        return CreatePoint(type, new Vector2(coord.x, coord.z),UVSpace);
    }

    private Coordinates CreatePoint(ProjectionType type, Vector2 coord, bool UVSpace = false)
    {
        Coordinates point = new Coordinates(type, coord, UVSpace);

        point.CompleteDictionary();
        return point;
    }

    public List<Coordinates> TransformPoints(List<Vector2> points, ProjectionType projection)
    {
        List<Coordinates> transformedCoordinates = new List<Coordinates>();
        return transformedCoordinates;
    }

    
    [ContextMenu("TransformPointSerie")]
    public void TransformPoints(ProjectionType projection = ProjectionType.GPS, bool UVSpace = false)
    {
        if (_coordinatesList== null)
            _coordinatesList = new List<Coordinates>();
        else
            _coordinatesList.Clear();
        List<Vector2> newPoints = new List<Vector2>();
        foreach (var point in PointList)
        {
            Coordinates coords = CreatePoint(projection,UVSpace? new Vector2(point.x, point.y): new Vector2(point.y, point.x), UVSpace);
            _coordinatesList.Add(coords);
            newPoints.Add(coords.CoordinatesDictionary[ProjectionType.GPS]);
        }

        if (projection != ProjectionType.GPS)
        {
            PointList.Clear();
            PointList.AddRange(newPoints);
        }
        

        UpdatePoints();
    }

    public void ClearPathsTrail()
    {
        foreach (var type in (ProjectionType[])Enum.GetValues(typeof(ProjectionType)))
        {
            Cursors[type].GetComponent<TrailRenderer>().Clear();
            Cursors[type].GetComponent<TrailRenderer>().enabled = false;
        }

    }
    public void UpdatePoints()
    {
        foreach (var type in (ProjectionType[])Enum.GetValues(typeof(ProjectionType)))
        {
            Cursors[type].GetComponent<TrailRenderer>().enabled = true;
        }
        StartCoroutine(UpdatePointsCR());
    }



    private IEnumerator UpdatePointsCR()
    {
        int i = 0;
        foreach (var point in _coordinatesList)
        {

            TextGPS.text = "Long : "+point.CoordinatesDictionary[ProjectionType.GPS].x.ToString()+" - Lat : "+point.CoordinatesDictionary[ProjectionType.GPS].y.ToString();
            foreach (var type in (ProjectionType[])Enum.GetValues(typeof(ProjectionType)))
            {
                //if (type != CurrentControl)
                {
                    Vector2 coord = point.CoordinatesDictionary[type];
                    print(type.ToString() + " - " + coord.ToString());
                    Vector2 relativeCoord = JSonParser.INSTANCE.GetInverseLerpMinMax(type, coord);
                    Cursors[type].localPosition = new Vector3(relativeCoord.x * scale, 0f, relativeCoord.y * scale);
                    if (i==0 && type != ProjectionType.GPS )
                    {
                        Cursors[type].GetComponent<TrailRenderer>().Clear();

                    }
                }
            }
            TextGPS2.text = NamesList[i];
            i++;
            yield return new WaitForSeconds(.1f);
        }
    }

}
