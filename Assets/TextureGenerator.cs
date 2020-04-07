#if UNITY_EDITOR

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using System.IO;
using System.Linq;



[ExecuteInEditMode]
public class TextureGenerator : MonoBehaviour
{
    public Dictionary<GPS, CoordinatesData> CoordinatesDictionary;
    //public Dictionary<ProjectionType, Texture2D> TexturesDictionary;

    public InputField fieldX, fieldY;
    JSonParser parser;

    public Texture2D textureMercator, textureAEQD, texturePeters;

    public Transform sphere1, sphere2, sphere3, Rotator;

    public Vector2 PetersScale;
    public float PetersOffset;

    public Vector2 MercatorScale;
    public float aeqdScale;

    public float gpsScale;

    public Vector2 cursorGPS;

    public Slider SliderMap;

    void Start()
    {
    }

    Vector2 MercatorUVToGPS(Vector2 UV, bool debug = false)
    {

        Vector2 result = Vector2.zero;

        Vector2 value = GetMercatorCoord(new Vector2(UV.x, UV.y));

        Vector2 gps = new Vector2(
            value.x > .5f ? (1f - value.x)*-360f: value.x * 360f, 
            value.y > .5f ? (1f - value.y) * -180f : value.y * 180f);
        //print(gps.ToString());

        CoordinatesData data = GetDataFromGPS(gps);

        if (debug)
        {
            // Move Spheres Around
            sphere1.transform.localPosition = new Vector3(
                (Mathf.InverseLerp(parser.MinMaxDictionary[ProjectionType.Mercator].x[0], parser.MinMaxDictionary[ProjectionType.Mercator].x[1], data.X_merc) - 0.5f) * MercatorScale.x,
              ( Mathf.InverseLerp(parser.MinMaxDictionary[ProjectionType.Mercator].y[0], parser.MinMaxDictionary[ProjectionType.Mercator].y[1], data.Y_merc) - 0.5f) * MercatorScale.y,
               0f);


            sphere2.transform.localPosition = new Vector3(
                Mathf.InverseLerp(parser.MinMaxDictionary[ProjectionType.Peters].x[0], parser.MinMaxDictionary[ProjectionType.Peters].x[1], data.X_peters) * PetersScale.x,
               Mathf.InverseLerp(parser.MinMaxDictionary[ProjectionType.Peters].y[0], parser.MinMaxDictionary[ProjectionType.Peters].y[1], data.Y_peters) * PetersScale.y,
               0f);

            sphere3.transform.localPosition = new Vector3(
                (Mathf.InverseLerp(parser.MinMaxDictionary[ProjectionType.AEQD].x[0], parser.MinMaxDictionary[ProjectionType.AEQD].x[1], data.X_aeqd) - 0.5f) * aeqdScale,
               (Mathf.InverseLerp(parser.MinMaxDictionary[ProjectionType.AEQD].y[0], parser.MinMaxDictionary[ProjectionType.AEQD].y[1], data.Y_aeqd) - 0.5f) * aeqdScale,
               0f);

            Rotator.localEulerAngles = new Vector3(0f, -gps.x, gps.y);
        }
        return gps;
    }


    CoordinatesData GetDataFromGPS(Vector2 gps)
    {
        CoordinatesData data;

        if (parser.CoordinatesDictionary.ContainsKey(new GPS(Mathf.FloorToInt(gps.x), Mathf.FloorToInt(gps.y))))
        {
            data = parser.CoordinatesDictionary[new GPS(Mathf.FloorToInt(gps.x), Mathf.FloorToInt(gps.y))];
        }
        else
        {
            data = new CoordinatesData();
            //print("Doesnt contain key : " + gps.x + " - " + gps.y);
        }

        return data;
    }

    // To get the coordinates from clicked mercator map
    Vector2 GetMercatorCoord(Vector2 color)
    {
        Vector2 value = new Vector2();
        Color col = new Color(0, 0, 0);

        float differenceMin = 999f;
        float ValueAtMinDiffX = 999f, ValueAtMinDiffY = 999f;

        for (float u = 0; u < 1.0f; u+=0.001f)
        {
            col = textureMercator.GetPixel(Mathf.FloorToInt(u * textureMercator.width), textureMercator.height/2);

            if (Mathf.Abs(color.x - col.r) < differenceMin)
            {
                differenceMin = Mathf.Abs(color.x - col.r);
                ValueAtMinDiffX = u;
            }
        }

        // -------------------------------------
        differenceMin = 999f;

        for (float v = 0; v < 1.0f; v += 0.001f)
        {
            col = textureMercator.GetPixel(textureMercator.width / 2, Mathf.FloorToInt(v * textureMercator.height));

            if (Mathf.Abs(color.y - col.g) < differenceMin)
            {
                differenceMin = Mathf.Abs(color.y - col.g);
                ValueAtMinDiffY = v;
            }
        }
        value = new Vector2(ValueAtMinDiffX, ValueAtMinDiffY);

        return value;
    }

    // Update is called once per frame
    void Update()
    {
        if (parser== null)
            parser = GetComponent<JSonParser>();

        if (Input.GetMouseButton(0)|| Input.GetMouseButtonDown(1))
        {
            Vector3 mousePosition = Input.mousePosition;
            cursorGPS =MercatorUVToGPS(new Vector2( mousePosition.x/Screen.width,mousePosition.y/Screen.height), Input.GetMouseButton(0));
        }
    }


    /*public void SwitchToMap(Slider slider)
    {
        switch (slider.value)
        {
            case 0:
                Camera.main.transform.localPosition;
                break;
            default:
                break;
        }
        print(slider.value.ToString());
    }*/

    public CoordinatesData GetPoint( Vector2 UV)
    {
        Vector2 result = Vector2.zero;
        print("UV : " + UV.ToString());

        Vector2 value = GetMercatorCoord(new Vector2(UV.x, UV.y));

        Vector2 gps = new Vector2(
            value.x > .5f ? (1f - value.x) * -360f : value.x * 360f,
            value.y > .5f ? (1f - value.y) * -180f : value.y * 180f);

        CoordinatesData data = GetDataFromGPS(gps);

        return data;
    }

    public Vector3 GetNormalizedPoint(Vector2 gps, ProjectionType projection)
    {
        //print("GPS : " + gps.ToString());

        CoordinatesData data = GetDataFromGPS(gps);
        Vector3 normalizedPosition = Vector3.zero;

        switch (projection)
        {
            case ProjectionType.GPS:
                normalizedPosition = new Vector3(0f, -gps.x, gps.y);
                break;
            case ProjectionType.Mercator:
                normalizedPosition = new Vector3(
                Mathf.InverseLerp(parser.MinMaxDictionary[projection].x[0], parser.MinMaxDictionary[projection].x[1], data.X_merc),
               Mathf.InverseLerp(parser.MinMaxDictionary[projection].y[0], parser.MinMaxDictionary[projection].y[1], data.Y_merc),
               0f);
                break;
            case ProjectionType.Peters:
                normalizedPosition = new Vector3(
                Mathf.InverseLerp(parser.MinMaxDictionary[projection].x[0], parser.MinMaxDictionary[projection].x[1], data.X_peters) * PetersScale.x,
               Mathf.InverseLerp(parser.MinMaxDictionary[projection].y[0], parser.MinMaxDictionary[projection].y[1], data.Y_peters) * PetersScale.y,
               0f);
                break;
            case ProjectionType.AEQD:
                normalizedPosition = new Vector3(
                Mathf.InverseLerp(parser.MinMaxDictionary[projection].x[0], parser.MinMaxDictionary[projection].x[1], data.X_aeqd -0.5f) * aeqdScale,
               Mathf.InverseLerp(parser.MinMaxDictionary[projection].y[0], parser.MinMaxDictionary[projection].y[1], data.Y_aeqd - 0.5f) * aeqdScale,
               0f);
                break;
            default:
                break;
        }

        return normalizedPosition;
    }


    // Editor Stuff

    [ContextMenu("Create Texture")]
    private void GenerateTexture()
    {

        CoordinatesDictionary = parser.CoordinatesDictionary;

        int width = parser.MinMaxDictionary[ProjectionType.Mercator].width;
        int heigth = parser.MinMaxDictionary[ProjectionType.Mercator].heigth;

        int mercatorMin_X = parser.MinMaxDictionary[ProjectionType.Mercator].x[0];
        int mercatorMin_Y = parser.MinMaxDictionary[ProjectionType.Mercator].y[0];

        Texture2D textureMercator = new Texture2D(width, heigth, TextureFormat.ARGB32, false);

        for (int i = parser.MinMaxDictionary[ProjectionType.GPS].x[0]; i < parser.MinMaxDictionary[ProjectionType.GPS].x[1]; i++)
        {
            for (int j = parser.MinMaxDictionary[ProjectionType.GPS].y[0]; j < parser.MinMaxDictionary[ProjectionType.GPS].y[1]; j++)
            {

                CoordinatesData data = CoordinatesDictionary[new GPS(i, j)];
                float x_Merc = ((float)(data.X_merc - mercatorMin_X))/ (float)parser.MinMaxDictionary[ProjectionType.Mercator].width;
                float y_Merc = ((float)(data.Y_merc - mercatorMin_Y))/ (float)parser.MinMaxDictionary[ProjectionType.Mercator].heigth;
                textureMercator.SetPixel( i, j, new Color(x_Merc, y_Merc, 0, 1));
            }
        }

        textureMercator.Apply();

        byte[] bs = textureMercator.EncodeToPNG();
        File.WriteAllBytes("Assets/mercator.png", bs);
        AssetDatabase.Refresh();


        // AEQD

        int aeqdMin_X = parser.MinMaxDictionary[ProjectionType.AEQD].x[0];
        int aeqdMin_Y = parser.MinMaxDictionary[ProjectionType.AEQD].y[0];

        Texture2D textureAeqd = new Texture2D(width, heigth, TextureFormat.ARGB32, false);

        for (int i = parser.MinMaxDictionary[ProjectionType.GPS].x[0]; i < parser.MinMaxDictionary[ProjectionType.GPS].x[1]; i++)
        {
            for (int j = parser.MinMaxDictionary[ProjectionType.GPS].y[0]; j < parser.MinMaxDictionary[ProjectionType.GPS].y[1]; j++)
            {
                CoordinatesData data = CoordinatesDictionary[new GPS(i, j)];
                float x_Aeqd = ((float)(data.X_aeqd - aeqdMin_X)) / (float)parser.MinMaxDictionary[ProjectionType.AEQD].width;
                float y_Aeqd = ((float)(data.Y_aeqd - aeqdMin_Y)) / (float)parser.MinMaxDictionary[ProjectionType.AEQD].heigth;


                textureAeqd.SetPixel(i, j, new Color(x_Aeqd, y_Aeqd, 0, 1));

            }
        }

        textureAeqd.Apply();

        bs = textureAeqd.EncodeToPNG();
        File.WriteAllBytes("Assets/Aeqd.png", bs);
        AssetDatabase.Refresh();

        // Peters

        int petersMin_X = parser.MinMaxDictionary[ProjectionType.Peters].x[0];
        int petersMin_Y = parser.MinMaxDictionary[ProjectionType.Peters].y[0];

        Texture2D texturePeters = new Texture2D(width, heigth, TextureFormat.ARGB32, false);

        for (int i = parser.MinMaxDictionary[ProjectionType.GPS].x[0]; i < parser.MinMaxDictionary[ProjectionType.GPS].x[1]; i++)
        {
            for (int j = parser.MinMaxDictionary[ProjectionType.GPS].y[0]; j < parser.MinMaxDictionary[ProjectionType.GPS].y[1]; j++)
            {
                CoordinatesData data = CoordinatesDictionary[new GPS(i, j)];
                float x_Peters = ((float)(data.X_peters - petersMin_X)) / (float)parser.MinMaxDictionary[ProjectionType.Peters].heigth; ;
                float y_Peters = ((float)(data.Y_peters - petersMin_Y)) / (float)parser.MinMaxDictionary[ProjectionType.Peters].heigth;

                texturePeters.SetPixel(i, j, new Color(x_Peters, y_Peters, 0, 1));

            }
        }

        //texture.ReadPixels(new Rect(0, 0, 360, 180), 0, 0);
        texturePeters.Apply();

        bs = texturePeters.EncodeToPNG();
        File.WriteAllBytes("Assets/Peters.png", bs);
        AssetDatabase.Refresh();
    }
}
#endif