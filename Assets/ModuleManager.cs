using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ModuleManager : MonoBehaviour
{
    public CoordinatesConverter converter;
    public Transform MapsParent;
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
    }

    public void ChangeMap(int way)
    {
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
