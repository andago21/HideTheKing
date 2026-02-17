using UnityEngine;
using UnityEngine.EventSystems;

public class ArrowSelector : MonoBehaviour, IPointerEnterHandler
{
    public RectTransform arrow;
    public float targetY;

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (arrow != null)
        {
            Vector3 pos = arrow.localPosition;
            pos.y = targetY;
            arrow.localPosition = pos;
        }
    }
}