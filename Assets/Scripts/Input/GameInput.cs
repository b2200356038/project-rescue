using UnityEngine;
namespace Input
{
    static class GameInput
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void RuntimeInitializeOnLoad()
        {
            Actions = new InputSystem_Actions();
            Actions.Enable();
        }
        internal static InputSystem_Actions Actions { get; private set; } = null!;
    }
}