using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Assign the weapon prefab and transform settings in the Inspector.
/// </summary>
public class ThemeWeaponRegistry : MonoBehaviour
{
    public static ThemeWeaponRegistry Instance;

    [Header("Weapon Prefab")]
    public GameObject weaponPrefabFPS;
    public GameObject weaponPrefabFigure => weaponPrefabFPS;

    [Header("Weapon Type")]
    public WeaponType weaponType;

    [Header("Own Weapon (Camera View)")]
    public Vector3 ownWeaponPosition = new Vector3(0.25f, -0.2f, 0.4f);
    public Vector3 ownWeaponRotation = new Vector3(0f, 0f, 0f);

    [Header("Enemy Weapon (On Figure)")]
    public Vector3 enemyWeaponPosition = new Vector3(0f, 0f, 0.01f);
    public Vector3 enemyWeaponRotation = new Vector3(0f, 90f, 0f);
    public float   enemyWeaponScale    = 0.0015f;

    private void Awake()
    {
        Instance   = this;
        weaponType = FigureStats.GetWeaponTypeForScene(SceneManager.GetActiveScene().name);
    }
}