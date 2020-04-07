using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
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

        // Properties

        public Dictionary<ProjectionType, Vector2> CoordinatesDictionary;
        bool _UVSpace; // possiblement inutile

        // Methods

        public void CompleteDictionary() // overload pour complete automatiquement sans avoir à specifier le type
        {
            if (CoordinatesDictionary != null)
            {
                if (CoordinatesDictionary.Count == 1)
                    CompleteDictionary(CoordinatesDictionary.First().Key);
            }
        }

        public void CompleteDictionary(ProjectionType knownCoordinate) // complete automatiquement le reste des coordonnees a partir d'une seule.
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

                CoordinatesDictionary.Remove(knownCoordinate);

                Vector2 gps= JSonParser.INSTANCE.GetGPSFrom(knownCoordinate, coord);

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

        private CoordinatesData GetAccurateDataFromGPS(Vector2 gps) // Interpolation bilineaire entre 4 points dans le dictionnaire
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

            Vector2 lerpedAEQD, lerpedMercator, lerpedPeters, lerpedGPS = new Vector2(Mathf.Abs(gps.x) % 1f, Mathf.Abs(gps.y) % 1f);

            if (gps.x<0f)
                lerpedGPS.x = 1f - lerpedGPS.x;
            if (gps.y >0f)
                lerpedGPS.y = 1f - lerpedGPS.y;

            lerpedAEQD = QuadLerp(topLeft.GetCoordinates(ProjectionType.AEQD), topRight.GetCoordinates(ProjectionType.AEQD), bottomRight.GetCoordinates(ProjectionType.AEQD), bottomLeft.GetCoordinates(ProjectionType.AEQD), lerpedGPS.x, lerpedGPS.y);
            lerpedMercator = QuadLerp(topLeft.GetCoordinates(ProjectionType.Mercator), topRight.GetCoordinates(ProjectionType.Mercator), bottomRight.GetCoordinates(ProjectionType.Mercator), bottomLeft.GetCoordinates(ProjectionType.Mercator), lerpedGPS.x, lerpedGPS.y);
            lerpedPeters = QuadLerp(topLeft.GetCoordinates(ProjectionType.Peters), topRight.GetCoordinates(ProjectionType.Peters), bottomRight.GetCoordinates(ProjectionType.Peters), bottomLeft.GetCoordinates(ProjectionType.Peters), lerpedGPS.x, lerpedGPS.y);

            return new CoordinatesData(lerpedAEQD, lerpedMercator, lerpedPeters, gps);
        }

        public Vector2 QuadLerp(Vector2 a, Vector2 b, Vector2 c, Vector2 d, float u, float v)
        {
            Vector2 abu = Vector2.Lerp(a, b, u);
            Vector2 dcu = Vector2.Lerp(d, c, u);
            return Vector2.Lerp(abu, dcu, v);
        }
    }

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
    public Text TextOutput;
    public Coroutine ShowingPoints = null;

    public float scale = .01f;

    double _eQuatorialEarthRadius = 6378.1370D;
    double _d2r = (Math.PI / 180D);

    void Start()
    {
        Cursors = new Dictionary<ProjectionType, Transform>();

        Cursors.Add(ProjectionType.GPS, gpsTransform);
        Cursors.Add(ProjectionType.Mercator, mercatorTransform);
        Cursors.Add(ProjectionType.Peters, petersTransform);
        Cursors.Add(ProjectionType.AEQD, aeqdTransform);
    }

  
    private Coordinates CreatePoint(ProjectionType type, Vector3 coord, bool UVSpace= false)// overload pour ajouter directement des points vector3
    {
        return CreatePoint(type, new Vector2(coord.x, coord.z),UVSpace);
    }

    private Coordinates CreatePoint(ProjectionType type, Vector2 coord, bool UVSpace = false) // ajouter un point et completer automatiquement les coordonnées manquantes
    {
        Coordinates point = new Coordinates(type, coord, UVSpace);

        point.CompleteDictionary();
        return point;
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
    public void UpdatePoints() // Show each point in the list
    {
        if (ShowingPoints != null)
            StopCoroutine(ShowingPoints);
      
        foreach (var type in (ProjectionType[])Enum.GetValues(typeof(ProjectionType)))
            Cursors[type].GetComponent<TrailRenderer>().enabled = true;
       
        ShowingPoints =StartCoroutine(UpdatePointsCR());
    }

    private IEnumerator UpdatePointsCR()
    {
        int i = 0;

        Vector2 start= _coordinatesList[0].CoordinatesDictionary[ProjectionType.GPS], end= _coordinatesList[_coordinatesList.Count - 1].CoordinatesDictionary[ProjectionType.GPS];
        float distance = (float )HaversineInKM(start, end);
        float pointByPointDistance = 0;
        print("Distance straight : " + distance.ToString() + " between " + start.ToString() + "  and  " + end.ToString());
        foreach (var point in _coordinatesList)
        {
            foreach (var type in (ProjectionType[])Enum.GetValues(typeof(ProjectionType)))
            {
                Vector2 coord = point.CoordinatesDictionary[type];
                Vector2 relativeCoord = JSonParser.INSTANCE.GetInverseLerpMinMax(type, coord);
                Cursors[type].localPosition = new Vector3(relativeCoord.x * scale, 0f, relativeCoord.y * scale);
                if (i==0 && type != ProjectionType.GPS )
                    Cursors[type].GetComponent<TrailRenderer>().Clear();
            }
            if (i > 0)
                pointByPointDistance += (float)HaversineInKM(_coordinatesList[i - 1].CoordinatesDictionary[ProjectionType.GPS], _coordinatesList[i].CoordinatesDictionary[ProjectionType.GPS]);
            i++;

            yield return new WaitForSeconds(.1f);
        }

        TextOutput.text = "Distance la plus courte : " + distance.ToString("F0") + " - Distance tracée : " + pointByPointDistance.ToString("F0") + " - Efficacité : " + (distance / pointByPointDistance * 100f).ToString("F0") + "%";
        ShowingPoints = null;
    }

    // https://stackoverflow.com/questions/365826/calculate-distance-between-2-gps-coordinates

    private double HaversineInKM(Vector2 start, Vector2 end)
    {
        return HaversineInKM(start.y, start.x, end.y, end.x);
    }

    private double HaversineInKM(double lat1, double long1, double lat2, double long2)
    {
        double dlong = (long2 - long1) * _d2r;
        double dlat = (lat2 - lat1) * _d2r;

        double a = Math.Pow(Math.Sin(dlat / 2D), 2D) + Math.Cos(lat1 * _d2r) * Math.Cos(lat2 * _d2r) * Math.Pow(Math.Sin(dlong / 2D), 2D);
        double c = 2D * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1D - a));
        double d = _eQuatorialEarthRadius * c;

        return d;
    }
}
