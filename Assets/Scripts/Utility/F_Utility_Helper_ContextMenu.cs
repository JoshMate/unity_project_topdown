using UnityEngine;

/// <summary>Provides the shared display labels and execution logic for inventory item context menu actions.</summary>
/// <remarks>Add new cases here whenever <see cref="enumItemContextAction"/> gains a value, so every
/// context menu option keeps a single, reusable source of behavior regardless of which item exposes it.</remarks>
public static class F_Utility_Helper_ContextMenu
{
    /// <summary>Returns the display label shown in the context menu for an action.</summary>
    public static string GetActionLabel(enumItemContextAction action)
    {
        switch (action)
        {
            case enumItemContextAction.Drop:
                return "Drop";
            default:
                return action.ToString();
        }
    }

    /// <summary>Executes the requested context menu action for an inventory item.</summary>
    /// <param name="action">The action selected from the context menu.</param>
    /// <param name="item">The item the action applies to.</param>
    /// <param name="playerInventory">The inventory that currently owns the item.</param>
    public static void ExecuteAction(enumItemContextAction action, F_Item item, F_PlayerInventory playerInventory)
    {
        if (item == null)
        {
            return;
        }

        switch (action)
        {
            case enumItemContextAction.Drop:
                // Omitting a cursor position drops the item on the floor beneath the player.
                F_Utility_Helper_Inventory.DropItemFromInventory(item, playerInventory);
                break;
            default:
                Debug.LogWarning($"F_Utility_Helper_ContextMenu: No execution defined for action '{action}'.");
                break;
        }
    }
}
