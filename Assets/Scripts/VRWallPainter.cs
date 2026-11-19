using UnityEngine;

public class VRWallPainter : Unity.Netcode.NetworkBehaviour
{
    [Header("VR references")]
    [SerializeField] private UnityEngine.XR.Interaction.Toolkit.Interactors.NearFarInteractor _rightInteractor;
    [SerializeField] private UnityEngine.XR.Interaction.Toolkit.Interactors.XRRayInteractor _teleportInteractor;
    [SerializeField] private UnityEngine.InputSystem.InputActionReference _paintAction;
    [SerializeField] private GameObject _paletteRoot;
    [SerializeField] private TMPro.TMP_Text _targetStatus;

    [Header("Validation")]
    [SerializeField, Tooltip("Room-scale tolerance from the server-known rig origin. Existing tracked transforms use server authority, so remote head poses cannot be treated as authoritative.")]
    private float _trackingOriginTolerance = 12f;
    [SerializeField, Tooltip("Minimum time between accepted paint requests.")]
    private float _minimumPaintInterval = 0.1f;

    private Color _selectedColor = Color.white;
    private int _pendingFrame = -1;
    private double _nextAllowedServerPaint;
    private bool _subscribed;
    private PaintableWall _targetWall;
    private int _targetFace;
    private Vector3 _rayOrigin;
    private Vector3 _rayDirection;
    private bool _lastValidTarget;
    private UnityEngine.InputSystem.InputAction _paletteToggleAction;

    public Color SelectedColor => _selectedColor;
    public bool HasPaintTarget => _targetWall != null;

    private void Awake()
    {
        if (_paletteRoot != null) _paletteRoot.SetActive(false);
        _paletteToggleAction = new UnityEngine.InputSystem.InputAction("Toggle Wrist Palette",
            UnityEngine.InputSystem.InputActionType.Button, "<XRController>{LeftHand}/primaryButton");
    }

    public override void OnNetworkSpawn()
    {
        if (_paletteRoot != null) _paletteRoot.SetActive(false);
        bool localVR = IsOwner && OwnerClientId != Unity.Netcode.NetworkManager.ServerClientId;
        if (localVR) Subscribe();
    }

    private void OnEnable()
    {
        if (IsSpawned && IsOwner && OwnerClientId != Unity.Netcode.NetworkManager.ServerClientId) Subscribe();
    }

    private void Subscribe()
    {
        if (_subscribed || _paintAction == null) return;
        // The existing InputActionManager owns enable/disable of this shared XRI action.
        _paintAction.action.performed += OnPaintPressed;
        _paletteToggleAction.performed += OnPaletteTogglePressed;
        _paletteToggleAction.Enable();
        _subscribed = true;
    }

    private void Unsubscribe()
    {
        if (_subscribed)
        {
            if (_paintAction != null) _paintAction.action.performed -= OnPaintPressed;
            _paletteToggleAction.performed -= OnPaletteTogglePressed;
        }
        _paletteToggleAction?.Disable();
        _subscribed = false;
        _pendingFrame = -1;
    }

    private void OnPaletteTogglePressed(UnityEngine.InputSystem.InputAction.CallbackContext context)
    {
        TogglePalette();
    }

    public void TogglePalette()
    {
        if (!IsSpawned || !IsOwner || OwnerClientId == Unity.Netcode.NetworkManager.ServerClientId || _paletteRoot == null) return;
        _pendingFrame = -1;
        _paletteRoot.SetActive(!_paletteRoot.activeSelf);
    }

    private void OnDisable()
    {
        Unsubscribe();
        if (_paletteRoot != null) _paletteRoot.SetActive(false);
    }

    public override void OnNetworkDespawn()
    {
        Unsubscribe();
        if (_paletteRoot != null) _paletteRoot.SetActive(false);
    }

    public override void OnDestroy()
    {
        Unsubscribe();
        _paletteToggleAction?.Dispose();
        base.OnDestroy();
    }

    public void SetSelectedColor(Color color)
    {
        color.a = 1f;
        if (NetworkWallPaintState.IsValidColor(color)) _selectedColor = color;
    }

    private void OnPaintPressed(UnityEngine.InputSystem.InputAction.CallbackContext context)
    {
        if (IsSpawned && IsOwner) _pendingFrame = Time.frameCount;
    }

    private bool IsBlocked()
    {
        if (_rightInteractor == null || !_rightInteractor.isActiveAndEnabled || _rightInteractor.hasSelection) return true;
        if (_teleportInteractor != null && _teleportInteractor.isActiveAndEnabled) return true;
        return _rightInteractor.TryGetCurrentUIRaycastResult(out var uiHit) && uiHit.gameObject != null;
    }

    private void LateUpdate()
    {
        if (!IsSpawned || !IsOwner || OwnerClientId == Unity.Netcode.NetworkManager.ServerClientId) return;
        _targetWall = null;
        var state = NetworkWallPaintState.Instance;
        if (!IsBlocked() && state != null && state.IsSpawned)
        {
            var origin = _rightInteractor.curveOrigin != null ? _rightInteractor.curveOrigin : _rightInteractor.transform;
            _rayOrigin = origin.position;
            _rayDirection = origin.forward;
            if (Physics.Raycast(_rayOrigin, _rayDirection, out RaycastHit hit, state.MaxPaintDistance, ~0, QueryTriggerInteraction.Ignore))
            {
                _targetWall = hit.collider.GetComponent<PaintableWall>();
                if (_targetWall != null) _targetFace = _targetWall.FaceFromWorldNormal(hit.normal);
            }
        }
        bool valid = _targetWall != null;
        if (_targetStatus != null && valid != _lastValidTarget)
        {
            _targetStatus.text = valid ? "TRIGGER TO PAINT" : "AIM AT A WALL";
            _targetStatus.color = valid ? new Color(0.4f, 1f, 0.7f) : Color.white;
        }
        _lastValidTarget = valid;
        // A press consumed by UI/grabbing/teleport must stay cancelled even if the pointer moves away next frame.
        if (_pendingFrame == Time.frameCount && IsBlocked()) _pendingFrame = -1;
        // Defer to the next frame so UI raycast and press dispatch always finish first.
        if (_pendingFrame < 0 || Time.frameCount <= _pendingFrame) return;
        _pendingFrame = -1;
        if (valid) PaintFaceRpc(_targetWall.WallId, _targetFace, _selectedColor, _rayOrigin, _rayDirection);
    }

    [Unity.Netcode.Rpc(Unity.Netcode.SendTo.Server, RequireOwnership = true)]
    private void PaintFaceRpc(int wallId, int face, Color color, Vector3 origin, Vector3 direction, Unity.Netcode.RpcParams rpcParams = default)
    {
        if (rpcParams.Receive.SenderClientId != OwnerClientId || OwnerClientId == Unity.Netcode.NetworkManager.ServerClientId || !NetworkObject.IsPlayerObject) return;
        if (!NetworkWallPaintState.IsFinite(origin) || Vector3.Distance(origin, transform.position) > _trackingOriginTolerance) return;
        double now = Time.unscaledTimeAsDouble;
        if (now < _nextAllowedServerPaint) return;
        _nextAllowedServerPaint = now + _minimumPaintInterval;
        var state = NetworkWallPaintState.Instance;
        if (state != null) state.TryApplyPaint(wallId, face, color, origin, direction);
    }
}
