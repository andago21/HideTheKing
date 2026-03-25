using UnityEngine;

public class FPSWeapon : MonoBehaviour
{
    [HideInInspector] public Camera fpsCamera;

    public float damage      = 20f;
    public float fireRate    = 1f;
    public float bulletRange = 50f;

    // Weapon visuals
    [HideInInspector] public WeaponType weaponType = WeaponType.Gun;
    [HideInInspector] public Transform  weaponHolder; // child of FPSBody

    // Particle systems
    public ParticleSystem muzzleFlashParticle;
    public ParticleSystem swingTrailParticle;

    private float _nextFireTime = 0f;
    private bool  _battleActive = false;

    // Animation state
    private bool  _isAnimating     = false;
    private float _animTimer       = 0f;
    private const float ANIM_DURATION = 0.25f;

    // Sword swing: rotate around Z axis
    private Quaternion _weaponStartRot;
    private Quaternion _weaponEndRot;

    // Gun kickback: move backward then return
    private Vector3 _weaponStartPos;
    private Vector3 _weaponKickbackPos;

    public void SetBattleActive(bool active)
    {
        _battleActive = active;
    }

    private void Update()
    {
        if (!_battleActive)    return;
        if (fpsCamera == null) return;

        HandleAnimation();

        if (Input.GetButton("Fire1") && Time.time >= _nextFireTime && !_isAnimating)
        {
            _nextFireTime = Time.time + (1f / fireRate);
            Attack();
        }
    }

    private void Attack()
    {
        StartWeaponAnimation();

        if (weaponType == WeaponType.Gun)
        {
            Shoot();
            if (muzzleFlashParticle != null)
                muzzleFlashParticle.Play();
        }
        else
        {
            Swing();
            if (swingTrailParticle != null)
                swingTrailParticle.Play();
        }
    }

    // ── Gun: Raycast schießen ──
    private void Shoot()
    {
        Ray ray = new Ray(fpsCamera.transform.position, fpsCamera.transform.forward);
        Debug.DrawRay(ray.origin, ray.direction * bulletRange, Color.red, 0.5f);

        int layerMask = ~LayerMask.GetMask("Ignore Raycast");
        if (Physics.Raycast(ray, out RaycastHit hit, bulletRange, layerMask))
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

    // ── Sword: Melee-Treffer prüfen ──
    private void Swing()
    {
        // Sphere cast vor der Kamera für Nahkampf
        Vector3 origin = fpsCamera.transform.position;
        float   meleeRange = 3f;

        Collider[] hits = Physics.OverlapSphere(origin + fpsCamera.transform.forward * 1.5f, 1f);
        foreach (var col in hits)
        {
            FPSHealth health = col.GetComponentInParent<FPSHealth>();
            if (health != null)
            {
                health.TakeDamageLocal(damage);
                Debug.Log($"[FPSWeapon] Sword hit {col.name} for {damage}");
                break; // nur einmal pro Swing
            }
        }
    }

    // ── Animationen ──
    private void StartWeaponAnimation()
    {
        if (weaponHolder == null) return;

        _isAnimating = true;
        _animTimer   = 0f;

        if (weaponType == WeaponType.Gun)
        {
            // Kickback: Waffe bewegt sich kurz rückwärts
            _weaponStartPos    = weaponHolder.localPosition;
            _weaponKickbackPos = _weaponStartPos + Vector3.back * 0.15f;
        }
        else
        {
            // Swing: Waffe dreht sich nach vorne
            _weaponStartRot = weaponHolder.localRotation;
            _weaponEndRot   = _weaponStartRot * Quaternion.Euler(-70f, 0f, 0f);
        }
    }

    private void HandleAnimation()
    {
        if (!_isAnimating || weaponHolder == null) return;

        _animTimer += Time.deltaTime;
        float t = _animTimer / ANIM_DURATION;

        if (weaponType == WeaponType.Gun)
        {
            // Kickback: hin und zurück
            if (t <= 0.5f)
                weaponHolder.localPosition = Vector3.Lerp(_weaponStartPos, _weaponKickbackPos, t * 2f);
            else
                weaponHolder.localPosition = Vector3.Lerp(_weaponKickbackPos, _weaponStartPos, (t - 0.5f) * 2f);
        }
        else
        {
            // Swing: vor und zurück
            if (t <= 0.5f)
                weaponHolder.localRotation = Quaternion.Lerp(_weaponStartRot, _weaponEndRot, t * 2f);
            else
                weaponHolder.localRotation = Quaternion.Lerp(_weaponEndRot, _weaponStartRot, (t - 0.5f) * 2f);
        }

        if (t >= 1f)
        {
            _isAnimating = false;
            // Reset
            if (weaponType == WeaponType.Gun)
                weaponHolder.localPosition = _weaponStartPos;
            else
                weaponHolder.localRotation = _weaponStartRot;
        }
    }
}