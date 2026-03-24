using UnityEngine;

public class FPSWeapon : MonoBehaviour
{
    [HideInInspector] public Camera fpsCamera;

    public float damage      = 20f;
    public float fireRate    = 1f;
    public float bulletRange = 50f;

    public GameObject muzzleFlashPrefab;
    public Transform  muzzlePoint;

    private float _nextFireTime = 0f;
    private bool  _battleActive = false;

    // Layer Mask — nur Figuren treffen, nicht das Brett
    // "Piece" Layer muss in Unity vorhanden sein und den Figuren zugewiesen werden
    // Falls kein Piece-Layer existiert, wird alles getroffen (Fallback)
    private int _pieceLayerMask = 0;

    public void SetBattleActive(bool active)
    {
        _battleActive = active;

        // Layer Mask beim Aktivieren setzen
        int pieceLayer = LayerMask.NameToLayer("Piece");
        if (pieceLayer != -1)
            _pieceLayerMask = LayerMask.GetMask("Piece");
        else
        {
            // Kein Piece-Layer — alles ausser dem FPSBody treffen
            _pieceLayerMask = ~LayerMask.GetMask("Ignore Raycast");
            Debug.LogWarning("[FPSWeapon] Kein 'Piece' Layer gefunden. Erstelle ihn in Unity und weise ihn den Figur-Prefabs zu.");
        }
    }

    private void Update()
    {
        if (!_battleActive)    return;
        if (fpsCamera == null) return;

        if (Input.GetButton("Fire1") && Time.time >= _nextFireTime)
        {
            _nextFireTime = Time.time + (1f / fireRate);
            Shoot();
        }
    }

    private void Shoot()
    {
        if (muzzleFlashPrefab != null && muzzlePoint != null)
            Instantiate(muzzleFlashPrefab, muzzlePoint.position, muzzlePoint.rotation);

        Ray ray = new Ray(fpsCamera.transform.position, fpsCamera.transform.forward);
        Debug.DrawRay(ray.origin, ray.direction * bulletRange, Color.red, 0.5f);

        // Mit Layer Mask schiessen
        if (Physics.Raycast(ray, out RaycastHit hit, bulletRange, _pieceLayerMask))
        {
            Debug.Log($"[FPSWeapon] Raycast hit: {hit.collider.name}");

            FPSHealth health = hit.collider.GetComponentInParent<FPSHealth>();
            if (health != null)
            {
                health.TakeDamageLocal(damage);
                Debug.Log($"[FPSWeapon] Hit piece for {damage} damage");
            }
            else
            {
                Debug.Log($"[FPSWeapon] Hit {hit.collider.name} aber keine FPSHealth gefunden");
            }
        }
    }
}