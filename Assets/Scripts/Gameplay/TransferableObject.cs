using System;
using Unity.Netcode;
using Game.GameManagement;

namespace Game.Gameplay
{
    public class TransferableObject :  NetworkBehaviour, IOwnershipRequestable
    {
        public event Action<NetworkBehaviour, NetworkObject.OwnershipRequestResponseStatus> OnNetworkObjectOwnershipRequestResponse;
        public override void OnNetworkSpawn()
        {
            if (HasAuthority)
            {
                NetworkObject.SetOwnershipStatus(NetworkObject.OwnershipStatus.Distributable, clearAndSet: true);
                NetworkObject.SetOwnershipStatus(NetworkObject.OwnershipStatus.Transferable);
            }
            base.OnNetworkSpawn();

            NetworkObject.OnOwnershipRequested += OnOwnershipRequested;
            NetworkObject.OnOwnershipRequestResponse += OnOwnershipRequestResponse;
        }
        public override void OnNetworkDespawn()
        {
            base.OnNetworkDespawn();
            if (NetworkObject)
            {
                NetworkObject.OnOwnershipRequested -= OnOwnershipRequested;
                NetworkObject.OnOwnershipRequestResponse -= OnOwnershipRequestResponse;
            }
            GameplayEventHandler.NetworkObjectDespawned(NetworkObject);
            OnNetworkObjectOwnershipRequestResponse = null;
        }

        protected override void OnOwnershipChanged(ulong previous, ulong current)
        {
            base.OnOwnershipChanged(previous, current);
            GameplayEventHandler.NetworkObjectOwnershipChanged(NetworkObject, previous, current);
        }
        
        protected virtual bool OnOwnershipRequested(ulong clientRequesting)
        {
            return true;
        }

        void OnOwnershipRequestResponse(NetworkObject.OwnershipRequestResponseStatus ownershipRequestResponse)
        {
            OnNetworkObjectOwnershipRequestResponse?.Invoke(this, ownershipRequestResponse);
        }
        
    }
}
