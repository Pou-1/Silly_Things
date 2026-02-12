using UnityEngine;

namespace Silly_Things.codes.SnakeCardboardBox
{
    internal class SnakeCardboardBoxUi
    {
        public bool IsOpen => isUIOpen;

        private bool isUIOpen;
        private GameObject? uiInstance;

        public bool CanOpenUI(bool buttonDown)
        {
            return buttonDown && !isUIOpen && Plugin.Instance.UI_SnakeCardboardBox != null;
        }

        public void OpenUI()
        {
            SnakeCardboardBox.Instance?.SyncSoundsServerRpc(0);

            uiInstance = Object.Instantiate(Plugin.Instance.UI_SnakeCardboardBox);
            if (uiInstance == null)
            {
                Plugin.Logger.LogError("UI instance null");
                return;
            }

            StartOfRound.Instance.localPlayerController.Crouch(true);
            StartOfRound.Instance.localPlayerController.movementAudio.Pause();
            SnakeCardboardBox.PlayerHiddenByBox = true;
            
            isUIOpen = true;
        }

        public void CloseUI()
        {
            SnakeCardboardBox.Instance?.SyncSoundsServerRpc(1);

            if (uiInstance != null)
                Object.Destroy(uiInstance);

            StartOfRound.Instance.localPlayerController.Crouch(false);
            StartOfRound.Instance.localPlayerController.movementAudio.UnPause();
            SnakeCardboardBox.PlayerHiddenByBox = false;

            uiInstance = null;
            isUIOpen = false;
        }
    }
}
