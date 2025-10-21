using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace FarmingEngine
{
    [RequireComponent(typeof(PlayerCharacter))]
    public class PlayerCharacterHoe : MonoBehaviour
    {
        public GroupData hoe_item;
        public ConstructionData hoe_soil;
        public float hoe_build_radius = 0.5f;
        public int hoe_energy = 1;

        private PlayerCharacter character;
        private StarterAssets.StarterAssetsInputs _inputs;

        void Awake()
        {
            character = GetComponent<PlayerCharacter>();
            _inputs = GetComponentInParent<StarterAssets.StarterAssetsInputs>();
            if (_inputs == null)
                Debug.LogWarning("PlayerCharacterHoe: StarterAssetsInputs not found on parent.");
        }

        private void Update()
        {
            if (_inputs != null &&
                _inputs.ConsumeAttackPressed() &&
                character.IsControlsEnabled())
            {
                Vector3 hoe_pos = character.GetInteractCenter() + character.GetFacing() * 1f;
                HoeGround(hoe_pos);
            }
        }

        public void HoeGround(Vector3 pos)
        {
            if (!CanHoe())
                return;

            character.StopMove();
            character.Attributes.AddAttribute(AttributeType.Energy, -hoe_energy);

            character.TriggerAnim(character.Animation ? character.Animation.hoe_anim : "", pos);
            character.TriggerBusy(0.8f, () =>
            {
                Construction prev = Construction.GetNearest(pos, hoe_build_radius);
                Plant plant = Plant.GetNearest(pos, hoe_build_radius);
                if (prev != null && plant == null && prev.data == hoe_soil)
                {
                    prev.Destroy(); //Destroy previous, if no plant on it
                    return;
                }

                Construction construct = Construction.CreateBuildMode(hoe_soil, pos);
                construct.GetBuildable().StartBuild(character);
                construct.GetBuildable().SetBuildPositionTemporary(pos);
                if (construct.GetBuildable().CheckIfCanBuild())
                    construct.GetBuildable().FinishBuild();
                else
                    Destroy(construct.gameObject);
            });
        }

        public bool CanHoe()
        {
            bool has_energy = character.Attributes.GetAttributeValue(AttributeType.Energy) >= hoe_energy;
            InventoryItemData ivdata = character.EquipData.GetEquippedItem(EquipSlot.Hand);
            ItemData idata = ItemData.Get(ivdata?.item_id);
            return has_energy && idata != null && idata.HasGroup(hoe_item) && !character.IsBusy();
        }
    }
}
