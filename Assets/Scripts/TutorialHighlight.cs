using UnityEngine;

// TutorialHighlight triggers tutorial rook movement when clicked. Uses explicit raycast for reliability.
public class TutorialHighlight : MonoBehaviour
{
    public int index = -1;
    public TutorialManager manager;

    void Update()
    {
        if (Input.GetMouseButtonDown(0))
        {
            if (Camera.main == null) return;
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            if (Physics.Raycast(ray, out RaycastHit hit, 100f))
            {
                // Check if the hit belongs to this highlight (or its children)
                Transform t = hit.transform;
                while (t != null)
                {
                    if (t == this.transform)
                    {
                        Debug.Log($"TutorialHighlight clicked (ray): index={index}, manager={(manager!=null?"present":"null")}");
                        if (manager != null && index >= 0) manager.MoveTutorialRookToIndex(index);
                        return;
                    }
                    t = t.parent;
                }
            }
        }
    }
}
