using Unity.Netcode;
using UnityEngine;

public class PlayerLocalSetup : NetworkBehaviour
{
    [Tooltip("The camera attached to this player prefab")]
    public Camera playerCamera;

    [Tooltip("AudioListeners and Movement scripts that should only run for the local owner")]
    public Behaviour[] componentsToDisable;

    public override void OnNetworkSpawn()
    {
        if (IsOwner)
        {
            // This is YOUR player. Turn off the scene's default camera so you can see through your prefab's eyes.
            GameObject defaultCamera = GameObject.Find("Main Camera");
            if (defaultCamera != null)
            {
                defaultCamera.SetActive(false);
            }
        }
        else
        {
            // If you do not own this avatar, turn off its camera so it doesn't override yours
            if (playerCamera != null)
            {
                playerCamera.gameObject.SetActive(false);
            }

            // Turn off movement scripts or audio listeners so you don't hear/control them locally
            foreach (var component in componentsToDisable)
            {
                if (component != null)
                {
                    component.enabled = false;
                }
            }
        }
    }
}