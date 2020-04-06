using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// Json serialisation

[System.Serializable]
public class CoordinatesJSon
{
    public ProjectionData[] coordinates;
}

[System.Serializable]
public class ProjectionData
{
    public float X_wgs84;
    public float Y_wgs84;

    public float X_aeqd;
    public float Y_aeqd;

    public float X_merc;
    public float Y_merc;

    public float X_peters;
    public float Y_peters;

}

// How we store it in the dictionary

public struct GPS
{
    public GPS(float x, float y)
    {
        X = x;
        Y = y;
    }

    public float X;
    public float Y;
}

public struct CoordinatesData
{
    public CoordinatesData(Vector2 aeqd, Vector2 merc, Vector2 peters, Vector2 gps)
    {
        X_aeqd = aeqd.x;
        Y_aeqd = aeqd.y;

        X_merc = merc.x;
        Y_merc = merc.y;

        X_peters = peters.x;
        Y_peters = peters.y;

        X_GPS = gps.x;
        Y_GPS = gps.y;

    }
    public CoordinatesData(float x_aeqd, float y_aeqd, float x_merc, float y_merc, float x_peters, float y_peters, float x_gps, float y_gps)
    {
        X_aeqd = x_aeqd;
        Y_aeqd = y_aeqd;

        X_merc = x_merc;
        Y_merc = y_merc;

        X_peters = x_peters;
        Y_peters = y_peters;

        X_GPS = x_gps;
        Y_GPS = y_gps;
    }

    public Vector2 GetCoordinates(ProjectionType type)
    {
        Vector2 returnValue = Vector2.zero;
        switch (type)
        {
            case ProjectionType.GPS:
                returnValue = new Vector2(X_GPS, Y_GPS);
                break;
            case ProjectionType.AEQD:
                returnValue = new Vector2(X_aeqd, Y_aeqd);
                break;
            case ProjectionType.Mercator:
                returnValue = new Vector2(X_merc, Y_merc);
                break;
            case ProjectionType.Peters:
                returnValue = new Vector2(X_peters, Y_peters);
                break;
            default:
                break;
        }

        return returnValue;
    }

    public float X_aeqd;
    public float Y_aeqd;

    public float X_merc;
    public float Y_merc;

    public float X_peters;
    public float Y_peters;

    public float X_GPS;
    public float Y_GPS;
}

[SerializeField]
public class MinMax
{
    public MinMax(int init)
    {
        x = new int[2];
        y = new int[2];

        x[0] = 0;
        x[1] = 0;
        y[0] = 0;
        y[1] = 0;

        width = 0;
        heigth = 0;

    }
    [SerializeField]
    public int[] x,y;

    public int width, heigth;

    public void ComputeSize()
    {
        width = x[1] - x[0];
        heigth = y[1] - y[0];

        Debug.Log("Width/weigth : " + width.ToString() + " / " + heigth.ToString());
    }
}

public enum ProjectionType
{
    GPS,
    Mercator,
    Peters,
    AEQD
}

public class JSonParser : MonoBehaviour
{
    public TextAsset jsonFile;
    public Texture2D TextureMerc;

    /*
    public AnimationCurve[] animCurveMercator;
    public AnimationCurve[] animCurvePeters;
    */
    public AnimationCurve MercatorReconstructCurve;
    public AnimationCurve PetersReconstructCurve;
    public AnimationCurve AEQDReconstructCurve;

    public AnimationCurve[] animCurveMercatorNormalized;
    public AnimationCurve[] animCurvePetersNormalized;
    public AnimationCurve[] animCurveAEQDNormalized;

    public Dictionary<ProjectionType, AnimationCurve[]> CurveDictionary;

    public AnimationCurve customCurve;

    public static JSonParser INSTANCE;
    //public MinMax GPS_MinMax = new MinMax(0);
    //public MinMax Mercator_MinMax = new MinMax(0);
    //public MinMax Aeqd_MinMax = new MinMax(0);
    //public MinMax Peters_MinMax = new MinMax(0);

    //public int width, height;
    //public int merc_width, merc_height;
    //public int aeqd_width, aeqd_height;
    //public int peters_width, peters_height;


    public Dictionary<GPS, CoordinatesData> CoordinatesDictionary;
    public Dictionary<ProjectionType, MinMax> MinMaxDictionary;

    public bool RegenCurves = false;

    private float _yAxis = 0f;

    void Start()
    {
        INSTANCE = this;


        if (RegenCurves)
        {
            
            animCurveMercatorNormalized = new AnimationCurve[2];

            animCurveMercatorNormalized[0] = new AnimationCurve();
            animCurveMercatorNormalized[1] = new AnimationCurve();

            animCurvePetersNormalized = new AnimationCurve[2];

            animCurvePetersNormalized[0] = new AnimationCurve();
            animCurvePetersNormalized[1] = new AnimationCurve();

            animCurveAEQDNormalized = new AnimationCurve[2];

            animCurveAEQDNormalized[0] = new AnimationCurve();
            animCurveAEQDNormalized[1] = new AnimationCurve();
            

        }
        CurveDictionary = new Dictionary<ProjectionType, AnimationCurve[]>();
        CurveDictionary.Add(ProjectionType.Mercator, animCurveMercatorNormalized);
        CurveDictionary.Add(ProjectionType.Peters, animCurvePetersNormalized);
        CurveDictionary.Add(ProjectionType.AEQD, animCurveAEQDNormalized);

        Parse();
    }

    private void CheckMinMax(int valueX, int valueY, ProjectionType projection)
    {
        if (valueX < MinMaxDictionary[projection].x[0])
            MinMaxDictionary[projection].x[0] = valueX;
        if (valueX > MinMaxDictionary[projection].x[1])
            MinMaxDictionary[projection].x[1] = valueX;

        if (valueY < MinMaxDictionary[projection].y[0])
            MinMaxDictionary[projection].y[0] = valueY;
        if (valueY > MinMaxDictionary[projection].y[1])
            MinMaxDictionary[projection].y[1] = valueY;

    }

    private void PrintMinMax( ProjectionType projection)
    {
        print(projection.ToString() + 
            MinMaxDictionary[projection].x[0] + " / " + MinMaxDictionary[projection].x[1] + " - " +
            MinMaxDictionary[projection].y[0] + " / " + MinMaxDictionary[projection].y[1]);

    }

    public Vector2 GetInverseLerpMinMax(ProjectionType type, Vector2 coord)
    {

        MinMax minMax = MinMaxDictionary[type];

        float Xaxis = Mathf.InverseLerp(minMax.x[0], minMax.x[1], coord.x);
        float Yaxis = Mathf.InverseLerp(minMax.y[0], minMax.y[1], coord.y);
        //print(Yaxis.ToString());

        switch (type)
        {
            case ProjectionType.Mercator:
                Yaxis = MercatorReconstructCurve.Evaluate(Yaxis);
                break;
            case ProjectionType.Peters:
                Yaxis = PetersReconstructCurve.Evaluate(Yaxis);
                break;
            case ProjectionType.AEQD:
                Yaxis = AEQDReconstructCurve.Evaluate(Yaxis);
                break;
            default:
                break;
        }
        //Yaxis = customCurve.Evaluate(Yaxis);

        if (type== ProjectionType.AEQD)
        {
           // _yAxis = Yaxis;

        }
        return new Vector2(Xaxis, Yaxis);
    }


    public Vector2 GetGPSFrom(ProjectionType type,Vector2 coord,bool UVSpace = false)
    {
        Vector2 returnCoord = Vector2.zero;

        returnCoord = new Vector2(CurveDictionary[type][0].Evaluate(coord.x), CurveDictionary[type][1].Evaluate(coord.y));

        return returnCoord;
    }

    [ContextMenu("AddPoint")]
    public void AddPoint()
    {

        customCurve.AddKey(_yAxis, 0.5f);
    }


    [ContextMenu("Parse")]
    public void Parse()
    {
        CoordinatesDictionary = new Dictionary<GPS, CoordinatesData>();
        MinMaxDictionary = new Dictionary<ProjectionType, MinMax>();

        MinMaxDictionary.Add(ProjectionType.GPS, new MinMax(0));
        MinMaxDictionary.Add(ProjectionType.Mercator, new MinMax(0));
        MinMaxDictionary.Add(ProjectionType.Peters, new MinMax(0));
        MinMaxDictionary.Add(ProjectionType.AEQD, new MinMax(0));

        CoordinatesJSon Data = JsonUtility.FromJson<CoordinatesJSon>(jsonFile.text);

        foreach (ProjectionData c in Data.coordinates)
        {

            {
                CheckMinMax(Mathf.FloorToInt(c.X_wgs84), Mathf.FloorToInt(c.Y_wgs84), ProjectionType.GPS);
                CheckMinMax(Mathf.FloorToInt(c.X_merc), Mathf.FloorToInt(c.Y_merc), ProjectionType.Mercator);
                CheckMinMax(Mathf.FloorToInt(c.X_peters), Mathf.FloorToInt(c.X_peters), ProjectionType.Peters);
                CheckMinMax(Mathf.FloorToInt(c.X_aeqd), Mathf.FloorToInt(c.Y_aeqd), ProjectionType.AEQD);
                
                CoordinatesDictionary.Add(new GPS(c.X_wgs84, c.Y_wgs84),
                    new CoordinatesData(c.X_aeqd, c.Y_aeqd, c.X_merc, c.Y_merc, c.X_peters, c.Y_peters, c.X_wgs84, c.Y_wgs84));
            }
        }


        if (RegenCurves)
        {
            for (float i = -89f; i < 90f; i++)
            {
                if (CoordinatesDictionary.ContainsKey(new GPS(0f,i)))
                {
                    var data = CoordinatesDictionary[new GPS(0f, i)];

                    float yMerc = data.Y_merc;
                    float yPeters = data.Y_peters;
                    float yAEQD = data.Y_aeqd;

                    /*
                    animCurveMercator[1].AddKey(i, yMerc);
                    animCurveMercator[3].AddKey(yMerc,i);
                    animCurvePeters[1].AddKey(i, yPeters);
                    animCurvePeters[3].AddKey(yPeters,i);
                    */
                    //animCurveMercatorNormalized[1].AddKey(i, GetInverseLerpMinMax(ProjectionType.Mercator, new Vector2(0f, yMerc)).y);
                    //animCurvePetersNormalized[1].AddKey(i, GetInverseLerpMinMax(ProjectionType.Peters, new Vector2(0f, yPeters)).y);
                    CurveDictionary[ProjectionType.Mercator][1].AddKey(GetInverseLerpMinMax(ProjectionType.Mercator, new Vector2(0f, yMerc)).y, i);
                    CurveDictionary[ProjectionType.Peters][1].AddKey(GetInverseLerpMinMax(ProjectionType.Peters, new Vector2(0f, yPeters)).y, i);
                    CurveDictionary[ProjectionType.AEQD][1].AddKey(GetInverseLerpMinMax(ProjectionType.AEQD, new Vector2(0f, yAEQD)).y, i);

                    //animCurveMercatorNormalized[3].AddKey(GetInverseLerpMinMax(ProjectionType.Mercator, new Vector2(0f, yMerc)).y,i);
                    //animCurvePetersNormalized[3].AddKey(GetInverseLerpMinMax(ProjectionType.Peters, new Vector2(0f, yPeters)).y,i);
                }
            }

            for (float i = -180f; i < 180f; i++)
            {
                if (CoordinatesDictionary.ContainsKey(new GPS(i, 0f)))
                {
                    var data = CoordinatesDictionary[new GPS(i, 0f)];

                    float xMerc = data.X_merc;
                    float xPeters = data.X_peters;
                    float xAEQD = data.X_aeqd;
                    /*
                    animCurveMercator[0].AddKey(i, xMerc);
                    animCurvePeters[0].AddKey(i, xPeters);
                    animCurveMercator[2].AddKey(xMerc,i);
                    animCurvePeters[2].AddKey(xPeters,i);
                    */
                    //animCurveMercatorNormalized[0].AddKey(i, GetInverseLerpMinMax(ProjectionType.Mercator, new Vector2(xMerc, 0f)).x);
                    //animCurvePetersNormalized[0].AddKey(i, GetInverseLerpMinMax(ProjectionType.Peters, new Vector2(xPeters,0f)).x);

                    CurveDictionary[ProjectionType.Mercator][0].AddKey(GetInverseLerpMinMax(ProjectionType.Mercator, new Vector2(xMerc,0f )).x, i);
                    CurveDictionary[ProjectionType.Peters][0].AddKey(GetInverseLerpMinMax(ProjectionType.Peters, new Vector2(xPeters,0f )).x, i);
                    CurveDictionary[ProjectionType.AEQD][0].AddKey(GetInverseLerpMinMax(ProjectionType.AEQD, new Vector2(xAEQD, 0f )).x, i);
                    //animCurveMercatorNormalized[2].AddKey(GetInverseLerpMinMax(ProjectionType.Mercator, new Vector2(xMerc, 0f)).x,i);
                    //animCurvePetersNormalized[2].AddKey(GetInverseLerpMinMax(ProjectionType.Peters, new Vector2(xPeters, 0f)).x,i);
                }   
            }
        }
        



        PrintMinMax(ProjectionType.GPS);
        PrintMinMax(ProjectionType.Mercator);
        PrintMinMax(ProjectionType.Peters);
        PrintMinMax(ProjectionType.AEQD);

        MinMaxDictionary[ProjectionType.GPS].ComputeSize();
        MinMaxDictionary[ProjectionType.Mercator].ComputeSize();
        MinMaxDictionary[ProjectionType.Peters].ComputeSize();
        MinMaxDictionary[ProjectionType.AEQD].ComputeSize();

    }

}
