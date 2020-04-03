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
        public Coordinates(ProjectionType type, Vector2 coord)
        {
            CoordinatesDictionary = new Dictionary<ProjectionType, Vector2>();
            CoordinatesDictionary.Add(type, coord);
        }

        // Enums

        // Properties
        public Dictionary<ProjectionType, Vector2> CoordinatesDictionary;

        // Methods
        public void CompleteDictionary()
        {
            if (CoordinatesDictionary != null)
            {
                if (CoordinatesDictionary.Count == 1)
                {
                    CompleteDictionary(CoordinatesDictionary.First().Key);
                }
            }
        }

        public void CompleteDictionary(ProjectionType knownCoordinate)
        {
            Vector2 gps = CoordinatesDictionary[ProjectionType.GPS];

            CoordinatesData accurateData = GetAccurateDataFromGPS(gps);

            foreach (var type in (ProjectionType[])Enum.GetValues(typeof(ProjectionType)))
            {
                if (type != knownCoordinate)
                {
                    Vector2 reconstructedAccurateCoordinate = accurateData.GetCoordinates(type);

                    CoordinatesDictionary.Add(type, reconstructedAccurateCoordinate);

                    //Debug.Log("Reconstructed accurate : " + type.ToString() + " - " + reconstructedAccurateCoordinate.ToString());
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
    private List<Coordinates> _coordinatesList;

    public Transform gpsTransform;
    public Transform mercatorTransform;
    public Transform petersTransform;
    public Transform aeqdTransform;

    public Vector3 gpsPosition;
    public bool ControlGPS = false;
    public bool ControlMercator = false;

    public float scale = .01f;

    void Start()
    {
        _jsp = GetComponent<JSonParser>();

        /*
        _coordinatesList = new List<Coordinates>();

        Coordinates newPoint = CreatePoint(ProjectionType.GPS, new Vector2(46.2f, 2.2f));

        _coordinatesList.Add(newPoint);


        foreach (var point in _coordinatesList)
        {
            point.CompleteDictionary();
        }
        */

    }

    private Coordinates CreatePoint( ProjectionType type, Vector2 coord)
    {
        Coordinates point = new Coordinates(type, coord);

        point.CompleteDictionary();
        return point;

    }

    // Update is called once per frame
    void Update()
    {
        Coordinates point;

        if (ControlGPS)
            gpsTransform.localPosition = new Vector3(Mathf.Clamp(gpsPosition.x, -179f, 179f), 0f, Mathf.Clamp(gpsPosition.z, -85f, 85f));
        else
            gpsTransform.localPosition = new Vector3(Mathf.Clamp(gpsTransform.localPosition.x, -179f, 179f), 0f, Mathf.Clamp(gpsTransform.localPosition.z, -85f, 85f));


        point = new Coordinates(ProjectionType.GPS, new Vector2(gpsTransform.localPosition.x, gpsTransform.localPosition.z));

        point.CompleteDictionary();

        Vector2 merc = point.CoordinatesDictionary[ProjectionType.Mercator];
        Vector2 peters = point.CoordinatesDictionary[ProjectionType.Peters];
        Vector2 aeqd = point.CoordinatesDictionary[ProjectionType.AEQD];
        //Vector2 peters = point.CoordinatesDictionary[ProjectionType.Peters];
        //petersTransform.localPosition = new Vector3(peters.x * scale, peters.y * scale, 0f);
        Vector2 relativeMerc = JSonParser.INSTANCE.GetInverseLerpMinMax(ProjectionType.Mercator, merc);
        Vector2 relativePeters = JSonParser.INSTANCE.GetInverseLerpMinMax(ProjectionType.Peters, peters);
        Vector2 relativeAEQD = JSonParser.INSTANCE.GetInverseLerpMinMax(ProjectionType.AEQD, aeqd);
        //print(merc.ToString());

        // inverse operation
        Vector2 gpsFromMerc= JSonParser.INSTANCE.GetGPSFrom(ProjectionType.Mercator, relativeMerc);

        mercatorTransform.localPosition = new Vector3(relativeMerc.x * scale,0f, relativeMerc.y * scale);
        petersTransform.localPosition = new Vector3(relativePeters.x * scale,0f, relativePeters.y * scale);
        aeqdTransform.localPosition = new Vector3(relativeAEQD.x * scale, 0f, relativeAEQD.y * scale);

        //print("GPS : "+ gpsFromMerc.ToString());

    }

    public Coordinates CreatePoint(ProjectionType type, Vector2 coord)
    {
        return new Coordinates(type, coord);
    }

}
