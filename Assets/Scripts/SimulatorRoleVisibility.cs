using UnityEngine;

public class SimulatorRoleVisibility : MonoBehaviour
{
    private Canvas[] _canvases;

    private void Awake()
    {
        _canvases = GetComponentsInChildren<Canvas>(true);
        SetVisible(false);
    }

    private void LateUpdate()
    {
        var manager = Unity.Netcode.NetworkManager.Singleton;
        bool isVrClient = manager != null && manager.IsConnectedClient && !manager.IsServer;
        SetVisible(isVrClient);
    }

    private void SetVisible(bool visible)
    {
        foreach (var canvas in _canvases)
        {
            if (canvas != null && canvas.enabled != visible)
            {
                canvas.enabled = visible;
            }
        }
    }
}
