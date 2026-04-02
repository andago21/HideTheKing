using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;
using System.Collections;
using HideTheKing.Core;
using Mirror;

/// <summary>
/// Shows the hidden king at game start for 5 seconds.
/// HideTheKing: shows own hidden king.
/// CrownOfConfusion: shows opponent's hidden king.
/// Place this script on the RevealManager Canvas in HideTheKing and CrownOfConfusion scenes.
/// </summary>
public class RevealManager : MonoBehaviour
{
    [Header("UI")]
    public GameObject panel;
    public TMP_Text   revealText;
    public TMP_Text   subRevealText;

    [Header("Settings")]
    public float displayDuration = 5f;

    public static bool IsRevealing { get; private set; } = false;

    private void Start()
    {
        if (panel != null) panel.SetActive(false);
        IsRevealing = false;
        StartCoroutine(WaitForBothPlayersAndReveal());
    }

    private IEnumerator WaitForBothPlayersAndReveal()
    {
        // Wait until multiplayer is active and local player is known
        float timeout = 15f;
        float elapsed = 0f;

        while (elapsed < timeout)
        {
            bool networkReady = ChessNetworkManager.LocalInstance != null;
            bool htkReady     = HideTheKingManager.Instance != null;

            if (networkReady && htkReady)
            {
                bool localIsWhite  = ChessNetworkManager.LocalInstance.isWhitePlayer;
                bool isCrown       = SceneManager.GetActiveScene().name.Contains("CrownOfConfussion");
                bool targetIsWhite = isCrown ? !localIsWhite : localIsWhite;

                var state = HideTheKingManager.Instance.GetHiddenState(targetIsWhite);
                if (state != null && state.HiddenTarget != null)
                    break;
            }

            elapsed += Time.deltaTime;
            yield return null;
        }

        ShowReveal();
        yield return new WaitForSeconds(displayDuration);
        HideReveal();
    }

    private void ShowReveal()
    {
        if (HideTheKingManager.Instance == null) return;
        if (ChessNetworkManager.LocalInstance == null) return;

        bool localIsWhite  = ChessNetworkManager.LocalInstance.isWhitePlayer;
        bool isCrown       = SceneManager.GetActiveScene().name.Contains("CrownOfConfussion");
        bool targetIsWhite = isCrown ? !localIsWhite : localIsWhite;

        var state = HideTheKingManager.Instance.GetHiddenState(targetIsWhite);
        if (state == null || state.HiddenTarget == null) return;

        Piece   hidden    = state.HiddenTarget;
        string  pieceName = hidden.type.ToString();

        // Add side specification from local player's perspective
        string sideInfo = "";
        PieceSide side  = HiddenTargetLogicGeneric.GetSide(hidden);
        if (side != PieceSide.None)
        {
            // For Black player, left/right is mirrored compared to White's perspective
            bool isBlackPlayer = !ChessNetworkManager.LocalInstance.isWhitePlayer;
            bool isLeft = (side == PieceSide.Left);
            if (isBlackPlayer) isLeft = !isLeft; // Mirror for black
            sideInfo = isLeft ? " (Left)" : " (Right)";
        }

        if (subRevealText != null)
        {
            subRevealText.text = pieceName + sideInfo;
        }

        if (panel != null) panel.SetActive(true);
        IsRevealing = true;

        Debug.Log($"[RevealManager] Showing king: {pieceName}{sideInfo}");
    }

    private void HideReveal()
    {
        if (panel != null) panel.SetActive(false);
        IsRevealing = false;
        Debug.Log("[RevealManager] Reveal hidden — game starts");
    }

    private void OnDestroy()
    {
        IsRevealing = false;
    }
}