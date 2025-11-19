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

        private void Awake()
        {
            _states = new Dictionary<PlayerState, PlayerStateBase>
            {
                [PlayerState.OnFoot] = new PlayerOnFootState(),
                [PlayerState.Vehicle] = new PlayerVehicleState()
            };

            foreach (var state in _states.Values)
            {
                state.Initialize(this);
            }
        }

        public override void OnNetworkSpawn()
        {
            base.OnNetworkSpawn();
            
            currentState.OnValueChanged += OnStateChangedCallback;

            if (IsOwner)
            {
                ChangeState(PlayerState.OnFoot);
                this.RegisterNetworkUpdate(updateStage: NetworkUpdateStage.Update);
                this.RegisterNetworkUpdate(updateStage: NetworkUpdateStage.FixedUpdate);
            }
        }

        public override void OnNetworkDespawn()
        {
            currentState.OnValueChanged -= OnStateChangedCallback;
            this.UnregisterAllNetworkUpdates();
            base.OnNetworkDespawn();
        }
        
        public void ChangeState(PlayerState newState)
        {
            if (!IsOwner) return;
            if (currentState.Value == newState) return;
            
            currentState.Value = newState;
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
        
        public void EnterVehicle(VehicleController vehicleController)
        {
            if (!HasAuthority) return;
            _currentVehicle = vehicleController;
            if (_states[PlayerState.Vehicle] is PlayerVehicleState vehicleState)
            {
                vehicleState.SetVehicle(vehicleController);
            }
            rb.isKinematic = true;
            rb.detectCollisions = false;
            if (playerCollider) playerCollider.enabled = false;
            ChangeState(PlayerState.Vehicle);
        }

        public void ExitVehicle()
        {
            if (!HasAuthority) return;

            if (_currentVehicle != null)
            {
                var seatManager = _currentVehicle.GetComponent<VehicleSeatManager>();
                seatManager.RequestExitVehicle(OwnerClientId);
                Transform exitPoint = seatManager.GetExitPoint();
                transform.position = exitPoint.position;
                transform.rotation = exitPoint.rotation;

                _currentVehicle = null;
            }

            rb.isKinematic = false;
            rb.detectCollisions = true;
            if (playerCollider) playerCollider.enabled = true;

            ChangeState(PlayerState.OnFoot);
        }
        public void NetworkUpdate(NetworkUpdateStage updateStage)
        {
            switch (updateStage)
            {
                case NetworkUpdateStage.Update:
                    _activeState?.OnNetworkUpdate();
                    SyncPositionWithSeat();
                    break;
                case NetworkUpdateStage.FixedUpdate:
                    _activeState?.OnNetworkFixedUpdate();
                    break;
            }
        }

        private void SyncPositionWithSeat()
        {
            if (IsOwner && currentState.Value == PlayerState.Vehicle && _currentVehicle != null)
            {
                var seatManager = _currentVehicle.GetComponent<VehicleSeatManager>();
                Transform seatTransform = seatManager.GetSeatTransform(OwnerClientId);
                
                if (seatTransform != null)
                {
                    transform.position = seatTransform.position;
                    transform.rotation = seatTransform.rotation;
                }
            }
        }

        public VehicleController GetCurrentVehicle() => _currentVehicle;
    }
}