using Input.Controls;

namespace Input
{
    public class InputManager
    {
        public static InputManager Instance => instance ??= new InputManager();
        private static InputManager instance;

        public IPawnControls PawnControls => pawnControls;
        private PawnControls pawnControls;

        private RogueLikeInput rogueLikeInput;

        private InputManager() => Init();

        private void Init()
        {
            rogueLikeInput = new RogueLikeInput();
            rogueLikeInput.Pawn.Enable();

            pawnControls = new PawnControls(rogueLikeInput);
        }

        public void Dispose()
        {
            pawnControls.Dispose();

            rogueLikeInput.Pawn.Disable();
            rogueLikeInput.Dispose();
        }
    }
}
