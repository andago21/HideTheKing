using UnityEngine;

[RequireComponent(typeof(CharacterController))]
public class FPSController : MonoBehaviour
{
    [HideInInspector] public Camera fpsCamera;
    [HideInInspector] public float  moveSpeed        = 5f;
    [HideInInspector] public float  mouseSensitivity = 2f;

    // Callback: wird jeden Frame mit der neuen Position aufgerufen
    // BattleChessManager setzt dies um die Position per RPC zu senden
    public System.Action<Vector3> onPositionChanged;

    private CharacterController _cc;
    private float _verticalRotation   = 0f;
    private float _horizontalRotation = 0f;
    private bool  _battleActive       = false;
    private float _startY;

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

        if (active)
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible   = false;
        }
        else
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible   = true;
        }
    }

    private void Update()
    {
        if (!_battleActive)    return;
        if (fpsCamera == null) return;

        Vector3 oldPos = transform.position;
        HandleMovement();
        HandleMouseLook();

        // Wenn Position geaendert, Callback aufrufen fuer Netzwerk-Sync
        if (transform.position != oldPos && onPositionChanged != null)
            onPositionChanged(transform.position);
    }

    private void HandleMovement()
    {
        if (_cc == null) return;

        float h = Input.GetAxis("Horizontal");
        float v = Input.GetAxis("Vertical");

        Vector3 camForward = fpsCamera.transform.forward;
        Vector3 camRight   = fpsCamera.transform.right;
        camForward.y = 0f;
        camRight.y   = 0f;
        camForward.Normalize();
        camRight.Normalize();

        Vector3 move = (camForward * v + camRight * h) * moveSpeed * Time.deltaTime;

        _cc.enabled = false;
        Vector3 newPos = transform.position + move;
        newPos.y = _startY;
        transform.position = newPos;
        _cc.enabled = true;
    }

    private void HandleMouseLook()
    {
        float mouseX = Input.GetAxis("Mouse X") * mouseSensitivity;
        float mouseY = Input.GetAxis("Mouse Y") * mouseSensitivity;

        _horizontalRotation += mouseX;
        _verticalRotation   -= mouseY;
        _verticalRotation    = Mathf.Clamp(_verticalRotation, -80f, 80f);

        fpsCamera.transform.position = transform.position + Vector3.up * 1.5f;
        fpsCamera.transform.rotation = Quaternion.Euler(_verticalRotation, _horizontalRotation, 0f);
    }

    public void PlaceAtPosition(Vector3 position, Vector3 lookAtTarget)
    {
        _startY = position.y;

        if (_cc == null) _cc = GetComponent<CharacterController>();
        _cc.enabled        = false;
        transform.position = position;
        _cc.enabled        = true;

        Vector3 dir = lookAtTarget - position;
        dir.y = 0;
        if (dir != Vector3.zero)
        {
            transform.rotation  = Quaternion.LookRotation(dir);
            _horizontalRotation = Quaternion.LookRotation(dir).eulerAngles.y;
        }

        _verticalRotation = 0f;

        if (fpsCamera != null)
        {
            fpsCamera.transform.position = position + Vector3.up * 1.5f;
            fpsCamera.transform.rotation = Quaternion.Euler(0f, _horizontalRotation, 0f);
        }
    }

    // Setzt die Figur-Position von aussen (empfangen vom anderen Client)
    public void SetRemoteFigurePosition(Vector3 pos, Transform figure)
    {
        if (figure != null)
            figure.position = pos;
    }
}