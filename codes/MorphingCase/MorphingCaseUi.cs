using GameNetcodeStuff;
using Lethal_Battle;
using LethalLib.Modules;
using MoreCompany.Cosmetics;
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
        private GameObject uiInstance;

        private Button closeButton;
        private Button resetButton;
        private Button buttonLeft;
        private Button buttonRight;

        private Transform content;
        private Transform contentRight;
        private Transform playerTemplate;

        private ManageCosmetics cosmeticsManager;

        private List<PlayerControllerB> overflowPlayers = new List<PlayerControllerB>();
        private int currentPage = 0;
        private const int playersPerPage = 6;

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
                    Plugin.log.LogInfo("[MorphingCaseUi] Reset button clicked");
                    cosmeticsManager.RestorePreviousCosmetics();
                    resetButton.interactable = false;
                });
                resetButton.interactable = false;
            }

            if (buttonLeft != null)
                buttonLeft.onClick.AddListener(() => ChangePage(-1));

            if (buttonRight != null)
                buttonRight.onClick.AddListener(() => ChangePage(1));

            if (playerTemplate != null)
                playerTemplate.gameObject.SetActive(false);
        }

        private void BuildPlayerList()
        {
            Plugin.log.LogInfo("[MorphingCaseUi] BuildPlayerList");

            if (content == null || contentRight == null || playerTemplate == null)
            {
                Plugin.log.LogError("[MorphingCaseUi] Content panels or template null");
                return;
            }

            foreach (Transform child in content)
                if (child != playerTemplate) Object.Destroy(child.gameObject);

            foreach (Transform child in contentRight)
                if (child != playerTemplate) Object.Destroy(child.gameObject);

            overflowPlayers.Clear();
            currentPage = 0;

            PlayerControllerB localPlayer = StartOfRound.Instance.localPlayerController;

            int leftCount = 0;
            int rightCount = 0;

            foreach (PlayerControllerB player in StartOfRound.Instance.allPlayerScripts)
            {
                if (!ManageCosmetics.IsValidPlayer(player))
                    continue;

                if (player == localPlayer)
                    continue;

                Plugin.log.LogError($"{player.playerUsername}  ---------------");
                Plugin.log.LogError($"{leftCount} {rightCount}  ---------------");

                if (leftCount < 3)
                {
                    CreatePlayerEntry(player, content);
                    leftCount++;
                }
                else if (rightCount < 3)
                {
                    CreatePlayerEntry(player, contentRight);
                    rightCount++;
                }
                else
                {
                    overflowPlayers.Add(player);
                }
            }

            Plugin.log.LogInfo($"[MorphingCaseUi] Overflow players count: {overflowPlayers.Count}");
            RefreshPage();
        }

        private void CreatePlayerEntry(PlayerControllerB source, Transform parentContent)
        {
            GameObject clone = Object.Instantiate(playerTemplate.gameObject, parentContent);
            clone.SetActive(true);

            TMP_Text name = clone.transform.Find("ButtonPlayer/PlayerName")?.GetComponent<TMP_Text>();
            if (name != null)
                name.text = source.playerUsername;

            Button btn = clone.transform.Find("ButtonPlayer")?.GetComponent<Button>();
            if (btn != null)
            {
                btn.onClick.RemoveAllListeners();
                btn.onClick.AddListener(() =>
                {
                    Plugin.log.LogInfo($"[MorphingCaseUi] Clicked on {source.playerUsername}");
                    cosmeticsManager.MorphToPlayer(source);
                    if (resetButton != null)
                        resetButton.interactable = true;
                });
            }
            Plugin.log.LogError($"{source.playerUsername} {parentContent} Create Button  ---------------");


            RawImage avatar = clone.transform.Find("PlayerAvatar")?.GetComponent<RawImage>();
            if (avatar != null && !GameNetworkManager.Instance.disableSteam)
                HUDManager.FillImageWithSteamProfile(avatar, source.playerSteamId);
        }

        private void ChangePage(int delta)
        {
            int maxPage = Mathf.CeilToInt((float)overflowPlayers.Count / playersPerPage) - 1;
            currentPage = Mathf.Clamp(currentPage + delta, 0, Mathf.Max(0, maxPage));

            Plugin.log.LogInfo($"[MorphingCaseUi] Changing to page {currentPage}");
            RefreshPage();
        }

        private void RefreshPage()
        {
            foreach (Transform child in content)
                if (child != playerTemplate) Object.Destroy(child.gameObject);

            foreach (Transform child in contentRight)
                if (child != playerTemplate) Object.Destroy(child.gameObject);

            int startIndex = currentPage * playersPerPage;
            for (int i = 0; i < playersPerPage; i++)
            {
                int index = startIndex + i;
                if (index >= overflowPlayers.Count) break;

                PlayerControllerB player = overflowPlayers[index];
                Transform targetContent = i < 3 ? content : contentRight;
                CreatePlayerEntry(player, targetContent);
                Plugin.log.LogError($"{player.playerUsername} Create Player  ---------------");
            }

            if (buttonLeft != null)
                buttonLeft.interactable = currentPage > 0;

            if (buttonRight != null)
                buttonRight.interactable = currentPage < Mathf.CeilToInt((float)overflowPlayers.Count / playersPerPage) - 1;
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
