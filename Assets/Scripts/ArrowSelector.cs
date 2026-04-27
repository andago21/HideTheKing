using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public class ArrowSelector : MonoBehaviour
{
    public RectTransform arrow;
    public List<RectTransform> buttons;

    private Canvas _canvas;

    void Start()
    {
        _canvas = GetComponentInParent<Canvas>();

        foreach (var button in buttons)
        {
            var trigger = button.GetComponent<EventTrigger>() ?? button.gameObject.AddComponent<EventTrigger>();
            var bt = button;

            var entry = new EventTrigger.Entry { eventID = EventTriggerType.PointerEnter };
            entry.callback.AddListener(_ => SnapToButton(bt));
            trigger.triggers.Add(entry);
        }
    }

    void SnapToButton(RectTransform button)
    {
        if (arrow == null || _canvas == null) return;

        Camera cam = _canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : Camera.main;

        Vector2 screenPoint = RectTransformUtility.WorldToScreenPoint(cam, button.position);

        if (RectTransformUtility.ScreenPointToLocalPointInRectangle(
                arrow.parent as RectTransform,
                screenPoint,
                cam,
                out Vector2 localPoint))
        {
            arrow.localPosition = new Vector3(arrow.localPosition.x, localPoint.y, arrow.localPosition.z);
        }
    }
}