using Game.Input;
using Game.Physics;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Game.Player
{
    public class PlayerOnFootStateNotOwner : PlayerStateBase
    {
        internal override void Initialize(PlayerStateMachine playerStateMachine)
        {
            base.Initialize(playerStateMachine);
        }

        public override void OnEnter()
        {
           
        }

        public override void OnNetworkUpdate()
        {
            
        }
        
        void OnJumped(InputAction.CallbackContext _)
        {
            
        }

        public override void OnNetworkFixedUpdate()
        {
            
        }

        public override void OnExit()
        {
            base.OnExit();
        }
    }
}
