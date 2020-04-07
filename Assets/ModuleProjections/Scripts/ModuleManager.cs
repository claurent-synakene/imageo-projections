using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ModuleManager : MonoBehaviour
{
    public CoordinatesConverter converter;
    public Transform MapsParent;

    // Car la map AEQD est pas anglée parfaitement avec la meridienne 0 vers le bas
    public Transform AEQDVector;

    public List<Vector2> Path;

    void Start()
    {
        Path = new List<Vector2>();
    }

    void Update()
    {
        // If just clicked, unclicked or holding click
        int click = Input.GetMouseButtonDown(0)? 1 : Input.GetMouseButtonUp(0) ? -1 : 0;

        if (click != 0 || Input.GetMouseButton(0))
        {
            ProjectionType type = converter.CurrentControl;

            // Raycast sur la map et recup des coordonnées UV
            RaycastHit hit;
            if (!Physics.Raycast(Camera.main.ScreenPointToRay(Input.mousePosition), out hit))
                return;

            Renderer rend = hit.transform.GetComponent<Renderer>();
            MeshCollider meshCollider = hit.collider as MeshCollider;

            if (rend == null || rend.sharedMaterial == null || rend.sharedMaterial.mainTexture == null || meshCollider == null)
                return;
            Vector2 pixelUV = hit.textureCoord;

            // If aeqd, use different system, directly compute gps position
            if (type == ProjectionType.AEQD)
            {
                Vector2 delta = (new Vector2(0.5f, 0.5f)-pixelUV);
                float angle = Vector2.SignedAngle(new Vector2(AEQDVector.localPosition.x, AEQDVector.localPosition.z), delta);
                float AEQD_Y = JSonParser.INSTANCE.AEQDYCurve.Evaluate((delta.magnitude * 2f));
                pixelUV = new Vector2(angle, AEQD_Y);
            }

            if (click == 1)
            {
                if (Path == null)
                    Path = new List<Vector2>();
                else
                    Path.Clear();

                Path.Add(pixelUV);
            }
            else if (click == -1)
            {
                Path.Add(pixelUV);
                FinishPath(converter.CurrentControl, true);
            }
            else
            {
                float delta = (Path[Path.Count - 1] - pixelUV).magnitude;
                if (delta > (type == ProjectionType.AEQD?5f: 0.05f))
                    Path.Add(pixelUV);
            }
            
        }

        // Map Swap

        if (Input.GetKeyDown(KeyCode.RightArrow))
            ChangeMap(1);
        if (Input.GetKeyDown(KeyCode.LeftArrow))
            ChangeMap(-1);

        Vector3 targetPosition = MapsParent.localPosition;
        switch (converter.CurrentControl)
        {
            case ProjectionType.Mercator:
                targetPosition = new Vector3(0f, 0f, 0f);
                break;
            case ProjectionType.Peters:
                targetPosition = new Vector3(20f, 0f, 0f);
                break;
            case ProjectionType.AEQD:
                targetPosition = new Vector3(40f, 0f, 0f);
                break;
            default:
                break;
        }

        MapsParent.localPosition = Vector3.Lerp(MapsParent.localPosition, targetPosition, 0.2f);
        // Show paths

        if (Input.GetKeyDown(KeyCode.Space))
            ComputePath(10);
        if (Input.GetKeyDown(KeyCode.LeftCommand))
            DisplayPath();

    }

    public void ComputePath(int steps)
    {
        //ComputePath(Points[0], Points[1], steps, converter.CurrentControl,true);

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

    private void FinishPath(ProjectionType projection = ProjectionType.GPS, bool UVSpace = false)
    {
        converter.PointList.Clear();
        converter.PointList.AddRange(Path);
        converter.TransformPoints(projection, UVSpace);
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
            converter.CurrentControl = (ProjectionType)((int)converter.CurrentControl + way);
    }
}
