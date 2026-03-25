using UnityEngine;
using UnityEngine.SceneManagement;

public enum WeaponType { Sword, Gun }

/// <summary>
/// Holds FPS combat stats and weapon type for each chess piece.
/// Weapon type is determined by the active scene (theme).
/// </summary>
public class FigureStats : MonoBehaviour
{
    [Header("Movement")]
    public float moveSpeed        = 5f;
    public float mouseSensitivity = 2f;

    [Header("Combat")]
    public float maxHealth  = 100f;
    public float damage     = 20f;
    public float fireRate   = 1f;
    public float bulletRange = 50f;

    [Header("Weapon")]
    public WeaponType weaponType = WeaponType.Gun;

    // Weapon prefabs — assigned by BattleChessManager -> ThemeWeaponRegistry
    [HideInInspector] public GameObject weaponPrefabFPS;    // local FPS view
    [HideInInspector] public GameObject weaponPrefabFigure; // visible to other player

    public void ApplyDefaults(PieceType type)
    {
        switch (type)
        {
            case PieceType.Pawn:
                moveSpeed    = 3.5f; maxHealth = 60f;
                damage       = 15f;  fireRate  = 0.8f; bulletRange = 30f; break;
            case PieceType.Knight:
                moveSpeed    = 6f;   maxHealth = 80f;
                damage       = 25f;  fireRate  = 1.0f; bulletRange = 40f; break;
            case PieceType.Bishop:
                moveSpeed    = 5f;   maxHealth = 70f;
                damage       = 20f;  fireRate  = 1.5f; bulletRange = 60f; break;
            case PieceType.Rook:
                moveSpeed    = 3f;   maxHealth = 150f;
                damage       = 35f;  fireRate  = 0.5f; bulletRange = 45f; break;
            case PieceType.Queen:
                moveSpeed    = 6.5f; maxHealth = 120f;
                damage       = 30f;  fireRate  = 2.0f; bulletRange = 70f; break;
            case PieceType.King:
                moveSpeed    = 2.5f; maxHealth = 200f;
                damage       = 40f;  fireRate  = 0.4f; bulletRange = 35f; break;
        }

        // Waffentyp basierend auf aktiver Szene
        weaponType = GetWeaponTypeForScene(SceneManager.GetActiveScene().name);
    }

    public static WeaponType GetWeaponTypeForScene(string sceneName)
    {
        // Sword Themes
        if (sceneName.Contains("MedivalBattle") ||
            sceneName.Contains("TimeTravel")    ||
            sceneName.Contains("SpaceOdyseey"))
            return WeaponType.Sword;

        // Gun Themes (WildWest, Pirates, CyberPunk)
        return WeaponType.Gun;
    }
}