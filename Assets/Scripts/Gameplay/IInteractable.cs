namespace Game.Gameplay
{
    public interface IInteractable
    {
        bool CanInteract();
        void Interact();
        string GetInteractionPrompt();
    }
}
