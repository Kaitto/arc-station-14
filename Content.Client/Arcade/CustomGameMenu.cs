using Content.Client.Arcade.UI;
using Content.Shared.Arcade;
using Robust.Client.ResourceManagement;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Client.UserInterface.CustomControls;
using Robust.Shared.Input;
using Robust.Shared.Timing;
using System.Numerics;
using static Content.Shared.Arcade.SharedCustomGameArcadeComponent;
using Vector3 = Robust.Shared.Maths.Vector3;


namespace Content.Client.Arcade
{
    public sealed class CustomGameMenu : DefaultWindow
    {
        public event Action<PlayerAction>? OnAction;
        public event Action<EntityData>? OnUpdate;
        private readonly PanelContainer _mainPanel;
        private Label _test_label;
        public CustomGameScreen gameScreen;

        public CustomGameMenu()
        {
            gameScreen = new CustomGameScreen();
            _mainPanel = new PanelContainer();
            _test_label = new Label();

            Title = "Custom Game Title";
            MinSize = SetSize = new Vector2(320, 320);
            _test_label.Text = "Hello world";
            _test_label.Visible = false;
            //_mainPanel.AddChild(_test_label);

            //_mainPanel.AddChild(gameScreen);
            gameScreen.AddChild(_test_label);
            gameScreen.HorizontalAlignment = HAlignment.Stretch;
            gameScreen.VerticalAlignment = VAlignment.Stretch;
            Contents.HorizontalAlignment = HAlignment.Stretch;
            Contents.VerticalAlignment = VAlignment.Stretch;
            Contents.AddChild(gameScreen);
            gameScreen.Win = Contents;
            CanKeyboardFocus = true;
            GrabKeyboardFocus();
        }
        public void ReceiveMessage(BoundUserInterfaceMessage message)
        {
            gameScreen.ReceiveMessage(message);
        }
        protected override void Resized()
        {
            base.Resized();
        }
        protected override void FrameUpdate(FrameEventArgs args)
        {
            base.FrameUpdate(args);
            if (HasKeyboardFocus())
            {
                this.Title= "> Custom Game Title <";
            } else
            {
                this.Title = "- Custom Game Title -";
            }
            gameScreen.Tick(args.DeltaSeconds);
        }
        protected override void KeyboardFocusEntered()
        {
            _test_label.Text = "KeyboardFocusEntered()";
        }
        protected override void KeyboardFocusExited()
        {
            _test_label.Text = "KeyboardFocusExited()";
        }
        protected override void ControlFocusExited()
        {
            _test_label.Text = "ControlFocusExited()";
        }
        public void UpdateGameState()
        {
            var data = new EntityData();
            data.position = new Vector3(gameScreen.dgame.position.X, gameScreen.dgame.position.Y, gameScreen.dgame.positionZ);
            data.Angle = gameScreen.dgame.rotation.Angle;
            data.Id = new NetEntity(); //should be the arcade id on server
            data.velocity = gameScreen.dgame.velocity;
            data.inputVelocity = gameScreen.dgame.inputVelocity;
            data.inputRotation = gameScreen.dgame.inputRotation.Angle;
            OnUpdate?.Invoke(data);
        }
        protected override void KeyBindDown(GUIBoundKeyEventArgs args)
        {
            base.KeyBindDown(args);
            //_test_label.Text = $"KeyBindDown({args.Function}):{args.Handled}";
            if (args.Function == EngineKeyFunctions.UIClick)
            {
                GrabKeyboardFocus();
            }

            if (HasKeyboardFocus())
            {
                if (gameScreen.SetKeyBindDown(args))
                {
                    UpdateGameState();
                }
            }
        }
        protected override void KeyBindUp(GUIBoundKeyEventArgs args)
        {
            base.KeyBindUp(args);
            if (args.Function == EngineKeyFunctions.UIClick)
            {
                GrabKeyboardFocus();
            }
            //_test_label.Text = $"KeyBindUp({args.Function}):{args.Handled}";
            if (HasKeyboardFocus())
            {
                if (gameScreen.SetKeyBindUp(args))
                {
                    UpdateGameState();
                }
            }
        }
    }
}
