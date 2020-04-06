using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ModuleManager : MonoBehaviour
{
    public CoordinatesConverter converter;
    public Transform MapsParent;

    public Vector2[] Points;
    private List<Vector2> Path;

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        Vector3 targetPosition = MapsParent.localPosition;
        switch (converter.CurrentControl)
        {
            case ProjectionType.Mercator:
                targetPosition = new Vector3(0f,0f,0f);
                break;
            case ProjectionType.Peters:
                targetPosition = new Vector3(20f,0f,0f);
                break;
            case ProjectionType.AEQD:
                targetPosition = new Vector3(40f,0f,0f);
                break;
            default:
                break;
        }

        MapsParent.localPosition = Vector3.Lerp(MapsParent.localPosition, targetPosition, 0.2f);


        int click = Input.GetMouseButtonDown(0)? 1 : Input.GetMouseButtonUp(0) ? -1 : 0;


        if (click != 0)
        {
            RaycastHit hit;
            if (!Physics.Raycast(Camera.main.ScreenPointToRay(Input.mousePosition), out hit))
                return;

            Renderer rend = hit.transform.GetComponent<Renderer>();
            MeshCollider meshCollider = hit.collider as MeshCollider;

            if (rend == null || rend.sharedMaterial == null || rend.sharedMaterial.mainTexture == null || meshCollider == null)
                return;

            Texture2D tex = rend.material.mainTexture as Texture2D;
            Vector2 pixelUV = hit.textureCoord;

            print(pixelUV.ToString());

            
            if (click == 1)
            {
                if (Path == null)
                    Path = new List<Vector2>();
                else
                    Path.Clear();

                Points[0] = pixelUV;

                Path.Add(pixelUV);
                converter.TransformPoints(Path, converter.CurrentControl);
            }
            else if (click == -1)
            {
                Path.Add(pixelUV);

                Points[1] = pixelUV;
                ComputePath(Points[0], Points[1], 10,converter.CurrentControl, true);
                //converter.
                //converter.TransformPoints(Path, converter.CurrentControl);
            }
            
        }
        if (Input.GetKeyDown(KeyCode.RightArrow))
            ChangeMap(1);
        if (Input.GetKeyDown(KeyCode.LeftArrow))
            ChangeMap(-1);
        if (Input.GetKeyDown(KeyCode.Space))
            ComputePath(10);
        if (Input.GetKeyDown(KeyCode.LeftCommand))
            DisplayPath();

    }

    public void ComputePath(int steps)
    {
        ComputePath(Points[0], Points[1], steps, converter.CurrentControl,true);

    }

    public void ComputePath(Vector2 start, Vector2 end, int steps, ProjectionType projection = ProjectionType.GPS, bool UVSpace =false)
    {
        Path = new List<Vector2>();

        for (int i = 0; i <= steps; i++)
        {
            Path.Add(Vector2.Lerp(start, end, ((float)i) / (float)steps));
        }

        converter.PointList.Clear();
        converter.PointList.AddRange(Path);
        converter.TransformPoints(projection,UVSpace);

    }

    public void DisplayPath()
    {
        
        converter.UpdatePoints();
    }

    public void ChangeMap(int way)
    {
        converter.ClearPathsTrail();


        if (converter.CurrentControl == ProjectionType.Mercator && way ==-1)
            converter.CurrentControl = ProjectionType.AEQD;
        else if (converter.CurrentControl == ProjectionType.AEQD && way == 1)
            converter.CurrentControl = ProjectionType.Mercator;
        else
        {
            converter.CurrentControl = (ProjectionType)((int)converter.CurrentControl + way);
        }

    }
}
