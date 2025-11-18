using System;
using Unity.Netcode;
using UnityEngine;

namespace Game.Gameplay
{
    public interface IOwnershipRequestable
    {
        event Action<NetworkBehaviour, NetworkObject.OwnershipRequestResponseStatus> OnNetworkObjectOwnershipRequestResponse;

    }
}
