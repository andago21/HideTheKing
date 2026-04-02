using UnityEngine;

public class FPSWeapon : MonoBehaviour
{
    [HideInInspector] public Camera    fpsCamera;
    [HideInInspector] public WeaponType weaponType = WeaponType.Gun;



    public float damage      = 20f;
    public float fireRate    = 1f;
    public float bulletRange = 50f;



    private float   _nextFireTime  = 0f;
    private bool    _battleActive  = false;

    // Weapon model — direct child of camera
    private Transform _weaponModel = null;

    // Animation
    private bool      _isAnimating   = false;
    private float     _animTimer     = 0f;
    private const float ANIM_DURATION = 0.2f;
    private Vector3   _modelBasePos;
    private Vector3   _modelKickPos;
    private Quaternion _modelBaseRot;
    private Quaternion _modelSwingRot;

    public void SetBattleActive(bool active)
    {
        _battleActive = active;
    }

    /// <summary>
    /// Call this after FPS camera is set up to attach weapon to camera.
    /// </summary>
    public void AttachWeaponToCamera(GameObject weaponPrefab)
    {
        if (fpsCamera == null || weaponPrefab == null) return;

        // Instantiate directly under camera
        GameObject obj = Instantiate(weaponPrefab, fpsCamera.transform);
        ThemeWeaponRegistry r = ThemeWeaponRegistry.Instance;
        obj.transform.localPosition = r != null ? r.ownWeaponPosition : new Vector3(0.25f, -0.2f, 0.4f);
        obj.transform.localRotation = Quaternion.Euler(r != null ? r.ownWeaponRotation : Vector3.zero);
        obj.transform.localScale    = weaponPrefab.transform.localScale;
        _weaponModel = obj.transform;

        // Save base pose for animation
        _modelBasePos  = obj.transform.localPosition;
        _modelBaseRot  = obj.transform.localRotation;
        _modelKickPos  = _modelBasePos + new Vector3(0f, 0f, -0.15f);
        _modelSwingRot = _modelBaseRot * Quaternion.Euler(70f, 0f, 0f);

        Debug.Log("[FPSWeapon] Weapon attached to camera");
    }

    private void PlayParticle(ParticleSystem[] unused)
    {
        ThemeWeaponRegistry reg = ThemeWeaponRegistry.Instance;
        if (reg == null) return;
        GameObject prefab = weaponType == WeaponType.Gun ? reg.gunParticlePrefab : reg.swordParticlePrefab;
        if (prefab != null)
        {
            GameObject ps = Instantiate(prefab, fpsCamera.transform.position, fpsCamera.transform.rotation);
            Destroy(ps, 3f);
        }
    }

    private void Update()
    {
        if (!_battleActive || fpsCamera == null) return;

        HandleAnimation();

        if (Input.GetButton("Fire1") && Time.time >= _nextFireTime && !_isAnimating)
        {
            _nextFireTime = Time.time + (1f / fireRate);
            Attack();
        }
    }

    private void Attack()
    {
        StartAnimation();

        if (weaponType == WeaponType.Gun)
        {
            Shoot();
            PlayParticle(null);
        }
        else
        {
            Swing();
            PlayParticle(null);
        }
    }

    private void Shoot()
    {
        Ray ray = new Ray(fpsCamera.transform.position, fpsCamera.transform.forward);
        if (Physics.Raycast(ray, out RaycastHit hit, bulletRange))
        {
            FPSHealth health = hit.collider.GetComponentInParent<FPSHealth>();
            if (health != null)
            {
                health.TakeDamageLocal(damage);
                Debug.Log($"[FPSWeapon] Hit {hit.collider.name} for {damage}");
            }
        }
    }

    private void Swing()
    {
        Vector3 origin = fpsCamera.transform.position;
        Collider[] hits = Physics.OverlapSphere(origin + fpsCamera.transform.forward * 0.8f, 0.4f);
        foreach (var col in hits)
        {
            FPSHealth health = col.GetComponentInParent<FPSHealth>();
            if (health != null)
            {
                health.TakeDamageLocal(damage);
                Debug.Log($"[FPSWeapon] Sword hit {col.name} for {damage}");
                break;
            }
        }
    }

    private void StartAnimation()
    {
        if (_weaponModel == null) return;
        _isAnimating = true;
        _animTimer   = 0f;
    }

    private void HandleAnimation()
    {
        if (!_isAnimating || _weaponModel == null) return;

        _animTimer += Time.deltaTime;
        float t = Mathf.Clamp01(_animTimer / ANIM_DURATION);

        if (weaponType == WeaponType.Gun)
        {
            // Kickback in camera-local space — always backward from camera
            if (t <= 0.5f)
                _weaponModel.localPosition = Vector3.Lerp(_modelBasePos, _modelKickPos, t * 2f);
            else
                _weaponModel.localPosition = Vector3.Lerp(_modelKickPos, _modelBasePos, (t - 0.5f) * 2f);
        }
        else
        {
            // Swing rotation in camera-local space
            if (t <= 0.5f)
                _weaponModel.localRotation = Quaternion.Lerp(_modelBaseRot, _modelSwingRot, t * 2f);
            else
                _weaponModel.localRotation = Quaternion.Lerp(_modelSwingRot, _modelBaseRot, (t - 0.5f) * 2f);
        }

        if (t >= 1f)
        {
            _isAnimating               = false;
            _weaponModel.localPosition = _modelBasePos;
            _weaponModel.localRotation = _modelBaseRot;
        }
    }

    public void DestroyWeaponModel()
    {
        if (_weaponModel != null)
        {
            Destroy(_weaponModel.gameObject);
            _weaponModel = null;
        }
    }
}