using Unity.Netcode;
using UnityEngine;

public class NetworkUI : MonoBehaviour
{
    private Texture2D backgroundTexture;

    void Awake()
    {
        backgroundTexture = new Texture2D(1, 1);
        backgroundTexture.SetPixel(0, 0, new Color(0f, 0.3f, 0.8f, 1f));
        backgroundTexture.Apply();
    }

    void OnGUI()
    {
        if (NetworkManager.Singleton == null) return;

        if (!NetworkManager.Singleton.IsClient && !NetworkManager.Singleton.IsServer)
        {
            // Cover the entire screen
            GUI.DrawTexture(new Rect(0, 0, Screen.width, Screen.height), backgroundTexture);

            // Center the button container on the screen. Increased height to 160 to fit everything.
            float boxWidth = 200;
            float boxHeight = 160;
            Rect areaRect = new Rect((Screen.width - boxWidth) / 2, (Screen.height - boxHeight) / 2, boxWidth, boxHeight);

            GUILayout.BeginArea(areaRect);

            GUILayout.Label("Choose", GUI.skin.label);
            GUILayout.Space(15);

            if (GUILayout.Button("Computer", GUILayout.Width(200), GUILayout.Height(50)))
                NetworkManager.Singleton.StartHost();

            GUILayout.Space(10);

            if (GUILayout.Button("Virtual Reality", GUILayout.Width(200), GUILayout.Height(50)))
                NetworkManager.Singleton.StartClient();

            GUILayout.EndArea();
        }
    }

    void OnDestroy()
    {
        if (backgroundTexture != null)
        {
            Destroy(backgroundTexture);
        }
    }
}