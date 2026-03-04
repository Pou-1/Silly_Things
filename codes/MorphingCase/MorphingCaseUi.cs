using GameNetcodeStuff;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Silly_Things.codes.MorphingCase
{
    internal class MorphingCaseUi
    {
        public bool IsOpen => isUIOpen;

        private bool isUIOpen;
        private GameObject? uiInstance;

        private Button? closeButton;
        private Button? resetButton;
        private Button? buttonLeft;
        private Button? buttonRight;

        private Transform? content;
        private Transform? contentRight;
        private Transform? playerTemplate;

        private ManageCosmetics? cosmeticsManager;

        private List<PlayerControllerB> overflowPlayers = new List<PlayerControllerB>();
        private int currentPage = 0;
        private const int playersPerPage = 6;

        public bool CanOpenUI(bool buttonDown)
        {
            return buttonDown && !isUIOpen && Plugin.Instance.UI_MorphingCase != null;
        }

        public void OpenUI()
        {
            MorphingCase.Instance?.SyncSoundsServerRpc(0);

            uiInstance = Object.Instantiate(Plugin.Instance.UI_MorphingCase);
            if (uiInstance == null)
            {
                Plugin.Logger.LogError("UI instance null");
                return;
            }

            cosmeticsManager = Plugin.Instance.CosmeticsManager;

            CacheUIRefs();

            if (resetButton != null)
                resetButton.interactable = Plugin.Instance.CosmeticsManager.HasStoredCosmetics;

            EnableCursor(true);
            BuildPlayerList();

            isUIOpen = true;
        }

        public void ForceCloseUI()
        {
            MorphingCase.Instance?.SyncSoundsServerRpc(1);

            if (uiInstance != null)
                Object.Destroy(uiInstance);

            uiInstance = null;
            isUIOpen = false;

            EnableCursor(false);

        }

        private void CacheUIRefs()
        {
            if (uiInstance != null)
            {
                closeButton = uiInstance.transform.Find("Panel/Panel/ButtonClose")?.GetComponent<Button>();
                resetButton = uiInstance.transform.Find("Panel/Panel/ButtonReset")?.GetComponent<Button>();
                buttonLeft = uiInstance.transform.Find("Panel/ButtonLeft")?.GetComponent<Button>();
                buttonRight = uiInstance.transform.Find("Panel/ButtonRight")?.GetComponent<Button>();

                content = uiInstance.transform.Find("Panel/Panel/PanelLeft/Viewport/Content");
                contentRight = uiInstance.transform.Find("Panel/Panel/PanelRight/Viewport/Content");
                playerTemplate = content.Find("PlayerNames");

                if (closeButton != null)
                    closeButton.onClick.AddListener(ForceCloseUI);

                if (resetButton != null)
                {
                    resetButton.onClick.AddListener(() =>
                    {
                        if (cosmeticsManager != null)
                            cosmeticsManager.RestorePreviousCosmetics();
                        resetButton.interactable = false;
                    });
                    resetButton.interactable = false;
                }

                buttonLeft?.onClick.AddListener(() => ChangePage(-1));
                buttonRight?.onClick.AddListener(() => ChangePage(1));
                playerTemplate.gameObject.SetActive(false);
            }
        }

        private void BuildPlayerList()
        {
            if (content == null || contentRight == null || playerTemplate == null)
                return;

            foreach (Transform child in content)
                if (child != playerTemplate)
                    Object.Destroy(child.gameObject);

            foreach (Transform child in contentRight)
                if (child != playerTemplate)
                    Object.Destroy(child.gameObject);

            overflowPlayers.Clear();
            currentPage = 0;

            PlayerControllerB localPlayer = StartOfRound.Instance.localPlayerController;

            foreach (PlayerControllerB player in StartOfRound.Instance.allPlayerScripts)
            {
                if (!ManageCosmetics.IsValidPlayer(player))
                    continue;
                if (player == localPlayer)
                    continue;

                overflowPlayers.Add(player);
            }

            RefreshPage();
        }

        private void CreatePlayerEntry(PlayerControllerB source, Transform parentContent)
        {
            if (playerTemplate != null)
            {
                GameObject clone = Object.Instantiate(playerTemplate.gameObject, parentContent);
                clone.SetActive(true);

                TMP_Text? name = clone.transform.Find("ButtonPlayer/PlayerName")?.GetComponent<TMP_Text>();
                if (name != null)
                    name.text = source.playerUsername;

                Button? btn = clone.transform.Find("ButtonPlayer")?.GetComponent<Button>();
                if (btn != null)
                {
                    btn.onClick.RemoveAllListeners();
                    btn.onClick.AddListener(() =>
                    {
                        cosmeticsManager.MorphToPlayer(source);
                        if (resetButton != null)
                            resetButton.interactable = true;
                    });
                }

                RawImage avatar = clone.GetComponentInChildren<RawImage>(true);

                bool isMultiplayer = !GameNetworkManager.Instance.disableSteam;

                if (avatar != null)
                {
                    if (isMultiplayer)
                        HUDManager.FillImageWithSteamProfile(avatar, source.playerSteamId);
                    else
                        avatar.gameObject.SetActive(false);
                }
                else
                {
                    Plugin.Logger.LogError("avatar fail to clone");
                }
            }
        }

        private void ChangePage(int delta)
        {
            int maxPage = Mathf.Max(0, Mathf.CeilToInt((float)overflowPlayers.Count / playersPerPage) - 1);
            currentPage = Mathf.Clamp(currentPage + delta, 0, maxPage);
            RefreshPage();
        }

        private void RefreshPage()
        {
            if (content != null && contentRight != null)
            {
                foreach (Transform child in content)
                    if (child != playerTemplate)
                        Object.Destroy(child.gameObject);

                foreach (Transform child in contentRight)
                    if (child != playerTemplate)
                        Object.Destroy(child.gameObject);
                int startIndex = currentPage * playersPerPage;
                int endIndex = Mathf.Min(startIndex + playersPerPage, overflowPlayers.Count);

                for (int i = startIndex; i < endIndex; i++)
                {
                    PlayerControllerB player = overflowPlayers[i];
                    Transform target = (i - startIndex) < 3 ? content : contentRight;
                    CreatePlayerEntry(player, target);
                }

                int maxPage = Mathf.Max(0, Mathf.CeilToInt((float)overflowPlayers.Count / playersPerPage) - 1);
                bool hasMultiplePages = maxPage > 0;

                if (buttonLeft != null)
                {
                    buttonLeft.gameObject.SetActive(hasMultiplePages);
                    buttonLeft.interactable = hasMultiplePages && currentPage > 0;
                }

                if (buttonRight != null)
                {
                    buttonRight.gameObject.SetActive(hasMultiplePages);
                    buttonRight.interactable = hasMultiplePages && currentPage < maxPage;
                }
            }
        }

        private static void EnableCursor(bool state)
        {
            Cursor.visible = state;
            Cursor.lockState = state ? CursorLockMode.None : CursorLockMode.Locked;
            StartOfRound.Instance.localPlayerController.disableLookInput = state;
        }
    }
}