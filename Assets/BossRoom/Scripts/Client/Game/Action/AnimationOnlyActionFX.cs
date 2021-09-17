namespace BossRoom.Visual
{
    /// <summary>
    /// Used for simple Actions that only need to play a few animations (one at startup and optionally
    /// one at end). Lasts a fixed duration as specified in the ActionDescription
    /// </summary>
    public class AnimationOnlyActionFX : ActionFX
    {
        public AnimationOnlyActionFX(ref ActionRequestData data, ClientCharacterVisualization parent) : base(ref data, parent) { }

        private void PlayStartAnim()
        {
            m_Parent.OurAnimator.SetTrigger(Description.Anim);
        }

        public override void AnticipateAction()
        {
            base.AnticipateAction();
            PlayStartAnim();
        }

        public override bool Update()
        {
            return ActionConclusion.Continue;
        }
    }
}
