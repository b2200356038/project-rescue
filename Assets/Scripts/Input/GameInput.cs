using UnityEngine;
namespace Input
{
    static class GameInput
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void RuntimeInitializeOnLoad()
        {
            Actions = new AvatarActions();
            Actions.Enable();
        }
        internal static AvatarActions Actions { get; private set; } = null!;
    }
}