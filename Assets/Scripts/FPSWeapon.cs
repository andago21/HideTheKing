using UnityEngine;
using Mirror;

/// <summary>
/// Handles shooting for a player in FPS mode.
/// Input is read client-side, but the actual raycast and damage run server-side.
/// </summary>
public class FPSWeapon : NetworkBehaviour
{
    [Header("References")]
    public Camera fpsCamera;            // The FPS camera for this player
    public GameObject muzzleFlashPrefab; // Optional visual effect, spawned client-side
    public Transform muzzlePoint;        // Where the bullet comes from visually

    // Stats come from FigureStats
    private float damage;
    private float fireRate;
    private float bulletRange;

    private float _nextFireTime = 0f;
    private bool  _battleActive = false;

    public void Initialize(FigureStats stats, Camera cam)
    {
        damage      = stats.damage;
        fireRate    = stats.fireRate;
        bulletRange = stats.bulletRange;
        fpsCamera   = cam;
    }

    public void SetBattleActive(bool active)
    {
        _battleActive = active;
    }

    private void Update()
    {
        // Only the local player reads input
        if (!isLocalPlayer) return;
        if (!_battleActive)  return;

        if (Input.GetButton("Fire1") && Time.time >= _nextFireTime)
        {
            _nextFireTime = Time.time + (1f / fireRate);
            TryShoot();
        }
    }

    private void TryShoot()
    {
        // Play muzzle flash locally for responsiveness
        if (muzzleFlashPrefab != null && muzzlePoint != null)
            Instantiate(muzzleFlashPrefab, muzzlePoint.position, muzzlePoint.rotation);

        // Send shoot command to server with ray direction
        Vector3 origin    = fpsCamera.transform.position;
        Vector3 direction = fpsCamera.transform.forward;
        CmdShoot(origin, direction);
    }

    /// <summary>
    /// Runs on server — authoritative raycast and damage application.
    /// </summary>
    [Command]
    private void CmdShoot(Vector3 origin, Vector3 direction)
    {
        Ray ray = new Ray(origin, direction);

        Debug.DrawRay(origin, direction * bulletRange, Color.red, 1f);

        if (Physics.Raycast(ray, out RaycastHit hit, bulletRange))
        {
            FPSHealth health = hit.collider.GetComponentInParent<FPSHealth>();
            if (health != null)
            {
                health.TakeDamage(damage);
                Debug.Log($"[FPSWeapon] Hit {hit.collider.name} for {damage} damage");

                // Tell all clients to show hit effect at impact point
                RpcShowHitEffect(hit.point, hit.normal);
            }
        }
    }

    [ClientRpc]
    private void RpcShowHitEffect(Vector3 point, Vector3 normal)
    {
        // TODO: spawn hit particle effect here when you have one
        Debug.Log($"[FPSWeapon] Hit effect at {point}");
    }
}