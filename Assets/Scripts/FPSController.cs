using UnityEngine;
using Mirror;

/// <summary>
/// FPS movement and camera control during battle.
/// </summary>
public class FPSController : NetworkBehaviour
{
    [HideInInspector] public Camera fpsCamera;
    [HideInInspector] public float moveSpeed        = 5f;
    [HideInInspector] public float mouseSensitivity = 2f;

    private CharacterController _cc;
    private float _verticalRotation = 0f;
    private bool  _battleActive     = false;
    private float _verticalVelocity = 0f;
    private const float Gravity     = -9.81f;

    public void Initialize(Camera cam, float speed, float sensitivity)
    {
        fpsCamera        = cam;
        moveSpeed        = speed;
        mouseSensitivity = sensitivity;
        _cc              = GetComponent<CharacterController>();
    }

    public void SetBattleActive(bool active)
    {
        _battleActive = active;

        if (active && isLocalPlayer)
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible   = false;
        }
        else if (!active)
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible   = true;
        }
    }

    private void Update()
    {
        if (!isLocalPlayer) return;
        if (!_battleActive)  return;

        HandleMovement();
        HandleMouseLook();
    }

    private void HandleMovement()
    {
        float h    = Input.GetAxis("Horizontal");
        float v    = Input.GetAxis("Vertical");
        Vector3 move = transform.right * h + transform.forward * v;
        move        *= moveSpeed;

        if (_cc != null && _cc.isGrounded)
            _verticalVelocity = -1f;
        else
            _verticalVelocity += Gravity * Time.deltaTime;

        move.y = _verticalVelocity;
        if (_cc != null) _cc.Move(move * Time.deltaTime);
    }

    private void HandleMouseLook()
    {
        float mouseX = Input.GetAxis("Mouse X") * mouseSensitivity;
        float mouseY = Input.GetAxis("Mouse Y") * mouseSensitivity;

        transform.Rotate(0f, mouseX, 0f);

        _verticalRotation -= mouseY;
        _verticalRotation  = Mathf.Clamp(_verticalRotation, -80f, 80f);

        if (fpsCamera != null)
        {
            fpsCamera.transform.position = transform.position + Vector3.up * 1.5f;
            fpsCamera.transform.rotation = transform.rotation *
                                           Quaternion.Euler(_verticalRotation, 0f, 0f);
        }
    }

    public void PlaceAtPosition(Vector3 position, Vector3 lookAtTarget)
    {
        if (_cc != null) _cc.enabled = false;
        transform.position = position;
        if (_cc != null) _cc.enabled = true;

        Vector3 dir = lookAtTarget - position;
        dir.y = 0;
        if (dir != Vector3.zero)
            transform.rotation = Quaternion.LookRotation(dir);

        _verticalRotation = 0f;

        if (fpsCamera != null)
        {
            fpsCamera.transform.position = position + Vector3.up * 1.5f;
            fpsCamera.transform.rotation = transform.rotation;
        }
    }
}