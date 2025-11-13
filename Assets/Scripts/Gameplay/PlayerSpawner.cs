using Unity.Netcode;
using UnityEngine;

namespace Gameplay
{
    class PlayerSpawner : NetworkBehaviour
    {
        [SerializeField]
        NetworkObject playerPrefab;

        protected override void OnNetworkSessionSynchronized()
        {
            Debug.Assert(playerPrefab != null, $"Prefab reference '{nameof(playerPrefab)}' is missing or not assigned.");

            if (playerPrefab != null)
            {
                var spawnPoint = PlayerSpawnPoints.Instance.GetRandomSpawnPoint();
                playerPrefab.InstantiateAndSpawn(networkManager: NetworkManager, ownerClientId: NetworkManager.LocalClientId, isPlayerObject: true, position: spawnPoint.position, rotation: spawnPoint.rotation);
            }

            base.OnNetworkSessionSynchronized();
        }
    }
}
