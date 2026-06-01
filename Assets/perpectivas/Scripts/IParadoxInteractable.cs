namespace Perpectivas
{
    public interface IParadoxInteractable
    {
        string Prompt { get; }
        void Interact(ParadoxFirstPersonController player);
    }
}
