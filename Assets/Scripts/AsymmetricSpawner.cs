using Unity.Netcode;
using UnityEngine;

public class AsymmetricSpawner : MonoBehaviour
{
    public GameObject vrPlayerPrefab;
    public GameObject pcPlayerPrefab;

    private void Start()
    {
        // Hook into the connection approval process before the network starts
        NetworkManager.Singleton.ConnectionApprovalCallback += ApprovalCheck;
    }

    private void ApprovalCheck(NetworkManager.ConnectionApprovalRequest request, NetworkManager.ConnectionApprovalResponse response)
    {
        // 1. Approve the connection and tell Netcode to create a player object
        response.Approved = true;
        response.CreatePlayerObject = true;

        // 2. ClientNetworkId 0 is strictly the Server/Host in NGO.
        bool isHostPC = request.ClientNetworkId == NetworkManager.ServerClientId;
        GameObject prefabToUse = isHostPC ? pcPlayerPrefab : vrPlayerPrefab;

        // 3. Pass the prefab's exact hash to NGO so it handles the spawning
        if (prefabToUse != null)
        {
            response.PlayerPrefabHash = prefabToUse.GetComponent<NetworkObject>().PrefabIdHash;
        }

        // 4. Use the PC prefab's configured hovering position; preserve the VR spawn.
        response.Position = isHostPC && pcPlayerPrefab != null
            ? pcPlayerPrefab.transform.position
            : new Vector3(2f, 0f, 0f);
        response.Rotation = Quaternion.identity;
    }
}