using GameNetcodeStuff;
using Silly_Things.codes.CameraItem;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

namespace Silly_Things.Codes.SailorMoonStick
{
    public class SailorMoonStick : PhysicsProp
    {
        private AudioSource? audio;
        private bool itemIsActive = false;

        //private GameObject? currentAimFx;
        private ParticleSystem[]? magicParticles;
        private GameObject? localVolumeObject;
        private float movementSpeedBase;

        public override void OnNetworkSpawn()
        {
            base.OnNetworkSpawn();

            localVolumeObject = transform.Find("Volume")?.GetComponent<GameObject>();
            audio = transform.Find("Audio")?.GetComponent<AudioSource>();
            magicParticles = GetComponentsInChildren<ParticleSystem>(true);
        }

        public override void ItemActivate(bool used, bool buttonDown = true)
        {
            base.ItemActivate(used, buttonDown);

            if (!IsOwner)
                return;

            itemIsActive = !itemIsActive;
            Modifiers(itemIsActive);
        }

        public override void Update()
        {
            base.Update();

            if (!isHeld || playerHeldBy == null)
                return;

            ApplyMoonGravity();
        }

        private void ApplyMoonGravity()
        {
            if (playerHeldBy.isPlayerDead)
                return;

            if (playerHeldBy.thisController.isGrounded)
                return;

            if (playerHeldBy.fallValue > 0f)
                playerHeldBy.fallValue -= 0.001f;

            if (playerHeldBy.fallValue < 0f)
                playerHeldBy.fallValue += 0.001f;
        }

        public void Modifiers(bool ModifyPlayer)
        {
            if (ModifyPlayer)
            {
                movementSpeedBase = playerHeldBy.movementSpeed;
                playerHeldBy.takingFallDamage = false;
                playerHeldBy.jumpForce = 30f;
                //playerHeldBy.movementSpeed = 0.7f;
                SyncSoundsClientRpc(0);
            }
            else
            {
                playerHeldBy.takingFallDamage = true;
                playerHeldBy.jumpForce = 5f;
                playerHeldBy.movementSpeed = movementSpeedBase;
                SyncSoundsClientRpc(1);
            }
        }

        [ClientRpc]
        public void SyncSoundsClientRpc(int idSound)
        {
            if (idSound == 0)
                audio?.PlayOneShot(Plugin.Instance.SoundOpenCardboardBox);
            else if (idSound == 1)
                audio?.PlayOneShot(Plugin.Instance.SoundCloseCardboardBox);
        }
    }
}
