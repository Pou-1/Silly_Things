using GameNetcodeStuff;
using Lethal_Battle;
using MoreCompany.Cosmetics;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Windows;

namespace Silly_Things.codes.MorphingCase
{
    internal class MorphingCaseUi
    {
        public bool IsOpen => isUIOpen;

        private bool isUIOpen;
        private GameObject uiInstance;

        private Button closeButton;
        private Button resetButton;
        private Transform content;
        private Transform playerTemplate;

        private ManageCosmetics cosmeticsManager;

        public bool CanOpenUI(bool buttonDown)
        {
            bool canOpen = buttonDown && !isUIOpen && Plugin.instance.UI_MorphingCase != null;
            Plugin.log.LogInfo($"[MorphingCaseUi] CanOpenUI: {canOpen}");
            return canOpen;
        }

        public void OpenUI()
        {
            Plugin.log.LogInfo("[MorphingCaseUi] OpenUI called");

            uiInstance = Object.Instantiate(Plugin.instance.UI_MorphingCase);
            if (uiInstance == null)
            {
                Plugin.log.LogError("[MorphingCaseUi] UI instance null");
                return;
            }

            cosmeticsManager = new ManageCosmetics();

            CacheUIRefs();
            EnableCursor(true);
            BuildPlayerList();

            isUIOpen = true;
            Plugin.log.LogInfo("[MorphingCaseUi] UI opened");
        }

        public void ForceCloseUI()
        {
            Plugin.log.LogInfo("[MorphingCaseUi] ForceCloseUI called");

            if (uiInstance != null)
                Object.Destroy(uiInstance);

            uiInstance = null;
            isUIOpen = false;

            EnableCursor(false);

            Plugin.log.LogInfo("[MorphingCaseUi] UI closed");
        }

        private void CacheUIRefs()
        {
            Plugin.log.LogInfo("[MorphingCaseUi] CacheUIRefs");

            closeButton = uiInstance.transform.Find("Panel/Panel/ButtonClose").GetComponent<Button>();
            resetButton = uiInstance.transform.Find("Panel/Panel/ButtonReset").GetComponent<Button>();
            content = uiInstance.transform.Find("Panel/Panel/PanelLeft/Viewport/Content");
            playerTemplate = content.Find("PlayerNames");

            if (closeButton != null)
                closeButton.onClick.AddListener(ForceCloseUI);
            else
                Plugin.log.LogError("[MorphingCaseUi] Close button not found");

            if (resetButton != null)
            {
                resetButton.onClick.AddListener(() =>
                {
                    Plugin.log.LogInfo("[MorphingCaseUi] Reset button clicked");
                    cosmeticsManager.RestorePreviousCosmetics();
                    resetButton.interactable = false;
                });
                resetButton.interactable = false;
            }
            else
                Plugin.log.LogError("[MorphingCaseUi] Reset button not found");

            if (playerTemplate != null)
                playerTemplate.gameObject.SetActive(false);
            else
                Plugin.log.LogError("[MorphingCaseUi] Player template not found");
        }

        private void BuildPlayerList()
        {
            Plugin.log.LogInfo("[MorphingCaseUi] BuildPlayerList");

            if (content == null || playerTemplate == null)
            {
                Plugin.log.LogError("[MorphingCaseUi] Content or template null");
                return;
            }

            foreach (Transform child in content)
            {
                if (child != playerTemplate)
                    Object.Destroy(child.gameObject);
            }

            PlayerControllerB localPlayer = StartOfRound.Instance.localPlayerController;

            foreach (PlayerControllerB player in StartOfRound.Instance.allPlayerScripts)
            {
                if (!ManageCosmetics.IsValidPlayer(player))
                    continue;

                if (player == localPlayer)
                {
                    Plugin.log.LogInfo("[MorphingCaseUi] Skipping local player");
                    continue;
                }

                CreatePlayerEntry(player);
            }

        }

        private void CreatePlayerEntry(PlayerControllerB source)
        {
            GameObject clone = Object.Instantiate(playerTemplate.gameObject, content);
            clone.SetActive(true);

            Plugin.log.LogInfo($"[MorphingCaseUi] Clone PlayerNames for {source.playerUsername}");

            TMP_Text name = clone.transform.Find("ButtonPlayer/PlayerName")?.GetComponent<TMP_Text>();

            if (name != null)
                name.text = source.playerUsername;
            else
                Plugin.log.LogError("[MorphingCaseUi] PlayerName TMP not found");

            Button btn = clone.transform.Find("ButtonPlayer")?.GetComponent<Button>();

            if (btn == null)
            {
                Plugin.log.LogError("[MorphingCaseUi] ButtonPlayer NOT FOUND");
                return;
            }

            btn.onClick.RemoveAllListeners();
            btn.onClick.AddListener(() =>
            {
                Plugin.log.LogInfo($"[MorphingCaseUi] Clicked on {source.playerUsername}");
                cosmeticsManager.MorphToPlayer(source);

                if (resetButton != null)
                    resetButton.interactable = true;
            });

            RawImage avatar = clone.transform.Find("PlayerAvatar")?.GetComponent<RawImage>();

            if (avatar != null && !GameNetworkManager.Instance.disableSteam)
                HUDManager.FillImageWithSteamProfile(avatar, source.playerSteamId);
            else
                Plugin.log.LogWarning("[MorphingCaseUi] PlayerAvatar not filled");
        }


        private static void EnableCursor(bool state)
        {
            Cursor.visible = state;
            Cursor.lockState = state ? CursorLockMode.None : CursorLockMode.Locked;
            StartOfRound.Instance.localPlayerController.disableLookInput = state;

            Plugin.log.LogInfo($"[MorphingCaseUi] Cursor state: {state}");
        }
    }
}
