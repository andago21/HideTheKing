using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class MoveHistoryUI : MonoBehaviour
{
    public GameObject historyWindow;
    public TextMeshProUGUI historyText;
    public Button toggleButton;
    private MoveNotation moveNotation;

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
    }

    void OnDestroy()
    {
        if (moveNotation != null)
            moveNotation.OnMoveAdded -= UpdateHistoryText;
        if (toggleButton != null)
            toggleButton.onClick.RemoveListener(ToggleHistoryWindow);
    }

    public void ToggleHistoryWindow()
    {
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
        historyText.fontSize = 24;
        historyText.enableWordWrapping = false;
        historyText.alignment = TMPro.TextAlignmentOptions.TopLeft;
    }


    void OnEnable()
    {
        if (moveNotation != null) moveNotation.OnMoveAdded += UpdateHistoryText;
    }

    void OnDisable()
    {
        if (moveNotation != null) moveNotation.OnMoveAdded -= UpdateHistoryText;
    }
}
