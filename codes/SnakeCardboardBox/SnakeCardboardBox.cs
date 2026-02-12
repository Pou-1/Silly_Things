using Unity.Netcode;

namespace Silly_Things.codes.SnakeCardboardBox
{
    public class SnakeCardboardBox : PhysicsProp
    {
        private readonly SnakeCardboardBoxUi ui = new SnakeCardboardBoxUi();
        public static SnakeCardboardBox? Instance {get; set;}
        public static bool PlayerHiddenByBox = false;

        public override void OnNetworkSpawn()
        {
            base.OnNetworkSpawn();
            Instance = this;
        }
        public override void ItemActivate(bool used, bool buttonDown = true)
        {
            base.ItemActivate(used, buttonDown);

            if (!buttonDown)
                return;

            if (!playerHeldBy.IsOwner)
                return;

            if (ui.IsOpen)
            {
                ui.CloseUI();
                PlayerHiddenByBox = false;
            }
            else
            {
                ui.OpenUI();
                PlayerHiddenByBox = true;
            }
        }

        public override void DiscardItem()
        {
            base.DiscardItem();
            ui.CloseUI();
            PlayerHiddenByBox = false;
        }

        public override void OnDestroy()
        {
            base.OnDestroy();
            PlayerHiddenByBox = false;
        }

        [ServerRpc(RequireOwnership = false)]
        public void SyncSoundsServerRpc(int idSound)
        {
            SyncSoundsClientRpc(idSound);
        }

        [ClientRpc]
        public void SyncSoundsClientRpc(int idSound)
        {
            if(idSound == 0)
                HUDManager.Instance.UIAudio.PlayOneShot(Plugin.Instance.SoundOpenUI);
            else if (idSound == 1)
                HUDManager.Instance.UIAudio.PlayOneShot(Plugin.Instance.SoundCloseUI);
        }
    }
}
