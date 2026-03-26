using UnityEngine;
using UnityEngine.SceneManagement;

public class ThemeWeaponRegistry : MonoBehaviour
{
    public static ThemeWeaponRegistry Instance;

    [Header("Weapon Prefab (used for FPS view)")]
    public GameObject weaponPrefabFPS;

    public GameObject weaponPrefabFigure => weaponPrefabFPS;

    [Header("Weapon Type (auto-detected from scene name)")]
    public WeaponType weaponType;

    private void Awake()
    {
        Instance   = this;
        weaponType = FigureStats.GetWeaponTypeForScene(SceneManager.GetActiveScene().name);
    }
}