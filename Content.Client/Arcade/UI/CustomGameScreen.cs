using Content.Client.Arcade.FPS;
using Content.Client.Resources;
using Content.Shared.Input;
using Content.Shared.Inventory;
using Content.Shared.Movement.Events;
using Microsoft.VisualBasic;
using Robust.Client.GameObjects;
using Robust.Client.Graphics;
using Robust.Client.ResourceManagement;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Client.UserInterface.CustomControls;
using Robust.Shared.ContentPack;
using Robust.Shared.Input;
using Robust.Shared.Map.Enumerators;
using Robust.Shared.Physics;
using Serilog;
using SixLabors.ImageSharp.PixelFormats;
using System.IO;
using System.Linq;
using System.Numerics;
using static Content.Shared.Arcade.SharedCustomGameArcadeComponent;
using Vector3 = Robust.Shared.Maths.Vector3;

namespace Content.Client.Arcade.UI
{
    public partial class CustomGameScreen : Control
    {
        public EntityUid? Owner;
        public float AccDelta=0.0f;
        public CustomGameDebug dgame;
        Label debug_label;
        public Control? Win;
        protected override void Parented(Control newParent)
        {
            base.Parented(newParent);
        }
        public CustomGameScreen()
        {
            debug_label = new Label();
            debug_label.Visible = false;
            debug_label.Text = "Test";
            AddChild(debug_label);
            dgame = new CustomGameDebug(Size, UIScale);
            var resman=IoCManager.Resolve<IResourceManager>();

            if (resman.TryContentFileRead(new Robust.Shared.Utility.ResPath("/freedoom1.wad"), out var s))
            {
                dgame.data.LoadWad(s);
                dgame.data.LoadWadVertexes();
                dgame.data.LoadWadLineDefs();
                dgame.data.LoadWadSides();
                dgame.data.LoadWadSectors();
                dgame.data.LoadTextures();
            }
            else
            {
                debug_label.Visible = true;
                debug_label.Text = "ERROR Can't read wad";
            }
            ;


            OnResized += () =>
            {
                dgame.ScreenSize = Size;
                dgame.UIScale = UIScale;
            };
        }
        protected override void UIScaleChanged()
        {
            dgame.ScreenSize = Size;
            dgame.UIScale = UIScale;
        }
        private Texture? sample_texture = null;

        protected override void Draw(DrawingHandleScreen handle)
        {
            CustomGameDataTextureManager.Instance.BuildTextures(handle);
            base.Draw(handle);
            if (Parent != null && Win!=null)
            {
                dgame.Draw(handle);
            }
            //Okay
            //if (sample_texture==null)
            //{
            //    var pixels = new List<Rgba32>();
            //    for (var i =0;i<64*64;i++) pixels.Add(new Rgba32 { A = 255, R = 255 });
            //    var arr = pixels.ToArray();
            //    var span = new ReadOnlySpan<Rgba32>(arr, 0, 64 * 64);
            //    var owned_texture= IoCManager.Resolve<IClyde>().CreateBlankTexture<SixLabors.ImageSharp.PixelFormats.Rgba32>(new Vector2i(64, 64), $"FPS_debug");
            //    owned_texture.SetSubImage<Rgba32>(Vector2i.Zero, Vector2i.One*64, span);
            //    sample_texture = owned_texture;
                
            //}
            //handle.DrawTexture(sample_texture, Vector2.One * 32.0f);
            //handle.DrawRect(new UIBox2i(32, 32, 32 + sample_texture.Width, 32 + sample_texture.Height), Color.Green, false);
        }
        public void Tick(float delta)
        {
            //use a fixed timestep. 
            var fixedDelta = 1.0f / 24.0f;
            int maxSteps = 10;
            int step = 0;
            AccDelta += delta;

            while ((AccDelta - fixedDelta) > 0.0f && step < maxSteps)
            {
                dgame.Tick(fixedDelta);
                AccDelta -= fixedDelta;
                step++;
            }
        }
        private bool KeyBinds(GUIBoundKeyEventArgs args, bool down)
        {
            var modifier = down ? 1.0f : -1.0f;
            //maybe i can just use args.Status
            if (false) return false;
            else if (args.Function == EngineKeyFunctions.MoveUp) dgame.Input(new Vector2(1, 0) * modifier);
            else if (args.Function == EngineKeyFunctions.MoveDown) dgame.Input(new Vector2(-1, 0) * modifier);
            else if (args.Function == EngineKeyFunctions.MoveLeft) dgame.Input(new Vector2(0, -1) * modifier);
            else if (args.Function == EngineKeyFunctions.MoveRight) dgame.Input(new Vector2(0, +1) * modifier);
            else if (args.Function == ContentKeyFunctions.Drop) dgame.Input(Vector2.Zero, -5 * modifier);
            else if (args.Function == EngineKeyFunctions.TextCursorUp) dgame.eyeZ += down ? 2 : 0;
            else if (args.Function == EngineKeyFunctions.TextCursorDown) dgame.eyeZ += down ? -2 : 0;
            else if (args.Function == ContentKeyFunctions.ActivateItemInWorld) dgame.Input(Vector2.Zero, +5 * modifier);
            else
            {
                debug_label.Text = $"game: else {args.Function}";
                return false;
            }
            debug_label.Text = $"game: ok {args.Function}";
            return true;
        }
        public bool SetKeyBindDown(GUIBoundKeyEventArgs args)
        {
            base.KeyBindDown(args);
            debug_label.Text = $"game: {args.Function}";
            return KeyBinds(args, true);
        }
        public bool SetKeyBindUp(GUIBoundKeyEventArgs args)
        {
            base.KeyBindUp(args);
            return KeyBinds(args, false);
        }
        public void ReceiveMessage(BoundUserInterfaceMessage message)
        {
            switch (message)
            {
                case CustomGameUpdateMessage msg:
                    //dgame.EntityShapes.Clear();
                    //I don't know how to replicate a entity from the server yet, so i'll just map the net id to a local entity instead.
                    //So to share changes about a entity, i'll use the netid as a key. and the local entity is just for rendering the sprites.
                    foreach (var e in msg.EntityData)
                    {
                        //just replace data for now
                        dgame.Entities[e.Id] = e;
                        var entityManager = IoCManager.Resolve<IEntityManager>();
                        //Create a entityshape if it doesn't exists.
                        //Do not draw own shape
                        if (!dgame.EntityShapes.ContainsKey(e.Id))
                        {
                            var id = entityManager.Spawn(e.prototype);
                            var data = new CustomGameDebug.EntityShape(id, e.Id);
                            if (Owner.HasValue) data.Visible = !entityManager.GetEntity(e.Id).Equals(Owner.Value);
                            dgame.EntityShapes[e.Id]= data;
                        }
                    }
                    break;
                default:
                    break;
            }
        }
    }
}
