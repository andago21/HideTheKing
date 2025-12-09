using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class MoveHistoryUI : MonoBehaviour
{
    public GameObject historyWindow;
    public TextMeshProUGUI historyText;
    public Button toggleButton;
    public ScrollRect historyScrollRect;
    
    private MoveNotation moveNotation;
    private bool userScrolledUp = false;

    void Start()
    {
        moveNotation = FindObjectOfType<MoveNotation>();
        if (moveNotation == null)
        {
            Debug.LogError("MoveNotation component not found!");
            return;
        }

        historyWindow?.SetActive(false);
        historyScrollRect?.gameObject.SetActive(false);

        toggleButton?.onClick.AddListener(ToggleHistoryWindow);
        moveNotation.OnMoveAdded += UpdateHistoryText;
        
        if (historyScrollRect != null)
        {
            historyScrollRect.horizontal = false;
            historyScrollRect.vertical = true;
            historyScrollRect.onValueChanged.AddListener(OnScrollValueChanged);
        }
    }

    void OnDestroy()
    {
        if (moveNotation != null) moveNotation.OnMoveAdded -= UpdateHistoryText;
        if (toggleButton != null) toggleButton.onClick.RemoveListener(ToggleHistoryWindow);
        if (historyScrollRect != null) historyScrollRect.onValueChanged.RemoveListener(OnScrollValueChanged);
    }

    public void ToggleHistoryWindow()
    {
        if (historyWindow == null) return;
        
        bool isActive = !historyWindow.activeSelf;
        historyWindow.SetActive(isActive);
        historyScrollRect?.gameObject.SetActive(isActive);
        if (isActive) UpdateHistoryText();
    }

    void UpdateHistoryText()
    {
        if (moveNotation == null || historyText == null) return;
        
        historyText.text = FormatMoveHistory();
        historyText.fontSize = 10;
        
        LayoutElement layout = historyText.GetComponent<LayoutElement>();
        if (layout == null) layout = historyText.gameObject.AddComponent<LayoutElement>();
        
        Canvas.ForceUpdateCanvases();
        layout.preferredHeight = historyText.preferredHeight;
        
        if (historyScrollRect != null && !userScrolledUp)
            historyScrollRect.verticalNormalizedPosition = 1f;
    }
    
    string FormatMoveHistory()
    {
        System.Text.StringBuilder sb = new System.Text.StringBuilder();
        for (int i = 0; i < moveNotation.moveHistory.Count; i += 2)
        {
            int moveNumber = (i / 2) + 1;
            string whiteMove = moveNotation.moveHistory[i];
            string blackMove = (i + 1 < moveNotation.moveHistory.Count) 
                ? moveNotation.moveHistory[i + 1] 
                : "";
            sb.AppendLine($"{moveNumber,2}. {whiteMove,-8} {blackMove}");
        }
        return sb.ToString();
    }

    void OnScrollValueChanged(Vector2 pos)
    {
        if (historyScrollRect == null) return;
        userScrolledUp = historyScrollRect.verticalNormalizedPosition > 0.01f;
    }

    void OnEnable()
    {
        if (moveNotation != null) moveNotation.OnMoveAdded += UpdateHistoryText;
        if (historyScrollRect != null) historyScrollRect.onValueChanged.AddListener(OnScrollValueChanged);
    }

    void OnDisable()
    {
        if (moveNotation != null) moveNotation.OnMoveAdded -= UpdateHistoryText;
        if (historyScrollRect != null) historyScrollRect.onValueChanged.RemoveListener(OnScrollValueChanged);
    }
}

