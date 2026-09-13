using Entropy.Engine.UI;

namespace Entropy.Game.Systems;

public sealed class DemoObjective
{
    public const string StoreMapId = "store_interior";
    public const string SafeRoomMapId = "apartment_dana_interior";

    public bool StoreVisited { get; private set; }
    public bool SafeRoomReached { get; private set; }
    public bool Complete => StoreVisited && SafeRoomReached;

    public string Description => Complete
        ? "Demo complete: the supplies are safe."
        : StoreVisited
            ? "Return to Dana's apartment and reach the safe room."
            : "Reach the corner store and find supplies.";

    public void Update(GameContext context)
    {
        if (!StoreVisited && context.MapId == StoreMapId)
        {
            StoreVisited = true;
            context.Log.Add("You found the corner store. Now get back to safety.", UiTheme.Valid);
        }

        if (!SafeRoomReached && StoreVisited && context.MapId == SafeRoomMapId)
        {
            SafeRoomReached = true;
            context.Log.Add("You reach the safe room with supplies. The block can wait.", UiTheme.Valid);
        }
    }

    public void Restore(bool storeVisited, bool safeRoomReached)
    {
        StoreVisited = storeVisited;
        SafeRoomReached = safeRoomReached;
    }
}
