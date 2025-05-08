
using Robust.Client.Graphics;
using Robust.Client.UserInterface;
using Robust.Shared.Physics;
using System.Drawing;
using System.Numerics;
using Color = Robust.Shared.Maths.Color;

namespace Content.Client.Arcade.FPS
{
    public partial struct GameData
    {
        public struct LineDef
        {
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
        public struct SideDef
        {
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
        public struct Sector
        {
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
        public struct SegDef
        {
            public int start;
            public int end;
            public float angle;
            public int linedef_idx;
            //public bool direction;
            //public float offset;
            public SegDef(int s, int e, float a, int l)
            {
                start = s;
                end = e;
                angle = a;
                linedef_idx = l;
            }
        }
        public struct SubSectors
        {
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

        public GameData() { }
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
                }
                ;

                var d = (b - a);
                var c = a + d / 2.0f;
                var r = new Quaternion2D(d.ToAngle());
                var f = Quaternion2D.RotateVector(r, Vector2.UnitY) * 15.0f;
                handle.DrawLine(c, c + f, Color.AntiqueWhite);
            }
        }
    }
}
