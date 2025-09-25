using Unity.Netcode;
using UnityEngine;

public class PlayerTagTrigger : NetworkBehaviour
{


    // --- Network senkronize edilen tag ---
    

    private NetworkVariable<PlayerTag> ownerTag = new NetworkVariable<PlayerTag>(
        PlayerTag.None,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server);

    private void Start()
    {
        // Clientlerde otomatik senkronize olacak
        ownerTag.OnValueChanged += OnOwnerTagChanged;
    }

    private void OnDestroy()
    {
        ownerTag.OnValueChanged -= OnOwnerTagChanged;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!IsServer) return; // sadece server karar verecek

        // Burada örnek: collider’ın tag değerinden PlayerTag seç
        PlayerTag newTag = PlayerTag.None;

        if (other.CompareTag("Player1"))
            newTag = PlayerTag.Player1;
        else if (other.CompareTag("Player2"))
            newTag = PlayerTag.Player2;

        // Server network variable’a yazıyor → tüm clientlerde tetiklenir
        ownerTag.Value = newTag;

        // Collider kapat (server authority)
        other.gameObject.GetComponent<Collider>().enabled = false;
    }

    private void OnOwnerTagChanged(PlayerTag oldTag, PlayerTag newTag)
    {
        // Tüm clientlerde tetiklenir
        string unityTag = newTag.ToString(); // "Player1" veya "Player2"
        gameObject.tag = unityTag;

        foreach (Transform child in transform)
        {
            child.gameObject.tag = unityTag;
        }

        Debug.Log($"[{(IsServer ? "Server" : "Client")}] {gameObject.name} {unityTag} tagine geçti.");
    }
}
