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
            case enumItemContextAction.Unload:
                return "Unload";
            default:
                return action.ToString();
        }
    }

    /// <summary>Executes the requested context menu action for an inventory item.</summary>
    /// <param name="action">The action selected from the context menu.</param>
    /// <param name="item">The item the action applies to.</param>
    /// <param name="playerInventory">The inventory that currently owns the item.</param>
    /// <param name="actionSound">Optional sound played directly when the action succeeds.</param>
    public static void ExecuteAction(enumItemContextAction action, F_Item item, F_PlayerInventory playerInventory, AudioClip actionSound = null)
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
            case enumItemContextAction.Unload:
                F_Item_Weapon weapon = item as F_Item_Weapon;
                bool wasUnloaded = F_Utility_Helper_Inventory.UnloadWeaponAmmoToInventory(weapon, playerInventory) > 0;
                if (wasUnloaded && actionSound != null)
                {
                    F_Logic_Audio.PlaySound(actionSound, EnumSoundType.Direct);
                }

                break;
            default:
                Debug.LogWarning($"F_Utility_Helper_ContextMenu: No execution defined for action '{action}'.");
                break;
        }
    }
}
