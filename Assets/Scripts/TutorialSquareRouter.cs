﻿using UnityEngine;
using UnityEngine.EventSystems;

public class TutorialSquareRouter : MonoBehaviour, IPointerClickHandler
{
    public int index = -1;

    static int _lastClickFrame = -1;

    /// <summary>Call this to consume the current click so this router won't forward it.</summary>
    public static void ConsumeClick() { _lastClickFrame = Time.frameCount; }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (eventData.button != PointerEventData.InputButton.Left) return;
        Forward();
    }

    void Update()
    {
        if (!Input.GetMouseButtonDown(0)) return;
        if (Time.frameCount == _lastClickFrame) return;
        if (Camera.main == null) return;

        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        if (!Physics.Raycast(ray, out RaycastHit hit, 100f)) return;

        // Only forward when this square (or a child) is the actual raycast hit.
        for (Transform t = hit.transform; t != null; t = t.parent)
        {
            if (t == this.transform)
            {
                _lastClickFrame = Time.frameCount;
                Forward();
                return;
            }
        }
    }

    void Forward()
    {
        if (index < 0) return;
        var tm = TutorialManager.Instance;
        if (tm == null || !tm.TutorialActive) return;
        tm.OnSquareClicked(index);
    }
}
