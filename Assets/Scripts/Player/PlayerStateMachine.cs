using System;
using System.Collections.Generic;
using Game.Vehicle;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Game.Player
{
    public class PlayerStateMachine : NetworkBehaviour, INetworkUpdateSystem
    {
        [Header("References")]
        [SerializeField] internal Rigidbody rb;
        [SerializeField] internal PlayerController playerController; 
        [SerializeField] internal PlayerInput playerInput;
        [SerializeField] internal Collider playerCollider; 

        [Header("State")]
        [SerializeField] private NetworkVariable<PlayerState> currentState = new(
            PlayerState.None,
            NetworkVariableReadPermission.Everyone,
            NetworkVariableWritePermission.Owner);

        private Dictionary<PlayerState, PlayerStateBase> _states;
        private PlayerStateBase _activeState;
        private VehicleController _currentVehicle;

        public PlayerState CurrentState => currentState.Value;
        public event Action<PlayerState, PlayerState> OnStateChanged;


        public override void OnNetworkSpawn()
        {
            base.OnNetworkSpawn();
            BuildStates();
            currentState.OnValueChanged += OnStateChangedCallback;
            this.RegisterNetworkUpdate(NetworkUpdateStage.Update);
            this.RegisterNetworkUpdate(NetworkUpdateStage.FixedUpdate);
            if (IsOwner)
            {
                ChangeState(PlayerState.OnFoot);
            }
            else
            {
                if (_states.TryGetValue(currentState.Value, out var st))
                {
                    _activeState = st;
                    _activeState.OnEnter();
                }
            }
        }


        public override void OnNetworkDespawn()
        {
            base.OnNetworkDespawn();
            
            currentState.OnValueChanged -= OnStateChangedCallback;
            this.UnregisterAllNetworkUpdates();
        }
        private void BuildStates()
        {
            _states = new Dictionary<PlayerState, PlayerStateBase>();

            if (IsOwner)
            {
                _states[PlayerState.OnFoot] = new PlayerOnFootStateOwner();
            }
            else
            {
                _states[PlayerState.OnFoot] = new PlayerOnFootStateNotOwner();
            }
            
            foreach (var st in _states.Values)
                st.Initialize(this);
        }
        
        public void ChangeState(PlayerState newState)
        {
            if (!IsOwner) return;
            if (currentState.Value == newState) return;

            currentState.Value = newState;
        }
        
        private void OnStateChangedCallback(PlayerState prev, PlayerState next)
        { 
            _activeState?.OnExit();
            if (_states.TryGetValue(next, out var st))
            {
                _activeState = st;
                _activeState.OnEnter();
            }
            OnStateChanged?.Invoke(prev, next);
        }
        
        public void NetworkUpdate(NetworkUpdateStage stage)
        {
            if (_activeState == null) return;

            switch (stage)
            {
                case NetworkUpdateStage.Update:
                    _activeState.OnNetworkUpdate();
                    break;

                case NetworkUpdateStage.FixedUpdate:
                    _activeState.OnNetworkFixedUpdate();
                    break;
            }
        }
    }
}
