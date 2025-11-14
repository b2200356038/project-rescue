using GameManagement;
using Unity.Netcode;
using UnityEngine;

namespace Services
{
    public class Vivox3DPositioning : NetworkBehaviour
    {
        private bool _initialized;
        private float _nextPosUpdate;

        public override void OnNetworkSpawn()
        {
            base.OnNetworkSpawn();
            if (!HasAuthority)
            {
                enabled = false;
                return;
            }

            GameplayEventHandler.OnChatIsReady += OnChatIsReady;
            GameplayEventHandler.OnExitedSession += OnExitSession;
        }
        
        void OnChatIsReady(bool chatIsReady, string channelName)
        {
            _initialized = chatIsReady;
        }

        void OnExitSession()
        {
            _initialized = false;
        }
        
        void Update()
        {
            if (!_initialized)
            {
                return;
            }

            if (Time.time > _nextPosUpdate)
            {
                VivoxManager.Instance.SetPlayer3DPosition(gameObject);
                _nextPosUpdate = Time.time + 0.3f;
            }
        }
        
        public override void OnNetworkDespawn()
        {
            base.OnNetworkDespawn();
            GameplayEventHandler.OnChatIsReady -= OnChatIsReady;
            GameplayEventHandler.OnExitedSession -= OnExitSession;
        }
    }
}
