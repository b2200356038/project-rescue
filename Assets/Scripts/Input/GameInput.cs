using UnityEngine;
namespace Game.Input
{
    public static class GameInput
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void RuntimeInitializeOnLoad()
        {
            Actions = new GameActions();
            Actions.Enable();
        }
        public static GameActions Actions { get; private set; } = null!;
    }
}