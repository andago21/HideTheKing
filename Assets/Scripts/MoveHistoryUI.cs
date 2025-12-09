using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class MoveHistoryUI : MonoBehaviour
{
    public GameObject historyWindow;
    public TextMeshProUGUI historyText;
    public Button toggleButton;
    public ScrollRect historyScrollRect; // Assign the ScrollRect of your Scroll View in the inspector
    private MoveNotation moveNotation;
    private bool userScrolledUp = false;

    void Awake()
    {
        moveNotation = FindObjectOfType<MoveNotation>();
        if (moveNotation == null)
        {
            Debug.LogError("MoveNotation component not found!");
            return;
        }
        if (historyWindow != null) historyWindow.SetActive(false);
    }

    void Start()
    {
        // Add click listener to the toggle button
        if (toggleButton != null)
            toggleButton.onClick.AddListener(ToggleHistoryWindow);
        // Subscribe to move events
        if (moveNotation != null)
            moveNotation.OnMoveAdded += UpdateHistoryText;
        // Setup scroll listener
        if (historyScrollRect != null)
        {
            historyScrollRect.onValueChanged.AddListener(OnScrollValueChanged);
            // Disable horizontal scrolling, enable only vertical
            historyScrollRect.horizontal = false;
            historyScrollRect.vertical = true;
        }
    }

    void OnDestroy()
    {
        if (moveNotation != null)
            moveNotation.OnMoveAdded -= UpdateHistoryText;
        if (toggleButton != null)
            toggleButton.onClick.RemoveListener(ToggleHistoryWindow);
        if (historyScrollRect != null)
            historyScrollRect.onValueChanged.RemoveListener(OnScrollValueChanged);
    }

    public void ToggleHistoryWindow()
    {
        if (historyWindow == null) return;
        historyWindow.SetActive(!historyWindow.activeSelf);
        if (historyWindow.activeSelf) UpdateHistoryText();
    }

    void UpdateHistoryText()
    {
        if (moveNotation == null || historyText == null) return;
        // Build the formatted move history
        System.Text.StringBuilder sb = new System.Text.StringBuilder();

        for (int i = 0; i < moveNotation.moveHistory.Count; i += 2)
        {
            int moveNumber = (i / 2) + 1;
            // Get white’s move (always exists)
            string whiteMove = moveNotation.moveHistory[i];
            // Get black’s move (may not exist yet)
            string blackMove = (i + 1 < moveNotation.moveHistory.Count)
                ? moveNotation.moveHistory[i + 1]
                : "";
            // Align columns (you can tweak the spacing)
            sb.AppendLine($"{moveNumber,2}. {whiteMove,-8} {blackMove}");
        }

        historyText.text = sb.ToString();
        historyText.fontSize = 10;
        historyText.enableWordWrapping = false;
        historyText.alignment = TMPro.TextAlignmentOptions.TopLeft;

        // Setup LayoutElement to allow text to expand vertically
        LayoutElement layoutElement = historyText.GetComponent<LayoutElement>();
        if (layoutElement == null)
        {
            layoutElement = historyText.gameObject.AddComponent<LayoutElement>();
        }
        
        // Force layout rebuild to get preferred height
        Canvas.ForceUpdateCanvases();
        
        // Set preferred height to content's actual height so it can scroll
        layoutElement.preferredHeight = historyText.preferredHeight;
        layoutElement.flexibleHeight = 0; // Don't stretch to fill
        
        // Position text at the very top of the content
        RectTransform textRect = historyText.GetComponent<RectTransform>();
        if (textRect != null)
        {
            // Set anchor to top-left
            textRect.anchorMin = new Vector2(0, 1);
            textRect.anchorMax = new Vector2(1, 1);
            textRect.pivot = new Vector2(0, 1);
            textRect.anchoredPosition = new Vector2(5, -20); // 5 pixels padding from left, 30 from top
        }
        
        // Remove any VerticalLayoutGroup that might override positioning
        VerticalLayoutGroup vlg = historyText.transform.parent.GetComponent<VerticalLayoutGroup>();
        if (vlg != null)
        {
            DestroyImmediate(vlg);
        }
        
        // Force layout rebuild again
        Canvas.ForceUpdateCanvases();

        // Auto-scroll to top when history window is first opened
        if (historyScrollRect != null && !userScrolledUp)
        {
            // Set to 1 to show earliest moves at the top
            historyScrollRect.verticalNormalizedPosition = 1f;
        }
    }

    void OnScrollValueChanged(Vector2 pos)
    {
        // verticalNormalizedPosition: 1 = top, 0 = bottom
        if (historyScrollRect == null) return;
        // Consider user has scrolled up if not near bottom
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
