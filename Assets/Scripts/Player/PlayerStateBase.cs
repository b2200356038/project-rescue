using Game.Physics;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Game.Player

{
    public enum PlayerState
    {
        None,
        OnFoot,
        Driving,
        Passenger,
        Observer  
    }
    public abstract class PlayerStateBase
    {
        internal PlayerStateMachine StateMachine;
        public virtual void OnEnter() { }
        public virtual void OnExit() { }
        public virtual void OnUpdate() { }
        public virtual void OnFixedUpdate() { }
        public virtual void OnNetworkUpdate() { }
        public virtual void OnNetworkFixedUpdate() { }
        internal void Initialize(PlayerStateMachine stateMachine)
        {
            StateMachine = stateMachine;
        }
        
    }
}