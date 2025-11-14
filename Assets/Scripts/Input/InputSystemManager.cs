using UnityEngine;
namespace Input
{
    public class InputSystemManager : MonoBehaviour
    {
        public static InputSystemManager Instance { get; private set; }
        
        private AvatarActions.UIActions _uiInputs;
        private AvatarActions.PlayerActions _gameplayInputs;

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
        }

        public void EnableUIInputs()
        {
            _gameplayInputs.Disable();
            _uiInputs.Enable();
        }
        public void EnableGameplayInputs()
        {
            _gameplayInputs.Enable();
            _uiInputs.Disable();
        }
    }
    
}

