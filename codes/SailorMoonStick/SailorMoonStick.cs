using GameNetcodeStuff;
using Unity.Netcode;
using UnityEngine;

namespace Silly_Things.Codes.SailorMoonStick
{
    public class SailorMoonStick : PhysicsProp
    {
        private AudioSource? audio;

        private GameObject? currentAimFx;
        private ParticleSystem[]? magicParticles;
        private GameObject? localVolumeObject;

        public override void OnNetworkSpawn()
        {
            base.OnNetworkSpawn();

            localVolumeObject = transform.Find("Volume")?.GetComponent<GameObject>();
            audio = transform.Find("Audio")?.GetComponent<AudioSource>();
            magicParticles = GetComponentsInChildren<ParticleSystem>(true);
        }

        public override void EquipItem()
        {
            base.EquipItem();
            SetControlTips();
            isPocketed = false;

            SetupVolumeLayer();
        }

        public override void SetControlTipsForItem()
        {
            SetControlTips();
        }

        private void SetControlTips()
        {
            string[] allLines = {"Fix Light : [LMB]"};

            if (IsOwner)
            {
                HUDManager.Instance.ClearControlTips();
                HUDManager.Instance.ChangeControlTipMultiple(allLines, holdingItem: true, itemProperties);
            }
        }

        private void SetupVolumeLayer()
        {
            if (playerHeldBy == null || playerHeldBy.gameplayCamera == null || localVolumeObject == null)
                return;

            int cameraLayer = playerHeldBy.gameplayCamera.gameObject.layer;

            localVolumeObject.layer = cameraLayer;

            foreach (Transform t in localVolumeObject.GetComponentsInChildren<Transform>(true))
                t.gameObject.layer = cameraLayer;
        }

        public override void ItemActivate(bool used, bool buttonDown = true)
        {
            base.ItemActivate(used, buttonDown);

            if (!playerHeldBy.IsOwner || !buttonDown)
                return;

            Transform cam = playerHeldBy.gameplayCamera.transform;

            if (Physics.Raycast(cam.position + new Vector3(0.1f, 0.1f, 0.1f), cam.forward, out RaycastHit hit, 12f))
            {
                if (hit.collider.GetComponentInParent<PlayerControllerB>() != null || hit.transform.name == "Player")
                {
                    Plugin.Logger.LogInfo("Hit a player → Ignoring.");
                    return;
                }

                Light? targetLight = FindLight(hit.transform);
                if (targetLight == null)
                {
                    Plugin.Logger.LogInfo($"No light found on {hit.transform.name} → Ignoring.");
                    return;
                }

                Plugin.Logger.LogInfo("Sailor Moon magic used on light: " + targetLight.name);

                TurnOnTargetLight(targetLight);
                CastMagicClientRpc(hit.point);
            }
        }

        void PlayMagicParticles()
        {
            if (magicParticles == null)
                return;

            foreach (var p in magicParticles)
            {
                p.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
                p.Play();
            }
        }

        public void TurnOnTargetLight(Light l)
        {
            if (l != null)
            {
                l.enabled = true;
                l.intensity = 3f;
                l.range = 20f;
                l.color = new Color(1f, 0.92f, 0.75f);
            }
        }

        [ClientRpc]
        public void CastMagicClientRpc(Vector3 pos)
        {
            Plugin.Logger.LogInfo("Magic effect triggered");

            PlayMagicParticles();

            Collider[] cols = Physics.OverlapSphere(pos, 12f);

            foreach (var c in cols)
            {
                Light l = c.GetComponentInChildren<Light>();

                if (l != null)
                {
                    l.enabled = true;
                    l.intensity = 3f;
                    l.color = new Color(1f, 0.9f, 0.75f);
                }
            }
        }

        public override void Update()
        {
            if (!playerHeldBy || !playerHeldBy.IsOwner)
                return;

            Transform cam = playerHeldBy.gameplayCamera.transform;

            if (Physics.Raycast(cam.position, cam.forward, out RaycastHit hit, 12f))
            {
                ShowAimEffect(hit.point);
                return;
            }

            HideAimEffect();
        }

        public void ShowAimEffect(Vector3 pos)
        {
            if (currentAimFx != null)
                currentAimFx.transform.position = pos;
        }

        public void HideAimEffect()
        {
            if (currentAimFx != null)
                Destroy(currentAimFx);

            currentAimFx = null;
        }

        public Light? FindLight(Transform t)
        {
            Light l = t.GetComponent<Light>();
            if (l != null)
                return l;

            l = t.GetComponentInParent<Light>();
            if (l != null)
                return l;

            l = t.GetComponentInChildren<Light>();
            if (l != null)
                return l;

            foreach (Transform child in t.GetComponentsInChildren<Transform>(true))
            {
                l = child.GetComponent<Light>();
                if (l != null)
                    return l;
            }

            return null;
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
