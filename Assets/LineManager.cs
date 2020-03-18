using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine;

[System.Serializable]
public class LineCustom
{

    [SerializeField]
    private string _name;

    [SerializeField]
    public LineRenderer _lineRenderer;

    public Vector3[] Points;
    public Color LineColor;

    public void UpdatePoints(Vector3[] points)
    {
        Points = points;

        _lineRenderer.positionCount = Points.Length;
        _lineRenderer.SetPositions(Points);
    }
        
}


public class LineManager : MonoBehaviour
{
    [SerializeField]
    public LineCustom[] lines;
    public Vector3[] Points;
    public int PointCount = 5;

    public Dictionary<ProjectionType, LineCustom> LinesDictionary;
   

    
    //public 
    // Start is called before the first frame update
    void Start()
    {

        Points = new Vector3[PointCount];

        lines[0]._lineRenderer.startColor = lines[0].LineColor; 
        lines[1]._lineRenderer.startColor = lines[1].LineColor; 
        lines[2]._lineRenderer.startColor = lines[2].LineColor; 
        lines[3]._lineRenderer.startColor = lines[3].LineColor;

    }

    private void InitDictionary()
    {
        LinesDictionary = new Dictionary<ProjectionType, LineCustom>();

        LinesDictionary.Add(ProjectionType.Mercator, lines[0]);
        LinesDictionary.Add(ProjectionType.Peters, lines[1]);
        LinesDictionary.Add(ProjectionType.AEQD, lines[2]);
        LinesDictionary.Add(ProjectionType.GPS, lines[3]);

    }

    // points in gps coordinates
    public void SetLine(ProjectionType projection, Vector3[] points)
    {
        if (LinesDictionary == null)
        {
            InitDictionary();
        }
        LinesDictionary[projection].UpdatePoints(points);

    }

    public void SetEndLine(Vector2 gps)
    {
        Points[PointCount-1] = gps;

        for (int i = 0; i < PointCount-1; i++)
        {
            Points[i] = Vector2.Lerp(Points[0], Points[PointCount - 1], (float)i / ((float) PointCount-1));
        }


    }

    public void SetStartLine(Vector2 gps)
    {
        Points[0] = gps;

        for (int i = 1; i < PointCount; i++)
        {
            Points[i] = Vector2.Lerp(Points[0], Points[PointCount-1], (float)i / ((float)PointCount-1));
        }


    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
