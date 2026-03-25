using UnityEngine;
using UnityEngine.SceneManagement;
 

public class ThemeWeaponRegistry : MonoBehaviour
{
    public static ThemeWeaponRegistry Instance;
 
    [Header("FPS Weapon Prefab (lokale Ansicht)")]
    public GameObject weaponPrefabFPS;
 
    [Header("Figure Weapon Prefab (sichtbar fuer anderen Spieler)")]
    public GameObject weaponPrefabFigure;
 
    [Header("Weapon Type")]
    public WeaponType weaponType;
 
    private void Awake()
    {
        Instance = this;
        // Auto-detect weapon type from scene name
        weaponType = FigureStats.GetWeaponTypeForScene(SceneManager.GetActiveScene().name);
    }
}
 