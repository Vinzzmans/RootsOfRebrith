using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace FarmingEngine
{
    [RequireComponent(typeof(PlayerCharacter))]
    public class PlayerCharacterRide : MonoBehaviour
    {
        private PlayerCharacter character;
        private StarterAssets.StarterAssetsInputs _inputs;

        private bool is_riding = false;
        private AnimalRide riding_animal = null;

        void Awake()
        {
            character = GetComponent<PlayerCharacter>();
            _inputs = GetComponentInParent<StarterAssets.StarterAssetsInputs>();
            if (_inputs == null)
                Debug.LogWarning("PlayerCharacterRide: StarterAssetsInputs not found on parent.");
        }

        void Update()
        {
            if (TheGame.Get().IsPaused() || character.IsDead())
                return;

            if (!is_riding) return;

            if (riding_animal == null || riding_animal.IsDead())
            {
                StopRide();
                return;
            }

            transform.position = riding_animal.GetRideRoot();
            transform.rotation = Quaternion.LookRotation(riding_animal.transform.forward, Vector3.up);

            if (character.IsControlsEnabled() && _inputs != null)
            {
                if (_inputs.ConsumeJumpPressed() || _inputs.ConsumeInteractPressed() || _inputs.ConsumeUICancelPressed())
                    StopRide();
            }
        }

        public void RideNearest()
        {
            AnimalRide animal = AnimalRide.GetNearest(transform.position, 2f);
            RideAnimal(animal);
        }

        public void RideAnimal(AnimalRide animal)
        {
            if (!is_riding && character.IsMovementEnabled() && animal != null)
            {
                is_riding = true;
                character.SetBusy(true);
                character.DisableMovement();
                character.DisableCollider();
                riding_animal = animal;
                transform.position = animal.GetRideRoot();
                animal.SetRider(character);
            }
        }

        public void StopRide()
        {
            if (!is_riding) return;

            if (riding_animal != null)
                riding_animal.StopRide();

            is_riding = false;
            character.SetBusy(false);
            character.EnableMovement();
            character.EnableCollider();
            character.FaceDir(transform.forward);
            character.StopMove();
            riding_animal = null;
        }

        public bool IsRiding() => is_riding;
        public AnimalRide GetAnimal() => riding_animal;
        public PlayerCharacter GetCharacter() => character;
    }
}
