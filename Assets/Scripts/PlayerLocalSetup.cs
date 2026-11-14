using Unity.Netcode;
using UnityEngine;

public class PlayerLocalSetup : NetworkBehaviour
{
    [Tooltip("The camera attached to this player prefab")]
    public Camera playerCamera;

    [Tooltip("AudioListeners and Movement scripts that should only run for the local owner")]
    public Behaviour[] componentsToDisable;

    [SerializeField, Tooltip("Controller roots to show on remote avatars after local XR input is disabled.")]
    private GameObject[] _remoteControllerRoots = new GameObject[0];

    public override void OnNetworkSpawn()
    {
        if (IsOwner)
        {
            // Only disable the scene fallback camera, never a camera belonging to a player.
            foreach (Camera sceneCamera in Camera.allCameras)
            {
                if (sceneCamera != playerCamera && sceneCamera.name == "Main Camera" &&
                    sceneCamera.GetComponentInParent<PlayerLocalSetup>() == null)
                {
                    sceneCamera.gameObject.SetActive(false);
                }
            }
        }
        else
        {
            // If you do not own this avatar, turn off its camera so it doesn't override yours
            if (playerCamera != null)
            {
                playerCamera.enabled = false;
            }

            // Turn off movement scripts or audio listeners so you don't hear/control them locally
            foreach (var component in componentsToDisable)
            {
                if (component != null)
                {
                    component.enabled = false;
                }
            }

            foreach (var controllerRoot in _remoteControllerRoots)
            {
                if (controllerRoot != null)
                {
                    controllerRoot.SetActive(true);
                }
            }
        }
    }
}