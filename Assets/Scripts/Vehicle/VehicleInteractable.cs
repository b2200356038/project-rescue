using Game.Gameplay;
using Unity.Netcode;
using UnityEngine;

namespace Game.Vehicle
{
    public class VehicleInteractable : NetworkBehaviour, IInteractable
    {
        [SerializeField] private VehicleController vehicleController;
        [SerializeField] private VehicleSeatManager seatManager;
        
        [Header("Interaction Settings")]
        [SerializeField] private string interactionPrompt = "Press E to enter vehicle";
        [SerializeField] private float interactionRange = 3f;

        private void Awake()
        {
            if (vehicleController == null)
                vehicleController = GetComponent<VehicleController>();
                
            if (seatManager == null)
                seatManager = GetComponent<VehicleSeatManager>();
        }

        public bool CanInteract()
        {
            return seatManager.HasEmptySeats();
        }

        public void Interact()
        {
        }

        public string GetInteractionPrompt()
        {
            if (!seatManager.HasEmptySeats())
                return "Vehicle is full";
            return interactionPrompt;
        }
    }
}