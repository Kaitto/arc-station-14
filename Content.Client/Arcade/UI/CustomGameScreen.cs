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
using Robust.Shared.Input;
using Robust.Shared.Map.Enumerators;
using Robust.Shared.Physics;
using Serilog;
using System.IO;
using System.Linq;
using System.Numerics;
using static Content.Shared.Arcade.SharedCustomGameArcadeComponent;
using Vector3 = Robust.Shared.Maths.Vector3;

namespace Content.Client.Arcade.UI
{
    public sealed class CustomGameScreen : Control
    {
        public struct GameData {
           
            public struct LineDef {
                public int start;
                public int end;
                public int front_sidedef;
                public int back_sidedef;

                //add flags, type, etc
                public LineDef(int start, int end, int front_sidedef, int back_sidedef)
                {
                    this.start = start;
                    this.end = end;
                    this.front_sidedef = front_sidedef;
                    this.back_sidedef = back_sidedef;
                }
            }
            public struct SideDef{
                /*int x_offset; int y_offset;*/
                public int sector;
                //could use a index to a texture list instead
                public string upper_texture;
                public string middle_texture;
                public string lower_texture;
                public SideDef(float ox, float oy, string ut, string lt, string mt, int s)
                {
                    sector = s;
                    upper_texture = ut;
                    middle_texture = mt;
                    lower_texture = lt;
                }
            }
            public struct Sector{
                public int floor_height;
                public int ceiling_height;
                //again, could index the texture
                public string floor_texture;
                public string ceiling_texture;
                //int light_level;
                //int special_type;
                //int tag;
                public Sector(int fh, int ch, string ft, string ct, int ll, int t, int tag)
                {
                    this.floor_height = fh;
                    this.ceiling_height = ch;
                    this.floor_texture = ft;
                    this.ceiling_texture = ct;

                }
            }
            //segments of a linedef
            public struct SegDef{
                public int start;
                public int end;
                public float angle;
                public int linedef_idx;
                //public bool direction;
                //public float offset;
                public SegDef(int s, int e ,float a, int l){
                    start = s;
                    end = e;
                    angle = a;
                    linedef_idx = l;
                }
            }
            public struct SubSectors{
                public int segs_num;
                public int segs_idx;
            }
            public struct ThingDef
            {
                public Vector2 position;
                float angle;
                int type;
                int flags;
                public ThingDef(float x, float y, float a, int t, int f)
                {
                    position = new Vector2(x, y);
                    angle = a;
                    type = t;
                    flags = f;
                }
            }
            public List<Vector2> lVertex = new();
            public List<LineDef> lLines = new();
            public List<SegDef> lSegs = new();
            public List<SubSectors> lSSectors = new();
            public List<Sector> lSectors = new();
            public List<SideDef> lSides = new();
            public List<ThingDef> lThings = new();

            public GameData()
            {
                //test scene / debug only
                lVertex.Add(new Vector2(-576, 384));
                lVertex.Add(new Vector2(-384, 384));
                lVertex.Add(new Vector2(-256, 448));
                lVertex.Add(new Vector2(-256, 128));
                lVertex.Add(new Vector2(-384, 192));
                lVertex.Add(new Vector2(-576, 192));
                lVertex.Add(new Vector2(-192, 448));
                lVertex.Add(new Vector2(-192, 128));
                lVertex.Add(new Vector2(-192, 768));
                lVertex.Add(new Vector2(-64, 768));
                lVertex.Add(new Vector2(-64, -192));
                lVertex.Add(new Vector2(-192, -192));
                lVertex.Add(new Vector2(128, 640));
                lVertex.Add(new Vector2(320, 640));
                lVertex.Add(new Vector2(320, 384));
                lVertex.Add(new Vector2(128, 384));
                lVertex.Add(new Vector2(-64, 384));
                lVertex.Add(new Vector2(-64, 320));
                lVertex.Add(new Vector2(320, 320));
                lVertex.Add(new Vector2(-64, 192));
                lVertex.Add(new Vector2(320, 192));
                lVertex.Add(new Vector2(320, 128));
                lVertex.Add(new Vector2(-64, 128));
                lVertex.Add(new Vector2(-64, -256));
                lVertex.Add(new Vector2(320, -256));
                lVertex.Add(new Vector2(320, -192));
                lVertex.Add(new Vector2(192, -128));
                lVertex.Add(new Vector2(192, 0));
                lVertex.Add(new Vector2(0, 384));
                lVertex.Add(new Vector2(0, 320));
                lVertex.Add(new Vector2(0, 128));
                lVertex.Add(new Vector2(0, 192));
                lVertex.Add(new Vector2(-542, 384));
                lVertex.Add(new Vector2(-542, 418));
                lVertex.Add(new Vector2(-576, 418));
                lVertex.Add(new Vector2(-64, 640));
                lVertex.Add(new Vector2(192, -256));
                lLines.Add(new LineDef(0, 32, 1, 59));
                lLines.Add(new LineDef(1, 2, 2, 65535));
                lLines.Add(new LineDef(2, 3, 3, 9));
                lLines.Add(new LineDef(3, 4, 4, 65535));
                lLines.Add(new LineDef(4, 5, 5, 65535));
                lLines.Add(new LineDef(5, 0, 0, 65535));
                lLines.Add(new LineDef(1, 4, 6, 7));
                lLines.Add(new LineDef(2, 6, 10, 65535));
                lLines.Add(new LineDef(6, 7, 11, 13));
                lLines.Add(new LineDef(7, 3, 8, 65535));
                lLines.Add(new LineDef(6, 8, 14, 65535));
                lLines.Add(new LineDef(8, 9, 15, 65535));
                lLines.Add(new LineDef(9, 16, 16, 20));
                lLines.Add(new LineDef(10, 11, 17, 65535));
                lLines.Add(new LineDef(11, 7, 12, 65535));
                lLines.Add(new LineDef(9, 12, 21, 65535));
                lLines.Add(new LineDef(12, 13, 22, 65535));
                lLines.Add(new LineDef(13, 14, 23, 65535));
                lLines.Add(new LineDef(14, 15, 24, 30));
                lLines.Add(new LineDef(16, 17, 18, 28));
                lLines.Add(new LineDef(15, 28, 19, 29));
                lLines.Add(new LineDef(17, 19, 25, 65535));
                lLines.Add(new LineDef(18, 29, 27, 65535));
                lLines.Add(new LineDef(14, 18, 26, 65535));
                lLines.Add(new LineDef(19, 22, 31, 34));
                lLines.Add(new LineDef(19, 31, 35, 65535));
                lLines.Add(new LineDef(20, 21, 36, 65535));
                lLines.Add(new LineDef(22, 10, 32, 42));
                lLines.Add(new LineDef(21, 30, 33, 43));
                lLines.Add(new LineDef(23, 10, 41, 65535));
                lLines.Add(new LineDef(24, 23, 40, 65535));
                lLines.Add(new LineDef(25, 24, 39, 65535));
                lLines.Add(new LineDef(26, 25, 38, 65535));
                lLines.Add(new LineDef(21, 27, 44, 65535));
                lLines.Add(new LineDef(27, 26, 37, 65535));
                lLines.Add(new LineDef(28, 16, 45, 46));
                lLines.Add(new LineDef(29, 17, 47, 65535));
                lLines.Add(new LineDef(28, 29, 48, 49));
                lLines.Add(new LineDef(30, 22, 50, 51));
                lLines.Add(new LineDef(31, 20, 52, 65535));
                lLines.Add(new LineDef(31, 30, 53, 54));
                lLines.Add(new LineDef(32, 1, 55, 65535));
                lLines.Add(new LineDef(33, 32, 58, 65535));
                lLines.Add(new LineDef(34, 33, 57, 65535));
                lLines.Add(new LineDef(0, 34, 56, 65535));
                lSegs.Add(new SegDef(18, 29, 22, 0));
                lSegs.Add(new SegDef(29, 28, 37, 1));
                lSegs.Add(new SegDef(28, 15, 20, 1));
                lSegs.Add(new SegDef(15, 14, 18, 1));
                lSegs.Add(new SegDef(14, 18, 23, 0));
                lSegs.Add(new SegDef(29, 17, 36, 0));
                lSegs.Add(new SegDef(17, 16, 19, 1));
                lSegs.Add(new SegDef(16, 28, 35, 1));
                lSegs.Add(new SegDef(28, 29, 37, 0));
                lSegs.Add(new SegDef(35, 9, 12, 1));
                lSegs.Add(new SegDef(9, 12, 15, 0));
                lSegs.Add(new SegDef(14, 15, 18, 0));
                lSegs.Add(new SegDef(15, 28, 20, 0));
                lSegs.Add(new SegDef(28, 16, 35, 0));
                lSegs.Add(new SegDef(16, 35, 12, 1));
                lSegs.Add(new SegDef(12, 13, 16, 0));
                lSegs.Add(new SegDef(13, 14, 17, 0));
                lSegs.Add(new SegDef(24, 36, 30, 0));
                lSegs.Add(new SegDef(26, 25, 32, 0));
                lSegs.Add(new SegDef(25, 24, 31, 0));
                lSegs.Add(new SegDef(36, 23, 30, 0));
                lSegs.Add(new SegDef(27, 26, 34, 0));
                lSegs.Add(new SegDef(23, 10, 29, 0));
                lSegs.Add(new SegDef(10, 22, 27, 1));
                lSegs.Add(new SegDef(22, 30, 38, 1));
                lSegs.Add(new SegDef(30, 21, 28, 1));
                lSegs.Add(new SegDef(21, 27, 33, 0));
                lSegs.Add(new SegDef(21, 30, 28, 0));
                lSegs.Add(new SegDef(30, 31, 40, 1));
                lSegs.Add(new SegDef(31, 20, 39, 0));
                lSegs.Add(new SegDef(20, 21, 26, 0));
                lSegs.Add(new SegDef(30, 22, 38, 0));
                lSegs.Add(new SegDef(22, 19, 24, 1));
                lSegs.Add(new SegDef(19, 31, 25, 0));
                lSegs.Add(new SegDef(31, 30, 40, 0));
                lSegs.Add(new SegDef(19, 22, 24, 0));
                lSegs.Add(new SegDef(22, 10, 27, 0));
                lSegs.Add(new SegDef(10, 11, 13, 0));
                lSegs.Add(new SegDef(11, 7, 14, 0));
                lSegs.Add(new SegDef(7, 6, 8, 1));
                lSegs.Add(new SegDef(6, 8, 10, 0));
                lSegs.Add(new SegDef(8, 9, 11, 0));
                lSegs.Add(new SegDef(9, 35, 12, 0));
                lSegs.Add(new SegDef(35, 16, 12, 0));
                lSegs.Add(new SegDef(16, 17, 19, 0));
                lSegs.Add(new SegDef(17, 19, 21, 0));
                lSegs.Add(new SegDef(7, 3, 9, 0));
                lSegs.Add(new SegDef(3, 2, 2, 1));
                lSegs.Add(new SegDef(2, 6, 7, 0));
                lSegs.Add(new SegDef(6, 7, 8, 0));
                lSegs.Add(new SegDef(3, 4, 3, 0));
                lSegs.Add(new SegDef(4, 1, 6, 1));
                lSegs.Add(new SegDef(1, 2, 1, 0));
                lSegs.Add(new SegDef(2, 3, 2, 0));
                lSegs.Add(new SegDef(32, 0, 0, 1));
                lSegs.Add(new SegDef(0, 34, 44, 0));
                lSegs.Add(new SegDef(34, 33, 43, 0));
                lSegs.Add(new SegDef(33, 32, 42, 0));
                lSegs.Add(new SegDef(4, 5, 4, 0));
                lSegs.Add(new SegDef(5, 0, 5, 0));
                lSegs.Add(new SegDef(0, 32, 0, 0));
                lSegs.Add(new SegDef(32, 1, 41, 0));
                lSegs.Add(new SegDef(1, 4, 6, 0));
                lSectors.Add(new Sector(8, 128, "FLAT1", "FLAT1", 144, 0, 0));
                lSectors.Add(new Sector(0, 128, "FLAT1", "FLAT1", 176, 0, 0));
                lSectors.Add(new Sector(16, 96, "FLAT1", "FLAT1", 176, 0, 0));
                lSectors.Add(new Sector(16, 128, "FLAT1", "FLAT1", 144, 0, 0));
                lSectors.Add(new Sector(-16, 184, "FLAT1", "FLAT1", 128, 0, 0));
                lSectors.Add(new Sector(112, 160, "FLAT1", "FLAT1", 208, 0, 0));
                lSectors.Add(new Sector(112, 160, "FLAT1", "FLAT1", 208, 0, 0));
                lSectors.Add(new Sector(-16, 184, "FLAT1", "FLAT1", 128, 0, 0));
                lSectors.Add(new Sector(16, 160, "FLAT1", "FLAT1", 144, 0, 0));
                lSectors.Add(new Sector(16, 160, "FLAT1", "FLAT1", 144, 0, 0));
                lSectors.Add(new Sector(0, 50, "FLAT1", "FLAT1", 176, 0, 0));
                lSides.Add(new SideDef(0, 0, "GRAY1", "GRAY1", "GRAY1", 1));
                lSides.Add(new SideDef(0, 0, "GRAY1", "GRAY1", "-", 1));
                lSides.Add(new SideDef(0, 0, "GRAY1", "GRAY1", "GRAY1", 0));
                lSides.Add(new SideDef(0, 0, "GRAY1", "GRAY1", "-", 0));
                lSides.Add(new SideDef(0, 0, "GRAY1", "GRAY1", "GRAY1", 0));
                lSides.Add(new SideDef(0, 0, "GRAY1", "GRAY1", "GRAY1", 1));
                lSides.Add(new SideDef(0, 0, "GRAY1", "GRAY1", "-", 1));
                lSides.Add(new SideDef(0, 0, "GRAY1", "GRAY1", "-", 0));
                lSides.Add(new SideDef(0, 0, "GRAY1", "GRAY1", "GRAY1", 2));
                lSides.Add(new SideDef(0, 0, "GRAY1", "GRAY1", "-", 2));
                lSides.Add(new SideDef(0, 0, "GRAY1", "GRAY1", "GRAY1", 2));
                lSides.Add(new SideDef(0, 0, "GRAY1", "GRAY1", "-", 2));
                lSides.Add(new SideDef(0, 0, "GRAY1", "GRAY1", "GRAY1", 3));
                lSides.Add(new SideDef(0, 0, "GRAY1", "GRAY1", "-", 3));
                lSides.Add(new SideDef(0, 0, "GRAY1", "GRAY1", "GRAY1", 3));
                lSides.Add(new SideDef(0, 0, "GRAY1", "GRAY1", "GRAY1", 3));
                lSides.Add(new SideDef(0, 0, "GRAY1", "GRAY1", "-", 3));
                lSides.Add(new SideDef(0, 0, "GRAY1", "GRAY1", "GRAY1", 3));
                lSides.Add(new SideDef(0, 0, "GRAY1", "GRAY1", "-", 3));
                lSides.Add(new SideDef(0, 0, "GRAY1", "GRAY1", "-", 4));
                lSides.Add(new SideDef(0, 0, "GRAY1", "GRAY1", "-", 4));
                lSides.Add(new SideDef(0, 0, "GRAY1", "GRAY1", "GRAY1", 4));
                lSides.Add(new SideDef(0, 0, "GRAY1", "GRAY1", "GRAY1", 4));
                lSides.Add(new SideDef(0, 0, "GRAY1", "GRAY1", "GRAY1", 4));
                lSides.Add(new SideDef(0, 0, "GRAY1", "GRAY1", "-", 4));
                lSides.Add(new SideDef(0, 0, "GRAY1", "GRAY1", "GRAY1", 3));
                lSides.Add(new SideDef(0, 0, "GRAY1", "GRAY1", "GRAY1", 5));
                lSides.Add(new SideDef(0, 0, "GRAY1", "GRAY1", "GRAY1", 5));
                lSides.Add(new SideDef(0, 0, "GRAY1", "GRAY1", "-", 8));
                lSides.Add(new SideDef(0, 0, "GRAY1", "GRAY1", "-", 5));
                lSides.Add(new SideDef(0, 0, "GRAY1", "GRAY1", "-", 5));
                lSides.Add(new SideDef(0, 0, "GRAY1", "GRAY1", "-", 3));
                lSides.Add(new SideDef(0, 0, "GRAY1", "GRAY1", "-", 3));
                lSides.Add(new SideDef(0, 0, "GRAY1", "GRAY1", "-", 6));
                lSides.Add(new SideDef(0, 0, "GRAY1", "GRAY1", "-", 9));
                lSides.Add(new SideDef(0, 0, "GRAY1", "GRAY1", "GRAY1", 9));
                lSides.Add(new SideDef(0, 0, "GRAY1", "GRAY1", "GRAY1", 6));
                lSides.Add(new SideDef(0, 0, "GRAY1", "GRAY1", "GRAY1", 7));
                lSides.Add(new SideDef(0, 0, "GRAY1", "GRAY1", "GRAY1", 7));
                lSides.Add(new SideDef(0, 0, "GRAY1", "GRAY1", "GRAY1", 7));
                lSides.Add(new SideDef(0, 0, "GRAY1", "GRAY1", "GRAY1", 7));
                lSides.Add(new SideDef(0, 0, "GRAY1", "GRAY1", "GRAY1", 7));
                lSides.Add(new SideDef(0, 0, "GRAY1", "GRAY1", "-", 7));
                lSides.Add(new SideDef(0, 0, "GRAY1", "GRAY1", "-", 7));
                lSides.Add(new SideDef(0, 0, "GRAY1", "GRAY1", "GRAY1", 7));
                lSides.Add(new SideDef(0, 0, "GRAY1", "GRAY1", "-", 4));
                lSides.Add(new SideDef(0, 0, "GRAY1", "GRAY1", "-", 8));
                lSides.Add(new SideDef(0, 0, "GRAY1", "GRAY1", "GRAY1", 8));
                lSides.Add(new SideDef(0, 0, "GRAY1", "GRAY1", "-", 8));
                lSides.Add(new SideDef(0, 0, "GRAY1", "GRAY1", "-", 5));
                lSides.Add(new SideDef(0, 0, "GRAY1", "GRAY1", "-", 9));
                lSides.Add(new SideDef(0, 0, "GRAY1", "GRAY1", "-", 7));
                lSides.Add(new SideDef(0, 0, "GRAY1", "GRAY1", "GRAY1", 6));
                lSides.Add(new SideDef(0, 0, "GRAY1", "GRAY1", "-", 9));
                lSides.Add(new SideDef(0, 0, "GRAY1", "GRAY1", "-", 6));
                lSides.Add(new SideDef(0, 0, "GRAY1", "GRAY1", "GRAY1", 1));
                lSides.Add(new SideDef(0, 0, "GRAY1", "GRAY1", "GRAY1", 10));
                lSides.Add(new SideDef(0, 0, "GRAY1", "GRAY1", "GRAY1", 10));
                lSides.Add(new SideDef(0, 0, "GRAY1", "GRAY1", "GRAY1", 10));
                lSides.Add(new SideDef(0, 0, "GRAY1", "GRAY1", "-", 10));
                lThings.Add(new ThingDef(-480, 291, 0, 1, 7));
                lThings.Add(new ThingDef(-394, 270, 0, 2048, 7));


            }
            public void DrawFlat(DrawingHandleScreen handle)
            {
                //handle.SetTransform!!!
                foreach (Vector2 v in lVertex) handle.DrawCircle(v * new Vector2(1, -1), 4, Color.Green);
                foreach (LineDef l in lLines)
                {
                    var a = lVertex[l.start] * new Vector2(1, -1);
                    var b = lVertex[l.end] * new Vector2(1, -1);
                    if (l.back_sidedef == 65535)
                    {
                        handle.DrawLine(a, b, Color.Blue);
                    }
                    else
                    {
                        handle.DrawLine(a, b, Color.White);
                    };
                    
                    var d = (b - a);
                    var c = a + d / 2.0f;
                    var r = new Quaternion2D(d.ToAngle());
                    var f = Quaternion2D.RotateVector(r, Vector2.UnitY) * 15.0f;
                    handle.DrawLine(c, c + f, Color.AntiqueWhite);
                }
            }
        }
        public class DebugGame
        {
            public EntityUid? Owner;

            public Vector2 inputVelocity;
            public Quaternion2D inputRotation;
            public Vector2 position;
            public float positionZ=0.0f;
            public float eyeZ=0;
            public Vector2 velocity;
            public Quaternion2D rotation;
            public float fov = 60;
            public int slices = 64;
            public List<UIBox2> boxes = new List<UIBox2>();
            public int currentSector = -1;
            private GameData data = new();
            private float headbob = 0.0f;

            public Dictionary<NetEntity, EntityData> Entities = new();
            public Dictionary<NetEntity, EntityShape> EntityShapes = new();

            public List<List<ScreenShape>> ScreenShapes = new();
            public List<Vector2> ScreenMask = new();
            public event Action? OnFixedTick;
            
            public class ScreenShape
            {
                public float depth = -1;
                public Vector2 Ceiling = new();
                public Vector2 Top = new();
                public string TopTexture = "";
                public Vector2 Middle = new();
                public string MidTexture = "";
                public Vector2 Bottom = new();
                public string BotTexture = "";
                public Vector2 Floor = new();
                public float uv_x = 0.0f;
                public int slice;
                public ScreenShape() { }
                public void Draw(DrawingHandleScreen handle)
                {
                    //TODO: Calculate texture uv
                    DrawSlice(handle, slice, Top.X, Top.Y, uv_x, Color.DarkRed);
                    DrawSlice(handle, slice, Middle.X, Middle.Y, uv_x, Color.DarkGreen);
                    DrawSlice(handle, slice, Bottom.X, Bottom.Y, uv_x, Color.LightGray);
                    //TODO: I should convert this slices into horizontal spans (visplanes) to be able to use textures and clip by depth correctly
                    //that would require to scan the screen in rows, and use a HorizontalScreenShape or something, and recalculate it's depth too.
                    DrawSlice(handle, slice, Ceiling.X, Ceiling.Y, uv_x, Color.Yellow);
                    DrawSlice(handle, slice, Floor.X, Floor.Y, uv_x, Color.Blue);
                }
                private void DrawSlice(DrawingHandleScreen handle, int slice, float elevation, float column_height, float uv_x, Texture texture)
                {
                    var x = slice;
                    var left = x;
                    var right = x + 1;
                    var top = -elevation - column_height;
                    var bottom = -elevation;

                    var column = new UIBox2(left, top, right, bottom);
                    var uv = new UIBox2(uv_x, 0.0f, uv_x + 1 / 32.0f, 1.0f);

                    uv.Left = (texture.Size.X * uv.Left);
                    uv.Right = (texture.Size.X * uv.Right);
                    uv.Top = (texture.Size.Y * uv.Top);
                    uv.Bottom = (texture.Size.Y * uv.Bottom);

                    uv.Left = uv.Left % texture.Size.X;
                    uv.Right = uv.Right % texture.Size.X;
                    handle.DrawTextureRectRegion(texture, column, uv);
                    //handle.DrawLine(new Vector2(left, bottom), new Vector2(right, top),Color.Black);
                }
                private void DrawSlice(DrawingHandleScreen handle, int slice, float elevation, float column_height, float uv_x, Color color, bool filled = true)
                {
                    var x = slice;
                    var left = x;
                    var right = x + 1;
                    var top = -elevation - column_height;
                    var bottom = -elevation;

                    var column = new UIBox2(left, top, right, bottom);
                    var uv = new UIBox2(uv_x, 0.0f, uv_x + 1 / 32.0f, 1.0f);

                    handle.DrawRect(column, color, filled);
                }
                //private void DrawSlice(DrawingHandleScreen handle, int slice, float column_height, float uv_x)
                //{
                //    var texture = IoCManager.Resolve<IResourceCache>().GetTexture("/Textures/Structures/Walls/solid.rsi/full.png");
                //    DrawSlice(handle, slice, -column_height / 2.0f, column_height, uv_x, texture);
                //}
            };
            public class EntityShape
            {
                //move to a different class, this should be used only for rendering
                /*
                public Vector3 position;
                public Angle rotation;
                public Vector2 velocity;
                */
                public EntityUid entity;
                public float ScreenDepth=1.0f;
                public Vector2 ScreenPosition;
                public Vector2 ScreenScale;
                public Angle ScreenRotation;
                public NetEntity ServerId;
                public bool Visible = true;
                public EntityShape(EntityUid e, NetEntity net/*, Vector3 pos = new(), Vector2 vel=new()*/)
                {
                    ServerId = net;
                    entity = e;
                    //position = pos;
                    //velocity = vel;
                    var entityManager = IoCManager.Resolve<IEntityManager>();
                    var s = entityManager.GetComponent<SpriteComponent>(e);
                    //Workaround for jumpsuits using "StencilDraw" shader. (ClientClothingSystem.cs:365 @8359ada)
                    if (s.LayerMapTryGet("jumpsuit-0", out var layer)) s.LayerSetShader(layer, null, null);
                }
                public void Draw(DrawingHandleScreen handle)
                {
                    if (Visible)
                    {
                        handle.DrawEntity(entity, ScreenPosition, ScreenScale, ScreenRotation);
                        handle.DrawLine(ScreenPosition - new Vector2(5, 0), ScreenPosition + new Vector2(5, 0), Color.Red);
                    }
                }
            };
            //This is used to sort all the different shapes by depth, ScreenShapes shouldn't have any overdraw, but it's needed to clip the entity sprites
            //An alternative would be rendering the entities on a render target, then slice that and pushing them like any other ScreenShape.
            struct RenderItem
            {
                public ScreenShape? Wall;
                public EntityShape? Entity;

                public RenderItem(ScreenShape wall)
                {
                    Wall = wall;
                    Entity = null;
                }

                public RenderItem(EntityShape entity)
                {
                    Entity = entity;
                    Wall = null;
                }

                public void Draw(DrawingHandleScreen handle)
                {
                    if (Wall != null) Wall.Draw(handle);
                    else if (Entity != null) Entity.Draw(handle);
                }
            }

            public DebugGame()
            {
                inputVelocity = new Vector2();
                inputRotation = new Quaternion2D();
                position = new Vector2(25.0f, 25.0f);
                velocity = new Vector2(0.0f, 0.0f);
                rotation = new Quaternion2D(0.0f);
            }
            public void Input(Vector2 movement, float rot = 0.0f)
            {
                inputVelocity += movement * 120.0f;
                inputRotation = new Quaternion2D(inputRotation.Angle + float.DegreesToRadians(rot) * 25.0f);
            }
            public void Tick(float delta)
            {
                var vel = inputVelocity;
                if (inputVelocity.LengthSquared() > 0) vel = vel.Normalized() * 120.0f;

                velocity = Vector2.Lerp(velocity, Quaternion2D.RotateVector(rotation, vel), delta * 25.0f);
                rotation = rotation.Set(rotation.Angle + inputRotation.Angle * delta);

                if (currentSector != -1.0f)
                {
                    positionZ = float.Lerp(positionZ, -data.lSectors[currentSector].floor_height - 32.0f, delta * 2.0f);
                }
                eyeZ = positionZ + (float) Math.Cos(headbob) * 5.0f;

                if (velocity.LengthSquared() > 0.0f)
                {
                    var dir = -velocity.Normalized();
                    var results = IntersectMap(position, dir);
                    foreach (var r in results)
                    {
                        var l = data.lLines[r.LineIndex];
                        bool isPortal = (l.back_sidedef < data.lSides.Count);

                        if (r.FrontFacing && !isPortal)
                        {
                            // Collision threshold (adjustable)
                            float minDistance = 15.0f;

                            if (r.Distance < minDistance)
                            {
                                // Calculate 90° clockwise normal (front-facing) using corrected coordinate system
                                var A = data.lVertex[l.start] * new Vector2(1, -1);
                                var B = data.lVertex[l.end] * new Vector2(1, -1);
                                var dirVec = (B - A).Normalized();
                                var normal = new Vector2(-dirVec.Y, dirVec.X);

                                // Slide along the wall: remove normal component from velocity
                                velocity -= Vector2.Dot(velocity, normal) * normal;

                                // Soft pushback to avoid overlap without teleporting
                                position += normal * MathF.Max(0.01f, (minDistance - r.Distance)) * delta;
                            }
                        }
                    }
                }

                position += velocity * delta;
                velocity *= 0.95f;

                headbob += velocity.Length() / 350.0f;
                headbob %= (float) (Math.PI * 2);

                foreach (var e in Entities.Values)
                {
                    e.Tick(delta);
                }
            }

            private List<LineIntersection> IntersectMap(Vector2 pos, Vector2 dir)
            {
                //this should be optimized later by using the bsp nodes
                List<LineIntersection> results = new();
                for (var idx = 0; idx < data.lLines.Count; idx++)
                {
                    var it = new LineIntersection(data, idx, pos, dir);
                    if (it.LineIndex != -1) results.Add(it);
                }
                // Sort the lines by depth.
                //back to front
                //results.Sort((a, b) => b.Distance.CompareTo(a.Distance));
                //front to back (faster with Screen masking / zbuffer)
                results.Sort((a, b) => a.Distance.CompareTo(b.Distance));
                return results;
            }
            private Vector2 GetRotationVector(float offset)
            {
                var qrot = new Quaternion2D(rotation.Angle - float.DegreesToRadians(fov) / 2.0f + float.DegreesToRadians(fov) / slices * offset);
                var dir = Quaternion2D.RotateVector(qrot, -Vector2.UnitX);
                return dir;
            }
            private void DrawWalls(DrawingHandleScreen handle)
            {
                var entityManager = IoCManager.Resolve<IEntityManager>();
                // Naive rendering.
                ScreenMask.Clear();
                ScreenMask.EnsureCapacity(slices);
                ScreenShapes.Clear();
                ScreenShapes.EnsureCapacity(slices);
                
                currentSector = -1;
                for (int i = 0; i < slices; i++)
                {
                    ScreenMask.Add(new Vector2(-32,32));
                    ScreenShapes.Add(new());
                    var dir = GetRotationVector(i);
                    List<LineIntersection> results = IntersectMap(position, dir);
                    foreach (var r in results)
                    {
                        if ((ScreenMask[i].Y - ScreenMask[i].X) > 1.0f)
                        {
                            if (currentSector == -1)
                            {
                                var linedef = r.FrontFacing ? data.lLines[r.LineIndex].front_sidedef : data.lLines[r.LineIndex].back_sidedef;
                                if (linedef < data.lSides.Count) currentSector = data.lSides[linedef].sector;
                            }
                            DrawLineDef(handle, r, i);
                        }
                    }
                }
            }
            private struct LineIntersection
            {
                public int LineIndex { get; } = -1;
                public float Distance { get; } = -1;
                public float u { get; } = -1;
                public bool FrontFacing { get; } = false;
                public LineIntersection(int lineIndex, float distance, float u)
                {
                    LineIndex = lineIndex;
                    Distance = distance;
                    this.u = u;
                }
                public LineIntersection (GameData data, int lineIndex, Vector2 pos, Vector2 dir, float max_depth = 99999.0f)
                {
                    var l = data.lLines[lineIndex];
                    var A = data.lVertex[l.start] * new Vector2(1, -1);
                    var B = data.lVertex[l.end] * new Vector2(1, -1);

                    Vector2 AB = A - B, AP = pos - A;
                    float det = dir.X * AB.Y - dir.Y * AB.X;
                    if (Math.Abs(det) < 1e-6) return; // Parallel
                    float t = (AP.X * AB.Y - AP.Y * AB.X) / det;
                    float u = (AP.X * dir.Y - AP.Y * dir.X) / det;
                    var abPerp = new Vector2(AB.Y, -AB.X);
                    if (t >= 0 && u >= 0 && u <= 1)
                    {
                        if (t < max_depth && l.back_sidedef == 65535) max_depth = t;
                        if (t > max_depth) return; // Discard. There's an external wall in front of this one
                        this.LineIndex = lineIndex;
                        this.Distance = t;
                        this.u = u;
                        this.FrontFacing = Vector2.Dot(dir, abPerp) > 0;
                        return;
                    }
                }
            }
            private bool ClipColumn(ref float elevation, ref float column, int slice)
            {
                var top = elevation + column;
                var bottom = elevation;
                var clip = new Vector2(Math.Max(bottom, ScreenMask[slice].X), Math.Min(top, ScreenMask[slice].Y));
                elevation = clip.X;
                column = clip.Y - clip.X;
                return clip.X < clip.Y;
            }
            private void DrawLineDef(DrawingHandleScreen handle, LineIntersection r, int slice)
            {
                ScreenShape shape=new();
                shape.depth = r.Distance;

                var texture = IoCManager.Resolve<IResourceCache>().GetTexture("/Textures/Structures/Walls/solid.rsi/full.png");
                var alt_texture = IoCManager.Resolve<IResourceCache>().GetTexture("/Textures/Structures/Walls/grille.rsi/grille.png");

                var floor_texture = IoCManager.Resolve<IResourceCache>().GetTexture("/Textures/Tiles/arcadeblue2.png");
                var ceiling_texture = IoCManager.Resolve<IResourceCache>().GetTexture("/Textures/Tiles/plating_burnt.png");

                var black_texture = IoCManager.Resolve<IResourceCache>().GetTexture("/Textures/Interface/Nano/square_black.png"); 
                float rfov = float.DegreesToRadians(fov);
                float ray_angle = (slice / (float) slices) * rfov - (rfov / 2.0f);
                var l = data.lLines[r.LineIndex];
                //if (l.back_sidedef != 65535) return;
                var A = data.lVertex[l.start] * new Vector2(1, -1);
                var B = data.lVertex[l.end] * new Vector2(1, -1);
                var uv_x = ((B - A).Length() * r.u) / 32.0f;

                float wall_height = 1.0f;
                float screen_wall = 64.0f;
                float ref_dist = 32.0f;
                float projection_scale = screen_wall * ref_dist;
                var corrected_distance = r.Distance * (float) Math.Cos(ray_angle);
                var z_offset = eyeZ / screen_wall;
                var wall_base_z = 0.0f;
                var sideInfo = r.FrontFacing ? l.front_sidedef : l.back_sidedef;
                var sideInfoBack = !r.FrontFacing ? l.front_sidedef : l.back_sidedef;
                var portal = l.back_sidedef < data.lSides.Count && l.front_sidedef < data.lSides.Count;

                if (!portal && !r.FrontFacing) return;
                var portal_info = new Vector2();
                var portal_info_back = new Vector2();
                var side = data.lSides[sideInfo];
                var sector = data.lSectors[side.sector];

                wall_base_z = sector.floor_height / screen_wall;
                wall_height = (sector.ceiling_height - sector.floor_height) / screen_wall;

                if (portal)
                {
                    var sideBack = data.lSides[sideInfoBack];
                    var sectorBack = data.lSectors[sideBack.sector];

                    portal_info = new Vector2(sector.floor_height, sector.ceiling_height) / screen_wall;
                    portal_info_back = new Vector2(sectorBack.floor_height, sectorBack.ceiling_height) / screen_wall;
                }
                var currentMask = ScreenMask[slice];

                if (portal)
                {

                    //top
                    float pColumn, pElevation;
                    if (portal_info_back.Y < portal_info.Y)
                    {
                        pColumn = (portal_info.Y - portal_info_back.Y) * projection_scale / corrected_distance;
                        pElevation = ((portal_info_back.Y + z_offset) * projection_scale) / corrected_distance;
                    }
                    else
                    {
                        pColumn = 0;
                        pElevation = ((portal_info.Y + z_offset) * projection_scale) / corrected_distance;
                    }
                    if (ClipColumn(ref pElevation, ref pColumn, slice)) shape.Top = new Vector2(pElevation, pColumn);
                    var hiMask = Math.Min(ScreenMask[slice].Y, pElevation);
                    //bottom
                    if (portal_info_back.X > portal_info.X)
                    {
                        pColumn = (portal_info_back.X - portal_info.X) * projection_scale / corrected_distance;
                        pElevation = (portal_info.X + z_offset) * projection_scale / corrected_distance;
                    }
                    else
                    {
                        pColumn = 0;
                        pElevation = ((portal_info.X + z_offset) * projection_scale) / corrected_distance;
                    }
                    if (ClipColumn(ref pElevation, ref pColumn, slice)) shape.Bottom = new Vector2(pElevation, pColumn);
                    var loMask = Math.Max(ScreenMask[slice].X, pElevation + pColumn);

                    //portal leaves a space without drawing
                    currentMask = new Vector2(loMask, hiMask);
                }
                else
                {
                    if (r.FrontFacing)
                    {
                        float column = wall_height * projection_scale / corrected_distance;
                        float elevation = ((wall_base_z + z_offset) * projection_scale) / corrected_distance;
                        if (ClipColumn(ref elevation, ref column, slice)) shape.Middle = new Vector2(elevation, column);
                        currentMask = new Vector2(); //no more drawing here
                    }
                }
                //ceiling
                float c_elevation = ((wall_base_z + wall_height + z_offset) * projection_scale) / corrected_distance;
                float c_column = 64.0f - c_elevation;
                if (ClipColumn(ref c_elevation, ref c_column, slice)) shape.Ceiling = new Vector2(c_elevation, c_column);
                //floor
                float f_elevation = -32.0f;
                float f_column = ((wall_base_z + z_offset) * projection_scale) / corrected_distance - f_elevation;
                var color = currentSector == side.sector ? Color.LightGreen : Color.LightGray;
                if (ClipColumn(ref f_elevation, ref f_column, slice)) shape.Floor = new Vector2(f_elevation, f_column);
                shape.uv_x = uv_x;
                shape.slice = slice;
                ScreenShapes.Last().Add(shape);
                ScreenMask[slice] = currentMask;
            }
            public static int GetDepthIndex(float z, float zNear = 1.0f, float zFar = 20.0f, int numBuckets = 256)
            {
                float invZ = 1.0f / MathF.Max(z, 0.0001f);
                float invNear = 1.0f / zNear;
                float invFar = 1.0f / zFar;
                float norm = (invZ - invFar) / (invNear - invFar);
                norm = Math.Clamp(norm, 0.0f, 1.0f);
                return (int) (norm * (numBuckets - 1));
            }

            public void Draw(DrawingHandleScreen handle, Control ctrl)
            {
                slices = (int) (ctrl.Size.X/5.0f);
                fov = 90.0f;
                const float rad = 5;
                var fov1 = new Quaternion2D(rotation.Angle - float.DegreesToRadians(fov) / 2.0f);
                var fov2 = new Quaternion2D(rotation.Angle + float.DegreesToRadians(fov) / 2.0f);
                var offset = new Vector2(rad / 2.0f, rad / 2.0f);
                var l1 = Quaternion2D.RotateVector(fov1, Vector2.UnitX);
                var l2 = Quaternion2D.RotateVector(fov2, Vector2.UnitX);
                float rfov = float.DegreesToRadians(fov);
                float projection_plane_distance = (float)((ctrl.Size.Y / 2.0f) / Math.Tan(rfov / 2.0f));
                var sx = ctrl.Size.X / slices;
                var centerY = ctrl.Size.Y / 2.0f;

                var pop = handle.GetTransform();
                var texture = IoCManager.Resolve<IResourceCache>().GetTexture("/Textures/Structures/Walls/solid.rsi/full.png");

                //Transform the handle matrix, so the the X axis is [0,slices] and Y axis is [-32,32]
                Matrix3x2 T =
                    Matrix3Helpers.CreateTranslation(new Vector2(0, 32))*
                    Matrix3Helpers.CreateScale(ctrl.Size.X / (slices), ctrl.Size.Y / 64.0f) * ctrl.UIScale * pop;

                handle.SetTransform(T);

                //Debug to check if the matrix is working
                //Lines from each corner to the center
                //handle.DrawLine(
                //        new Vector2(0, -32),
                //        new Vector2(slices-1, 32),
                //        Color.DarkGray
                //    );
                //handle.DrawLine(
                //        new Vector2(slices-1, -32),
                //        new Vector2(0, 32),
                //        Color.DarkGray
                //    );
                //Draw a rect for the screen borders
                //handle.DrawRect(new UIBox2(2, -31, slices - 2, 31), Color.DarkCyan, false);

                DrawWalls(handle);

                //Sort all the shapes to be drawn, grouping them by depth
                Dictionary<int, List<RenderItem>> depth_buckets = new();
                foreach (var l in ScreenShapes)
                {
                    foreach (var r in l)
                    {
                        int idx = GetDepthIndex(r.depth, 1, 2000, 1024);
                        if (!depth_buckets.TryGetValue(idx, out var list))
                        {
                            list = new List<RenderItem>();
                            depth_buckets[idx] = list;
                        }
                        depth_buckets[idx].Add(new RenderItem(r));
                    }
                }
                //Calculate the shape for all entities
                foreach (var e in Entities.Values)
                {
                    //only if there's a shape for this entity
                    if (EntityShapes.ContainsKey(e.Id))
                    {
                        Vector2 e_pos = new Vector2(e.position.X, e.position.Y); //world coordinates
                        Vector2 e_rel_dir = e_pos - position; //world relative direction
                        float ray_angle = (float) Quaternion2D.InvRotateVector(rotation, e_rel_dir).ToAngle();
                        float e_slice = (ray_angle + (rfov / 2.0f)) / rfov * (float) slices;

                        var wall_height = 64.0f;
                        var fpos = new Vector2(e_slice, e.position.Z / (wall_height*0.5f));
                        var corrected_distance = (e_rel_dir.Length() * (float) Math.Cos(ray_angle));
                        var projection_scale = (32.0f * 32.0f);
                        
                        var z_offset = eyeZ / wall_height;
                        var corrected = new Vector2(fpos.X, ((fpos.Y - z_offset) * projection_scale) / corrected_distance);
                        var scale = Vector2.One * projection_scale / corrected_distance;
                        var pos = corrected * new Vector2(ctrl.Size.X / slices, ctrl.Size.Y / 64.0f) * ctrl.UIScale;

                        EntityShapes[e.Id].ScreenDepth = corrected_distance;
                        EntityShapes[e.Id].ScreenPosition = pos;
                        EntityShapes[e.Id].ScreenScale = scale;
                        EntityShapes[e.Id].ScreenRotation =(-e_rel_dir).ToAngle() - e.Angle;
                    }

                    
                    //handle.DrawEntity(e.entity, pos, scale, e_rel_dir.ToAngle());
                }
                //Add them to the depth buckets
                foreach (var e in EntityShapes.Values)
                {
                    int idx = GetDepthIndex(e.ScreenDepth, 1, 2000, 1024);
                    if (!depth_buckets.TryGetValue(idx, out var list))
                    {
                        list = new List<RenderItem>();
                        depth_buckets[idx] = list;
                    }
                    depth_buckets[idx].Add(new RenderItem(e));
                }

                //Draw all items
                //I'm starting to wonder if it would be easier to do the effort of drawing all of this in 3D using a shader or something.
                //Maybe packing everything in primitives instead of lists and lists of rects and sprites for performance.
                foreach (var bucket in depth_buckets.OrderBy(kvp => kvp.Key))
                {
                    foreach (var item in bucket.Value)
                    {
                        item.Draw(handle);
                    }
                }

                //Restore the original matrix, and make the center of the window (0,0). Pixel coordinates
                T = Matrix3Helpers.CreateTranslation(ctrl.Size / 2.0f - position) * ctrl.UIScale * pop;
                handle.SetTransform(T);
                data.DrawFlat(handle);
                handle.DrawCircle(position - offset, rad, Color.GreenYellow, true);
                handle.DrawCircle(position - offset, rad, Color.Red, false);
                handle.DrawDottedLine(position, position + l1 * 50.0f, Color.AliceBlue);
                handle.DrawDottedLine(position, position + l2 * 50.0f, Color.AliceBlue);
                foreach (var b in boxes)
                {
                    handle.DrawRect(b, Color.White, false);
                }
                foreach (var e in Entities.Values)
                {
                    handle.DrawCircle(new Vector2(e.position.X,e.position.Y) - offset * 2, rad * 2, Color.DarkRed, true);
                }
            }
        }
        public EntityUid? Owner;
        public float AccDelta=0.0f;
        public DebugGame dgame;
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
            dgame = new DebugGame();
        }
        protected override void Draw(DrawingHandleScreen handle)
        {
            base.Draw(handle);
            if (Parent != null && Win!=null)
            {
                //handle.SetTransform(Matrix3Helpers.CreateTranslation(GlobalPosition + Size / 2.0f - dgame.position));
                dgame.Draw(handle, Win);
            }
            
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
                            var data = new DebugGame.EntityShape(id, e.Id);
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
