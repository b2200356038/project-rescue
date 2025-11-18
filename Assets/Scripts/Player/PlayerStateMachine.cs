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
        [SerializeField] internal Rigidbody rb;
        [SerializeField] internal PlayerController playerController;
        [SerializeField] internal PlayerInput playerInput;

        private NetworkVariable<PlayerState> _currentState = new(
            PlayerState.None,
            NetworkVariableReadPermission.Everyone,
            NetworkVariableWritePermission.Owner);

        private Dictionary<PlayerState, PlayerStateBase> _states;
        private PlayerStateBase _activeState;

        private VehicleController _currentVehicle;

        public PlayerState CurrentState => _currentState.Value;
        public event Action<PlayerState, PlayerState> OnStateChanged;

        private void Awake()
        {
            _states = new Dictionary<PlayerState, PlayerStateBase>
            {
                [PlayerState.OnFoot] = new PlayerOnFootState(),
                [PlayerState.Driving] = new PlayerDrivingState()
            };
            foreach (var state in _states.Values)
            {
                state.Initialize(this);
            }
        }

        public override void OnNetworkSpawn()
        {
            base.OnNetworkSpawn();
            _currentState.OnValueChanged += OnStateChangedCallback;
            if (HasAuthority)
            {
                ChangeState(PlayerState.OnFoot);
                this.RegisterNetworkUpdate(updateStage: NetworkUpdateStage.Update);
                this.RegisterNetworkUpdate(updateStage: NetworkUpdateStage.FixedUpdate);
            }
        }

        public void ChangeState(PlayerState newState)
        {
            if (!HasAuthority)
                return;
            if (_currentState.Value == newState)
                return;
            _currentState.Value = newState;
        }

        public void EnterVehicle(VehicleController vehicleController, bool isDriver)
        {
            if (!HasAuthority)
                return;

            _currentVehicle = vehicleController;

            if (isDriver)
            {
                if (_states[PlayerState.Driving] is PlayerDrivingState drivingState)
                {
                    drivingState.SetVehicle(vehicleController);
                }
                ChangeState(PlayerState.Driving);
            }
            else
            {
                // TODO: Passenger state
            }
        }
        
        public void ExitVehicle()
        {
            if (!HasAuthority)
                return;

            _currentVehicle = null;
            ChangeState(PlayerState.OnFoot);
        }

        private void OnStateChangedCallback(PlayerState previousState, PlayerState newState)
        {
            if (_activeState != null)
            {
                _activeState.OnExit();
            }

            if (_states.TryGetValue(newState, out var state))
            {
                _activeState = state;
                if (HasAuthority) _activeState.OnEnter();
            }

            OnStateChanged?.Invoke(previousState, newState);
        }

        public void NetworkUpdate(NetworkUpdateStage updateStage)
        {
            switch (updateStage)
            {
                case NetworkUpdateStage.Update:
                    _activeState?.OnNetworkUpdate();
                    break;
                case NetworkUpdateStage.FixedUpdate:
                    _activeState?.OnNetworkFixedUpdate();
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(updateStage), updateStage, null);
            }
        }

        public override void OnNetworkDespawn()
        {
            _currentState.OnValueChanged -= OnStateChangedCallback;
            this.UnregisterAllNetworkUpdates();
            base.OnNetworkDespawn();
        }

        public VehicleController GetCurrentVehicle()
        {
            return _currentVehicle;
        }
    }
}