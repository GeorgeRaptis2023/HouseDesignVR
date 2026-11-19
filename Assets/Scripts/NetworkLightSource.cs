using UnityEngine;

[RequireComponent(typeof(Light), typeof(Unity.Netcode.NetworkObject))]
public class NetworkLightSource : Unity.Netcode.NetworkBehaviour
{
    [SerializeField, Tooltip("Whether the light is on when the session starts.")]
    private bool _startsOn = true;

    [SerializeField, Tooltip("Keyboard key used by the PC host to toggle this light.")]
    private UnityEngine.InputSystem.Key _toggleKey = UnityEngine.InputSystem.Key.L;

    private readonly Unity.Netcode.NetworkVariable<bool> _isOn =
        new Unity.Netcode.NetworkVariable<bool>(true,
            Unity.Netcode.NetworkVariableReadPermission.Everyone,
            Unity.Netcode.NetworkVariableWritePermission.Server);

    private Light _light;
    private UnityEngine.InputSystem.InputAction _toggleAction;

    public bool IsOn => _light != null && _light.enabled;

    private void Awake()
    {
        _light = GetComponent<Light>();
        _light.enabled = _startsOn;
        _toggleAction = new UnityEngine.InputSystem.InputAction("Toggle Light",
            UnityEngine.InputSystem.InputActionType.Button, "<Keyboard>/" + _toggleKey.ToString());
        _toggleAction.performed += OnTogglePerformed;
    }

    public override void OnNetworkSpawn()
    {
        _isOn.OnValueChanged += OnLightStateChanged;
        if (IsServer)
        {
            _isOn.Value = _startsOn;
        }
        _light.enabled = _isOn.Value;

        // AsymmetricSpawner assigns the PC player to the host, and VR to clients.
        if (IsHost && isActiveAndEnabled)
        {
            _toggleAction.Enable();
        }
    }

    private void OnEnable()
    {
        if (IsSpawned && IsHost && _toggleAction != null)
        {
            _toggleAction.Enable();
        }
    }

    private void OnDisable()
    {
        _toggleAction?.Disable();
    }

    public override void OnNetworkDespawn()
    {
        _isOn.OnValueChanged -= OnLightStateChanged;
        _toggleAction.Disable();
    }

    public override void OnDestroy()
    {
        if (_toggleAction != null)
        {
            _toggleAction.performed -= OnTogglePerformed;
            _toggleAction.Dispose();
        }
        base.OnDestroy();
    }

    private void OnTogglePerformed(UnityEngine.InputSystem.InputAction.CallbackContext context)
    {
        if (IsHost && NetworkManager.LocalClient != null &&
            NetworkManager.LocalClient.PlayerObject != null)
        {
            Toggle();
        }
    }

    private void OnLightStateChanged(bool previousValue, bool newValue)
    {
        _light.enabled = newValue;
    }

    public void Activate()
    {
        SetLightState(true);
    }

    public void Deactivate()
    {
        SetLightState(false);
    }

    public void Toggle()
    {
        if (IsSpawned && IsServer)
        {
            SetLightState(!_isOn.Value);
        }
    }

    private void SetLightState(bool isOn)
    {
        if (IsSpawned && IsServer)
        {
            _isOn.Value = isOn;
        }
    }
}
