using UnityEngine;

// TutorialHighlight triggers tutorial rook movement when clicked. Uses explicit raycast for reliability.
public class TutorialHighlight : MonoBehaviour
{
    public int index = -1;
    public TutorialManager manager;

    void Update()
    {
        if (!Input.GetMouseButtonDown(0)) return;
        if (Camera.main == null) return;

        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        if (!Physics.Raycast(ray, out RaycastHit hit, 100f)) return;

        for (Transform t = hit.transform; t != null; t = t.parent)
        {
            if (t == this.transform)
            {
                var tm = manager ?? TutorialManager.Instance;
                if (tm != null && index >= 0)
                    tm.OnSquareClicked(index);
                return;
            }
        }
    }
}
