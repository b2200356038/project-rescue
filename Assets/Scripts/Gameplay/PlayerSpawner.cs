using Unity.Netcode;
using UnityEngine;

namespace Gameplay
{
    public class PlayerSpawner : NetworkBehaviour
    {
        [SerializeField]
        private NetworkObject playerPrefab;
        protected override void OnNetworkSessionSynchronized()
        {
            Debug.Log("2er");

            if (playerPrefab!=null)
            {
                Debug.Log("er");
                var spawnPoint = PlayerSpawnPoints.Instance.GetRandomSpawnPoint();
                playerPrefab.InstantiateAndSpawn(networkManager: NetworkManager, ownerClientId: NetworkManager.LocalClientId, isPlayerObject: true, position: spawnPoint.position, rotation: spawnPoint.rotation);
            }
            base.InternalOnNetworkSessionSynchronized();
        }
    }
}
