namespace Silly_Things.codes.MorphingCase
{
    public class MorphingCase : PhysicsProp
    {
        private readonly MorphingCaseUi ui = new MorphingCaseUi();

        public override void ItemActivate(bool used, bool buttonDown = true)
        {
            base.ItemActivate(used, buttonDown);

            if (!playerHeldBy.IsOwner)
                return;

            if (ui.IsOpen)
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
