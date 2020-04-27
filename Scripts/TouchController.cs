using UnityEngine;

[ExecuteInEditMode]
class TouchController : MonoBehaviour
{
    private bool bMouseDownBegin = false;
    private Vector3 vecLastPos;
    void Update()
    {
        if (Input.GetMouseButton(0))
        {
            if (!bMouseDownBegin)
            {
                vecLastPos = Input.mousePosition;
                GridFluidSimulation.resetPointer(Input.mousePosition.x, Input.mousePosition.y);
                bMouseDownBegin = true;
            }
            else
            {
                if (Vector3.Magnitude(Input.mousePosition - vecLastPos) > 0 )
                    GridFluidSimulation.UpdatePointer(Input.mousePosition.x, Input.mousePosition.y);
            }
            
        }
        else if(Input.GetMouseButtonUp(0))
        {
            bMouseDownBegin = false;
        }
    }
}

