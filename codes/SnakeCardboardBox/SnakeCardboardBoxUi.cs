using UnityEngine;

namespace Silly_Things.codes.SnakeCardboardBox
{
    internal class SnakeCardboardBoxUi
    {
        public bool IsOpen => isUIOpen;

        private bool isUIOpen;
        private GameObject? uiInstance;
        public SnakeCardboardBox? box;

        public bool CanOpenUI(bool buttonDown)
        {
            return buttonDown && !isUIOpen && Plugin.Instance.UI_SnakeCardboardBox != null;
        }

        public void OpenUI()
        {
            uiInstance = Object.Instantiate(Plugin.Instance.UI_SnakeCardboardBox);
            if (uiInstance == null)
            {
                Plugin.Logger.LogError("UI instance null");
                return;
            }

            StartOfRound.Instance.localPlayerController.Crouch(true);
            StartOfRound.Instance.localPlayerController.movementAudio.Pause();

            if (box != null)
            {
                box.SyncSoundsServerRpc(0);
                box.PlayerHiddenByBox = true;
            }

            isUIOpen = true;
        }

        public void CloseUI()
        {
            if (uiInstance != null)
                Object.Destroy(uiInstance);

            StartOfRound.Instance.localPlayerController.Crouch(false);
            StartOfRound.Instance.localPlayerController.movementAudio.UnPause();
            
            if (box != null && isUIOpen)
            {
                box.SyncSoundsServerRpc(1);
                box.PlayerHiddenByBox = false;
            }

            uiInstance = null;
            isUIOpen = false;
        }
    }
}
