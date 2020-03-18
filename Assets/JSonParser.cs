using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// Json serialisation

[System.Serializable]
public class Coordinates
{
    public ProjectionData[] coordinates;
}

[System.Serializable]
public class ProjectionData
{
    public int X_wgs84;
    public int Y_wgs84;

    public int X_aeqd;
    public int Y_aeqd;

    public int X_merc;
    public int Y_merc;

    public int X_peters;
    public int Y_peters;

}

// How we store it in the dictionary

public struct GPS
{
    public GPS(int x, int y)
    {
        X = x;
        Y = y;
    }

    public int X;
    public int Y;
}

public struct CoordinatesData
{
    public CoordinatesData(int x_aeqd, int y_aeqd, int x_merc, int y_merc, int x_peters, int y_peters, int x_gps, int y_gps)
    {
        X_aeqd = x_aeqd;
        Y_aeqd = y_aeqd;

        X_merc = x_merc;
        Y_merc = y_merc;

        X_peters = x_peters;
        Y_peters = y_peters;

        X_GPS = x_peters;
        Y_GPS = y_peters;
    }

    public int X_aeqd;
    public int Y_aeqd;

    public int X_merc;
    public int Y_merc;

    public int X_peters;
    public int Y_peters;

    public int X_GPS;
    public int Y_GPS;
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

    void Start()
    {
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


    [ContextMenu("Parse")]
    public void Parse()
    {
        CoordinatesDictionary = new Dictionary<GPS, CoordinatesData>();
        MinMaxDictionary = new Dictionary<ProjectionType, MinMax>();

        MinMaxDictionary.Add(ProjectionType.GPS, new MinMax(0));
        MinMaxDictionary.Add(ProjectionType.Mercator, new MinMax(0));
        MinMaxDictionary.Add(ProjectionType.Peters, new MinMax(0));
        MinMaxDictionary.Add(ProjectionType.AEQD, new MinMax(0));

        Coordinates Data = JsonUtility.FromJson<Coordinates>(jsonFile.text);

        foreach (ProjectionData c in Data.coordinates)
        {

            {
                CheckMinMax(c.X_wgs84, c.Y_wgs84, ProjectionType.GPS);
                CheckMinMax(c.X_merc, c.Y_merc, ProjectionType.Mercator);
                CheckMinMax(c.X_peters, c.X_peters, ProjectionType.Peters);
                CheckMinMax(c.X_aeqd, c.Y_aeqd, ProjectionType.AEQD);
                /*
                if (c.X_wgs84 < GPS_MinMax.x[0])
                    GPS_MinMax.x[0] = c.X_wgs84;
                if (c.X_wgs84 > GPS_MinMax.x[1])
                    GPS_MinMax.x[1] = c.X_wgs84;

                if (c.Y_wgs84 < GPS_MinMax.y[0])
                    GPS_MinMax.y[0] = c.Y_wgs84;
                if (c.Y_wgs84 > GPS_MinMax.y[1])
                    GPS_MinMax.y[1] = c.Y_wgs84;
                // -----------------------------

                if (c.X_merc < Mercator_MinMax.x[0])
                    Mercator_MinMax.x[0] = c.X_merc;
                if (c.X_merc > Mercator_MinMax.x[1])
                    Mercator_MinMax.x[1] = c.X_merc;

                if (c.Y_merc < Mercator_MinMax.y[0])
                    Mercator_MinMax.y[0] = c.Y_merc;
                if (c.Y_merc > Mercator_MinMax.y[1])
                    Mercator_MinMax.y[1] = c.Y_merc;

                // -----------------------------
                if (c.X_aeqd < Aeqd_MinMax.x[0])
                    Aeqd_MinMax.x[0] = c.X_aeqd;
                if (c.X_aeqd > Aeqd_MinMax.x[1])
                    Aeqd_MinMax.x[1] = c.X_aeqd;

                if (c.Y_aeqd < Aeqd_MinMax.y[0])
                    Aeqd_MinMax.y[0] = c.Y_aeqd;
                if (c.Y_aeqd > Aeqd_MinMax.y[1])
                    Aeqd_MinMax.y[1] = c.Y_aeqd;

                // -----------------------------

                if (c.X_peters < Peters_MinMax.x[0])
                    Peters_MinMax.x[0] = c.X_peters;
                if (c.X_peters > Peters_MinMax.x[1])
                    Peters_MinMax.x[1] = c.X_peters;

                if (c.Y_peters < Peters_MinMax.y[0])
                    Peters_MinMax.y[0] = c.Y_peters;
                if (c.Y_peters > Peters_MinMax.y[1])
                    Peters_MinMax.y[1] = c.Y_peters;
                    */
                CoordinatesDictionary.Add(new GPS(c.X_wgs84, c.Y_wgs84),
                    new CoordinatesData(c.X_aeqd, c.Y_aeqd, c.X_merc, c.Y_merc, c.X_peters, c.Y_peters, c.X_wgs84, c.Y_wgs84));
            }
            

            //Debug.Log("Found coordinate : " + coordinate.firstName + " " + coordinate.lastName);
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
