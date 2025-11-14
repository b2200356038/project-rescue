
using Unity.Netcode;

namespace Game.Physics
{
    internal interface ICollisionHandler
    {
        void SendCollisionMessage(CollisionMessageInfo collisionMessage);
        
        void HandleCollisionRpc(CollisionMessageInfo collisionMessage, RpcParams rpcParams = default);
    }
}
