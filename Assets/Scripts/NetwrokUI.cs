using Unity.Netcode;
using UnityEngine;

public class NetworkUI : MonoBehaviour
{
    void OnGUI()
    {
        // Safeguard against null reference if NetworkManager hasn't initialized
        if (NetworkManager.Singleton == null) return;

        // Only show the buttons if the network hasn't started yet
        if (!NetworkManager.Singleton.IsClient && !NetworkManager.Singleton.IsServer)
        {
            GUILayout.BeginArea(new Rect(10, 10, 300, 300));

            if (GUILayout.Button("Start Host (PC)", GUILayout.Width(200), GUILayout.Height(50)))
                NetworkManager.Singleton.StartHost();

            GUILayout.Space(10);

            if (GUILayout.Button("Start Client (VR)", GUILayout.Width(200), GUILayout.Height(50)))
                NetworkManager.Singleton.StartClient();

            GUILayout.EndArea();
        }
    }
}