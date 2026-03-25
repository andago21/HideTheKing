using UnityEngine;
using TMPro;

public class EloDisplay : MonoBehaviour
{
    [Header("UI")]
    public TMP_Text eloText;

    private void Start()
    {
        UpdateDisplay();
    }

    private void OnEnable()
    {
        UpdateDisplay();
    }

    public void UpdateDisplay()
    {
        if (EloManager.Instance == null) return;
        if (eloText == null)            return;

        int    elo    = EloManager.Instance.GetElo();
        string rank   = EloManager.Instance.GetRank();
        int    wins   = EloManager.Instance.GetWins();
        int    losses = EloManager.Instance.GetLosses();
        int    draws  = EloManager.Instance.GetDraws();

        eloText.text = $"Rang: {rank}\nELO: {elo}\nW: {wins}  L: {losses}  D: {draws}";
    }
}