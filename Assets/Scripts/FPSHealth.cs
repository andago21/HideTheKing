using UnityEngine;
using Mirror;

/// <summary>
/// HP-Verwaltung fuer eine Figur im FPS-Kampf.
/// Schaden wird lokal registriert und ueber BattleChessManager server-seitig verarbeitet.
/// </summary>
public class FPSHealth : NetworkBehaviour
{
    [SyncVar(hook = nameof(OnHealthChanged))]
    public float currentHealth;

    private float maxHealth;
    private bool  isDead = false;

    [HideInInspector] public Piece ownerPiece;

    public void Initialize(float max)
    {
        maxHealth     = max;
        currentHealth = max;
        isDead        = false;
    }

    /// <summary>
    /// Wird lokal vom FPSWeapon aufgerufen.
    /// Schickt Schaden ueber BattleChessManager zum Server.
    /// </summary>
    public void TakeDamageLocal(float amount)
    {
        if (isDead) return;

        // Ueber BattleChessManager server-seitig verarbeiten
        BattleChessManager.Instance?.CmdApplyDamage(ownerPiece.position.x, ownerPiece.position.y, amount);
    }

    /// <summary>
    /// Wird vom Server aufgerufen um HP zu reduzieren.
    /// </summary>
    [Server]
    public void ApplyDamage(float amount)
    {
        if (isDead) return;

        currentHealth -= amount;
        currentHealth  = Mathf.Max(currentHealth, 0f);

        Debug.Log($"[FPSHealth] {ownerPiece?.type} took {amount} damage. HP: {currentHealth}/{maxHealth}");

        if (currentHealth <= 0f)
        {
            isDead = true;
            BattleChessManager.Instance?.OnFigureDied(ownerPiece);
        }
    }

    private void OnHealthChanged(float oldVal, float newVal)
    {
        Debug.Log($"[FPSHealth] HP updated: {newVal}/{maxHealth}");
        // TODO: Health Bar UI hier updaten
    }

    public float GetHealthPercent()
    {
        if (maxHealth <= 0) return 0;
        return currentHealth / maxHealth;
    }
}