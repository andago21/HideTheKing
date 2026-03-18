using UnityEngine;
using Mirror;

/// <summary>
/// Attached to each piece during battle.
/// Tracks HP server-side and notifies BattleChessManager on death.
/// </summary>
public class FPSHealth : NetworkBehaviour
{
    [SyncVar(hook = nameof(OnHealthChanged))]
    public float currentHealth;

    private float maxHealth;
    private bool isDead = false;

    // Which piece this health belongs to
    [HideInInspector] public Piece ownerPiece;

    public void Initialize(float max)
    {
        maxHealth     = max;
        currentHealth = max;
        isDead        = false;
    }

    /// <summary>
    /// Called server-side only when this figure takes damage.
    /// </summary>
    [Server]
    public void TakeDamage(float amount)
    {
        if (isDead) return;

        currentHealth -= amount;
        currentHealth  = Mathf.Max(currentHealth, 0f);

        Debug.Log($"[FPSHealth] {ownerPiece?.type} took {amount} damage. HP: {currentHealth}/{maxHealth}");

        if (currentHealth <= 0f)
        {
            isDead = true;
            // Tell BattleChessManager this figure died
            BattleChessManager.Instance?.OnFigureDied(ownerPiece);
        }
    }

    // Fires on all clients when HP changes — use this to update health bar UI later
    private void OnHealthChanged(float oldVal, float newVal)
    {
        Debug.Log($"[FPSHealth] HP updated: {newVal}/{maxHealth}");
        // TODO: update health bar UI here when ready
    }

    public float GetHealthPercent()
    {
        if (maxHealth <= 0) return 0;
        return currentHealth / maxHealth;
    }
}