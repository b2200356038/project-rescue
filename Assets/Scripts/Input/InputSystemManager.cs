using UnityEngine;
namespace Game.Input
{
    public class InputSystemManager : MonoBehaviour
    {
        public static InputSystemManager Instance { get; private set; }
        
        private GameActions.UIActions _uiInputs;
        private GameActions.PlayerActions _gameplayInputs;
        private GameActions.VehicleActions _vehicleActions;

        private void Awake()
        {
            if (Instance!= null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);
            Cursor.visible = true;
            Cursor.lockState = CursorLockMode.None;
        }

        private void Start()
        {
            _uiInputs = GameInput.Actions.UI;
            _gameplayInputs = GameInput.Actions.Player;
            _vehicleActions = GameInput.Actions.Vehicle;
        }

        public void EnableUIInputs()
        {
            _vehicleActions.Disable();
            _gameplayInputs.Disable();
            _uiInputs.Enable();
        }
        public void EnableOnFootInputs()
        {
            _vehicleActions.Disable();
            _gameplayInputs.Enable();
            _uiInputs.Disable();
        }

        public void EnableVehicleInputs()
        {
            _vehicleActions.Enable();
            _gameplayInputs.Disable();
            _uiInputs.Disable();
        }
    }
    
}

