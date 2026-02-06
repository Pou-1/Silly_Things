using GameNetcodeStuff;
using UnityEngine;

namespace Silly_Things.codes.MorphingCase
{
    public class MorphingCase : PhysicsProp
    {
        private readonly MorphingCaseUi ui = new MorphingCaseUi();

        public override void ItemActivate(bool used, bool buttonDown = true)
        {
            base.ItemActivate(used, buttonDown);

            if (playerHeldBy != StartOfRound.Instance.localPlayerController)
                return;

            if (!ui.CanOpenUI(buttonDown))
                return;

            ui.OpenUI();
        }

        public override void DiscardItem()
        {
            base.DiscardItem();
            ui.ForceCloseUI();
        }
    }
}
