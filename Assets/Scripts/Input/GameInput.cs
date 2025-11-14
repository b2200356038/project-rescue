using UnityEngine;
namespace Game.Input
{
    public static class GameInput
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void RuntimeInitializeOnLoad()
        {
            Actions = new AvatarActions();
            Actions.Enable();
        }
        public static AvatarActions Actions { get; private set; } = null!;
    }
}