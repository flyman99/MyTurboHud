
namespace Turbo.Plugins
{
    using System.Windows.Forms;

    public interface IInteractionController
    {
        IPerfCounter PerfCounter { get; }

        void ClickAt(MouseButtons button, System.Drawing.RectangleF rect, double rndRatio);
        void ClickAt(MouseButtons button, double x, double y, double rndX, double rndY);

        void ClickHuman(MouseButtons button, System.Drawing.RectangleF rect, double rndRatio);
        void ClickHuman(MouseButtons button, double x, double y, double rndX, double rndY);

        void MoveMouseOverUiElement(IUiElement element);
        void ClickUiElement(MouseButtons button, IUiElement element, bool small = false, bool top = false);

        void ClickInventoryItem(MouseButtons button, IItem item);
        void MoveMouseOverInventoryItem(IItem item);

        void TalkTownActor(IActor actor);
        void TalkActor(IActor actor);

        bool IsHotKeySet(ActionKey key);
        void DoAction(ActionKey key, bool standStillDown = false, int sleepAfter = 10, int keyDownDelayOverride = -1, int keyDelayOverride = -1);

        // DoActionAutoStandStill
        void DoActionAutoShift(ActionKey key, int sleepAfter = 10, int keyDownDelayOverride = -1, int keyDelayOverride = -1);

        void StartContinuousAction(ActionKey key, bool standStillDown, int keyDownDelayOverride = -1, int keyDelayOverride = -1);
        void StopContinuousAction(ActionKey key, int keyDownDelayOverride = -1, int keyDelayOverride = -1);
        bool IsContinuousActionStarted(ActionKey key);

        void ToggleGameUi(bool visible, int keyDownDelayOverride = -1, int keyDelayOverride = -1);

        void PressEnter();
        void PressEsc();

        void ShiftDown();
        void ShiftUp();

        void StandStillDown();
        void StandStillUp();

        void ScrollUp(int count);
        void ScrollDown(int count);

        void StopAllContinuousActions();

        bool MouseMove(double x, double y, double rndX = 0, double rndY = 0, double length = -1, bool doEvents = true);
        void MouseDown(MouseButtons button);
        void MouseUp(MouseButtons button);

        bool NewGame();
        bool PressOkOnGenericModalDialog();
    }
}
