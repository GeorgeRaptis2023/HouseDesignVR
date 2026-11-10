using Unity.Netcode;
using UnityEngine;

public class VRHandSync : NetworkBehaviour
{
    public Transform leftController;
    public Transform rightController;

    // Variables owned by the client so they can freely update their hand positions over the network
    private NetworkVariable<Vector3> leftPos = new NetworkVariable<Vector3>(default, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);
    private NetworkVariable<Quaternion> leftRot = new NetworkVariable<Quaternion>(default, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);

    private NetworkVariable<Vector3> rightPos = new NetworkVariable<Vector3>(default, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);
    private NetworkVariable<Quaternion> rightRot = new NetworkVariable<Quaternion>(default, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);

    void Update()
    {
        if (leftController == null || rightController == null) return;

        if (IsOwner)
        {
            // Local VR player constantly writes their physical hand positions to the network
            leftPos.Value = leftController.localPosition;
            leftRot.Value = leftController.localRotation;

            rightPos.Value = rightController.localPosition;
            rightRot.Value = rightController.localRotation;
        }
        else
        {
            // The PC Host reads the network data and moves the VR avatar's hands to match
            leftController.localPosition = leftPos.Value;
            leftController.localRotation = leftRot.Value;

            rightController.localPosition = rightPos.Value;
            rightController.localRotation = rightRot.Value;
        }
    }
}