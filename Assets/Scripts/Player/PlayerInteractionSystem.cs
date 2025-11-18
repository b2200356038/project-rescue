using Game.Gameplay;
using Game.Input;
using Game.Vehicle;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Game.Player
{

    public class PlayerInteractionSystem : NetworkBehaviour
    {
        [Header("Settings")]
        [SerializeField] private float interactionRange = 3f;
        [SerializeField] private LayerMask interactionLayer = ~0;
        [SerializeField] private float raycastRadius = 0.5f;

        [Header("References")]
        [SerializeField] private Transform raycastOrigin;
        
        private IInteractable _currentInteractable;
        private Collider _currentCollider;
        private PlayerStateMachine _stateMachine;

        private void Awake()
        {
            _stateMachine = GetComponent<PlayerStateMachine>();
            
            if (raycastOrigin == null)
                raycastOrigin = transform;
        }

        public override void OnNetworkSpawn()
        {
            base.OnNetworkSpawn();
            
            if (!HasAuthority)
            {
                enabled = false;
                return;
            }

            GameInput.Actions.Player.Interact.performed += OnInteractPressed;
        }

        private void OnInteractPressed(InputAction.CallbackContext obj)
        {
            throw new System.NotImplementedException();
        }

        public override void OnNetworkDespawn()
        {
            base.OnNetworkDespawn();
            GameInput.Actions.Player.Interact.performed -= OnInteractPressed;
        }
 
    }
}