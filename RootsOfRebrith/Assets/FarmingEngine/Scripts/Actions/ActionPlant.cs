using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace FarmingEngine
{
    /// <summary>
    /// Sow a seed in the ground.
    /// </summary>
    
    [CreateAssetMenu(fileName = "Action", menuName = "FarmingEngine/Actions/Plant", order = 50)]
    public class ActionPlant : SAction
    {
        public override void DoAction(PlayerCharacter character, ItemSlot slot)
        {
            ItemData item = slot.GetItem();
            InventoryData inventory = slot.GetInventory();
            if (item != null)
            {
                character.Crafting.BuildItemBuildMode(inventory, slot.index);
            }
        }

        public override bool CanDoAction(PlayerCharacter character, ItemSlot slot)
        {
            ItemData item = slot.GetItem();
            return item != null && item.plant_data != null;
        }
    }

}