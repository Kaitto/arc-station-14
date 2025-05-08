using Content.Client.Resources;
using OpenToolkit.GraphicsLibraryFramework;
using Robust.Client.GameObjects;
using Robust.Client.Graphics;
using Robust.Client.ResourceManagement;
using Robust.Client.UserInterface;
using Robust.Shared.Physics;
using System;
using System.Data.Common;
using System.Drawing;
using System.Linq;
using System.Numerics;
using System.Reflection.Metadata;
using static Content.Client.Arcade.FPS.GameData;
using static Content.Shared.Arcade.SharedCustomGameArcadeComponent;
using Color = Robust.Shared.Maths.Color;
using Vector4 = System.Numerics.Vector4;

namespace Content.Client.Arcade.FPS
{
    public class CustomGameDebug
    {
        //Display
        public Vector2 ScreenSize;
        public float UIScale;
        private Matrix3x2 WindowTransform;

        //Player/Arcade
        public EntityUid? Owner;
        public Vector2 inputVelocity;
        public Quaternion2D inputRotation;
        public Vector2 position;
        public float positionZ = 0.0f;
        public float eyeZ = 0;
        public Vector2 velocity;
        public Quaternion2D rotation;
        public int currentSector = -1;
        private float headbob = 0.0f;

        //Game state
        public GameData data = new();
        public Dictionary<NetEntity, EntityData> Entities = new();

        //rendering data
        public float fov = 60;
        public int slices = 64;
        public Dictionary<NetEntity, EntityShape> EntityShapes = new();
        public List<List<ScreenShape>> ScreenShapes = new();
        public List<Vector2> ScreenMask = new();

        public event Action? OnFixedTick;
        public float XScreen(Vector2 rel_position, out float inverse)
        {
            float rfov = float.DegreesToRadians(fov);
            var ray_angle = (float) Quaternion2D.InvRotateVector(rotation, rel_position).ToAngle();
            inverse = 1.0f / MathF.Max(rel_position.Length() * MathF.Cos(ray_angle), 0.0001f);
            float e_slice = (ray_angle + (rfov / 2.0f)) / rfov * (float) slices;
            return e_slice;
        }
        public float YScreen(float rel_height, in float inverse_corrected_distance)
        {
            float ref_dist = 32.0f + 30.0f;
            float projection_scale = ref_dist;
            return (rel_height) * projection_scale * inverse_corrected_distance;
        }

        public class ScreenShape
        {
            public float idepth = -1;
            public Vector4 Ceiling = new();
            public Vector4 Top = new();
            public string TopTexture = "";
            public Vector4 Middle = new();
            public string MidTexture = "";
            public Vector4 Bottom = new();
            public string BotTexture = "";
            public Vector4 Floor = new();
            public float uv_x = 0.0f;
            public Vector2 uv_w = new();
            public int slice;
            public int sector;
            public ScreenShape() { }
            public void Draw(DrawingHandleScreen handle)
            {
                var txMan = CustomGameDataTextureManager.Instance;
                if (TopTexture == "") DrawSlice(handle, slice, Top.X, Top.Y, uv_x, Color.DarkRed);
                if (MidTexture == "") DrawSlice(handle, slice, Middle.X, Middle.Y, uv_x, Color.DarkGreen);
                if (BotTexture == "") DrawSlice(handle, slice, Bottom.X, Bottom.Y, uv_x, Color.LightGray);

                if (TopTexture != "" && TopTexture != "-") DrawSlice(handle, slice, Top.X, Top.Y, uv_x, txMan.GetTexture(TopTexture), uv_w.X, Top.Z, Top.W);
                if (MidTexture != "" && MidTexture != "-") DrawSlice(handle, slice, Middle.X, Middle.Y, uv_x, txMan.GetTexture(MidTexture), uv_w.X, Middle.Z, Middle.W);
                if (BotTexture != "" && BotTexture != "-") DrawSlice(handle, slice, Bottom.X, Bottom.Y, uv_x, txMan.GetTexture(BotTexture), uv_w.X, Bottom.Z, Bottom.W);

                //handle.HsvToRgb(360 * (sector / 128.0f), 1.0, 1.0, out var r, out var g, out var b);
                //DrawSlice(handle, slice, Ceiling.X, Ceiling.Y, uv_x, Color.FromArgb(255,r,g,b));
                //DrawSlice(handle, slice, Floor.X, Floor.Y, uv_x, Color.FromArgb(255,r, g, b));
            }
            private void DrawSlice(DrawingHandleScreen handle, int slice, float elevation, float column_height, float uv_x, Texture texture, float uv_w = 1 / 32.0f, float uv_oh = 0.0f, float uv_h = 32.0f)
            {
                var x = slice;
                var left = x;
                var right = x + 1;
                var top = -elevation - column_height;
                var bottom = -elevation;

                var column = new UIBox2(left, top, right, bottom);
                var voffset = 1 - (uv_h - uv_oh) / texture.Size.Y;
                var uv = new UIBox2(uv_x, 0.0f + voffset, uv_x + uv_w, uv_h / texture.Size.Y + voffset);

                uv.Left = (texture.Size.X * uv.Left);
                uv.Right = (texture.Size.X * uv.Right);
                uv.Top = (texture.Size.Y * uv.Top);
                uv.Bottom = (texture.Size.Y * uv.Bottom);

                uv.Left = uv.Left % texture.Size.X;
                uv.Right = uv.Right % texture.Size.X;
                float fadeStart = 64f;
                float fadeEnd = 1800f;
                float t = Clamp((1.0f / idepth - fadeStart) / (fadeEnd - fadeStart), 0f, 0.65f);
                byte shade = (byte) (255 * (1.0f - t)); // más lejos = más oscuro
                var c = new Color(shade, shade, shade);

                handle.DrawTextureRectRegion(texture, column, uv, c);
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
            public float iScreenDepth = 1.0f;
            public Vector2 ScreenPosition;
            public Vector2 ScreenSize;
            public Vector2 ScreenPositionFix;
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
                    handle.DrawEntity(entity, ScreenPositionFix, ScreenScale, ScreenRotation);
                    handle.DrawRect(new UIBox2(ScreenPosition.X - ScreenSize.X / 2.0f, ScreenPosition.Y - ScreenSize.Y, ScreenPosition.X + ScreenSize.X / 2.0f, ScreenPosition.Y), Color.Black, false);
                    // handle.DrawLine( ScreenPosition - new Vector2(5, 0) + new Vector2(0, -2),
                    //                  ScreenPosition + new Vector2(5, 0) + new Vector2(0, -2), Color.Red);
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

        public CustomGameDebug(Vector2 screensize, float uiscale)
        {
            UIScale = uiscale;
            ScreenSize = screensize;
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
                positionZ = float.Lerp(positionZ, -data.lSectors[currentSector].floor_height, delta * 16.0f);
            }

            eyeZ = positionZ - 41.0f + MathF.Cos(headbob) * 5.0f;

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
            headbob %= (MathF.PI * 2);

            foreach (var e in Entities.Values)
            {
                e.Tick(delta);

                var results = IntersectMap(new Vector2(e.position.X, e.position.Y), Vector2.UnitX);
                if (results.Count > 0)
                {
                    var r = results[0];
                    var l = data.lLines[r.LineIndex];
                    var ls = data.lSides[l.front_sidedef];
                    var sector = data.lSectors[ls.sector];
                    e.position.Z = float.Lerp(e.position.Z, -sector.floor_height, delta * 16.0f);
                }
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
                ScreenMask.Add(new Vector2(-32, 32));
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
            public LineIntersection(GameData data, int lineIndex, Vector2 pos, Vector2 dir, float max_depth = 99999.0f)
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
        private Vector4 BuildSpan(float nearValue, float farValue, ref float iDist, int slice, out float column, out float elevation, bool invertCheck = false)
        {
            Vector4 ret = new();
            bool condition = invertCheck ? nearValue > farValue : nearValue < farValue;

            if (condition)
            {
                column = YScreen(Math.Abs(farValue - nearValue), iDist);
                elevation = YScreen(Math.Min(nearValue, farValue) + eyeZ, iDist);
            }
            else
            {
                column = 0;
                elevation = YScreen(farValue + eyeZ, iDist);
            }

            // Guardar valores originales antes de clip
            float oldElevation = elevation;
            float oldColumn = column;

            if (ClipColumn(ref elevation, ref column, slice))
            {
                // Cálculo de altura en unidades de mundo
                float worldHeight = Math.Abs(farValue - nearValue);

                // Compensación por clipping
                float correctedHeight = worldHeight * (column / oldColumn);
                float verticalOffset = worldHeight * ((oldElevation - elevation) / oldColumn);

                ret = new Vector4(elevation, column, verticalOffset, correctedHeight);
            }

            return ret;
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
            ScreenShape shape = new();

            //var texture = IoCManager.Resolve<IResourceCache>().GetTexture("/Textures/Structures/Walls/solid.rsi/full.png");
            //var alt_texture = IoCManager.Resolve<IResourceCache>().GetTexture("/Textures/Structures/Walls/grille.rsi/grille.png");

            //var floor_texture = IoCManager.Resolve<IResourceCache>().GetTexture("/Textures/Tiles/arcadeblue2.png");
            //var ceiling_texture = IoCManager.Resolve<IResourceCache>().GetTexture("/Textures/Tiles/plating_burnt.png");

            //var black_texture = IoCManager.Resolve<IResourceCache>().GetTexture("/Textures/Interface/Nano/square_black.png");
            float rfov = float.DegreesToRadians(fov);
            float ray_angle = (slice / (float) slices) * rfov - (rfov / 2.0f);
            var l = data.lLines[r.LineIndex];
            //if (l.back_sidedef != 65535) return;
            var A = data.lVertex[l.start] * new Vector2(1, -1);
            var B = data.lVertex[l.end] * new Vector2(1, -1);
            var uv_x = ((B - A).Length() * r.u) / 64.0f;

            //float screen_wall = 64.0f;
            //float ref_dist = 32.0f + 30.0f;
            //float projection_scale = screen_wall * ref_dist;
            var iDist = 1.0f / MathF.Max(r.Distance * MathF.Cos(ray_angle), 0.0001f);
            shape.idepth = iDist;
            //var z_offset = eyeZ / screen_wall;
            //var wall_base_z = 0.0f;
            var sideInfo = r.FrontFacing ? l.front_sidedef : l.back_sidedef;
            var sideInfoBack = !r.FrontFacing ? l.front_sidedef : l.back_sidedef;
            var portal = l.back_sidedef < data.lSides.Count && l.front_sidedef < data.lSides.Count;

            if (!portal && !r.FrontFacing) return;
            var portal_info = new Vector2();
            var portal_info_back = new Vector2();
            var side = data.lSides[sideInfo];
            var sector = data.lSectors[side.sector];

            float wall_base_z = sector.floor_height;
            float wall_height = (sector.ceiling_height - sector.floor_height);

            if (portal)
            {
                var sideBack = data.lSides[sideInfoBack];
                var sectorBack = data.lSectors[sideBack.sector];

                portal_info = new Vector2(sector.floor_height, sector.ceiling_height);
                portal_info_back = new Vector2(sectorBack.floor_height, sectorBack.ceiling_height);
            }
            var currentMask = ScreenMask[slice];

            if (portal)
            {
                shape.Top = BuildSpan(portal_info_back.Y, portal_info.Y, ref iDist, slice, out var pColumn, out var pElevation);
                var hiMask = Math.Min(ScreenMask[slice].Y, pElevation);
                shape.Bottom = BuildSpan(portal_info_back.X, portal_info.X, ref iDist, slice, out pColumn, out pElevation, true);
                var loMask = Math.Max(ScreenMask[slice].X, pElevation + pColumn);

                //portal leaves a space without drawing
                currentMask = new Vector2(loMask, hiMask);
            }
            else
            {
                if (r.FrontFacing)
                {
                    shape.Middle = BuildSpan(sector.floor_height, sector.ceiling_height, ref iDist, slice, out var _, out var _);
                    currentMask = new Vector2(); //no more drawing here
                }
            }
            //ceiling
            float c_elevation = YScreen(wall_base_z + wall_height + eyeZ, iDist);
            float c_column = 64.0f - c_elevation;
            if (ClipColumn(ref c_elevation, ref c_column, slice)) shape.Ceiling = new Vector4(c_elevation, c_column, wall_base_z + wall_height, 0.0f);
            //shape.Ceiling = BuildSpan(sector.ceiling_height, 128, ref iDist, slice, out var _, out var _);
            //floor
            float f_elevation = -32.0f;
            float f_column = YScreen(wall_base_z + eyeZ, iDist) - f_elevation;
            if (ClipColumn(ref f_elevation, ref f_column, slice)) shape.Floor = new Vector4(f_elevation, f_column, wall_base_z, 0.0f);
            //shape.Floor = BuildSpan(-128, sector.floor_height, ref iDist, slice, out var _, out var _);

            /*
             shape.[section] = [clippedElevation, clippedColumn, originalElevation, originalColumn]
             */

            //Ensure textures are being loaded in cache (from wad instead of a file)
            //shape.TopTexture = side.upper_texture;
            //shape.MidTexture = side.middle_texture;
            //shape.BotTexture = side.lower_texture;
            shape.TopTexture = data.lSides[sideInfo].upper_texture;
            shape.MidTexture = data.lSides[sideInfo].middle_texture;
            shape.BotTexture = data.lSides[sideInfo].lower_texture;
            shape.sector = side.sector;
            data.GetTexture(shape.TopTexture);
            data.GetTexture(shape.MidTexture);
            data.GetTexture(shape.BotTexture);
            //flat format not supported yet
            data.GetFlatTexture(sector.ceiling_texture);
            data.GetFlatTexture(sector.floor_texture);
            //shape.TopTexture = "full.png";
            //shape.MidTexture = "full.png";
            //shape.BotTexture = "full.png";

            shape.uv_x = uv_x; //horizontal shift for UV texture
            shape.uv_w = new Vector2(ComputeUvW(iDist), wall_height); //Horizontal width for uv Texture, world height

            shape.slice = slice;
            ScreenShapes.Last().Add(shape);
            ScreenMask[slice] = currentMask;
        }
        private float ComputeUvW(float iDist)
        {
            float fov_rad = MathF.PI * fov / 180.0f; // pasa FOV a radianes
            float half_fov_tan = MathF.Tan(fov_rad / 2.0f); // tan(FOV/2)
            float slice_world_width = (2.0f * iDist * half_fov_tan) / slices; // cuánto mide cada slice
            return slice_world_width / 64.0f; // avance de UV por slice
        }
        public static int GetDepthIndex(float invZ, float zNear = 1.0f, float zFar = 20.0f, int numBuckets = 256)
        {
            //float invZ = 1.0f / MathF.Max(z, 0.0001f);
            float invNear = 1.0f / zNear;
            float invFar = 1.0f / zFar;
            float norm = (invZ - invFar) / (invNear - invFar);
            norm = Math.Clamp(norm, 0.0f, 1.0f);
            return (int) (norm * (numBuckets - 1));
        }
        public Matrix3x2 GetScreenTransform()
        {
            //player collision box is 32x32 units
            //player eye is at 41 units
            //player can see a wall completely 100 units ahead, (-20 to 102. standing on 0)
            //at z0, would be -61,61
            //player (sprite) is about 57 units tall


            return Matrix3Helpers.CreateTranslation(new Vector2(0, 32)) * Matrix3Helpers.CreateScale(ScreenSize.X / (slices), ScreenSize.Y / 64.0f) * UIScale * WindowTransform;
        }
        public Vector2 ScreenToWorldRelative(float x_screen, float y_screen, float rel_height)
        {
            float rfov = float.DegreesToRadians(fov);
            float projection_scale = 62.0f; // 32 + 30
            float ray_angle = ((x_screen / slices) * rfov) - (rfov / 2.0f);

            // Invertir YScreen
            float inverse_corrected_distance = y_screen / (rel_height * projection_scale);
            float corrected_distance = 1.0f / MathF.Max(inverse_corrected_distance * MathF.Cos(ray_angle), 0.0001f);

            // Dirección del rayo en espacio de mundo
            Vector2 direction = Quaternion2D.RotateVector(new Quaternion2D(ray_angle), Vector2.UnitX);

            // Posición relativa en el mundo
            return direction * corrected_distance;
        }

        // Nuevo algoritmo para generar spans horizontales desde ScreenShapes
        public class HorizontalShape
        {
            public float Top;
            public float Bottom;
            public float Z;
            public float Height;
            public float Depth;
            public int SliceStart;
            public int SliceEnd;
            public string Texture ="";
            public bool IsCeiling;
        }

        public List<HorizontalShape> CalculateHorizontalShapes(List<List<ScreenShape>> sliceShapes, int horizontalDivisions)
        {
            float screenTop = -32f;
            float screenBottom = 32f;
            float screenHeight = screenBottom - screenTop;
            float spanHeight = screenHeight / horizontalDivisions;

            var scanlines = new List<float>();
            for (int i = 0; i <= horizontalDivisions; i++)
            {
                scanlines.Add(screenTop + i * spanHeight);
            }

            var shapeGrid = new Dictionary<int, List<(int slice, ScreenShape shape, bool isCeiling)>>();

            for (int slice = 0; slice < sliceShapes.Count; slice++)
            {
                foreach (var shape in sliceShapes[slice])
                {
                    // Floor
                    float floorTop = shape.Floor.X;
                    float floorBottom = floorTop + shape.Floor.Y;

                    for (int i = 0; i < scanlines.Count - 1; i++)
                    {
                        float yTop = scanlines[i];
                        float yBottom = scanlines[i + 1];

                        if (floorBottom > yTop && floorTop < yBottom)
                        {
                            if (!shapeGrid.TryGetValue(i, out var list))
                                shapeGrid[i] = list = new List<(int, ScreenShape, bool)>();
                            list.Add((slice, shape, false));
                        }
                    }

                    // Ceiling
                    float ceilTop = shape.Ceiling.X;
                    float ceilBottom = ceilTop + shape.Ceiling.Y;

                    for (int i = 0; i < scanlines.Count - 1; i++)
                    {
                        float yTop = scanlines[i];
                        float yBottom = scanlines[i + 1];

                        if (ceilTop < yBottom && ceilBottom > yTop)
                        {
                            if (!shapeGrid.TryGetValue(i, out var list))
                                shapeGrid[i] = list = new List<(int, ScreenShape, bool)>();
                            list.Add((slice, shape, true));
                        }
                    }
                }
            }

            var spans = new List<HorizontalShape>();
            foreach (var kv in shapeGrid)
            {
                int rowIndex = kv.Key;
                var items = kv.Value;
                items.Sort((a, b) => a.slice.CompareTo(b.slice));

                (int sliceStart, ScreenShape prevShape, bool isCeiling) = items[0];
                int sliceEnd = sliceStart;

                var prev_sector = data.lSectors[prevShape.sector];

                for (int idx = 1; idx < items.Count; idx++)
                {
                    var (slice, shape, isCeil) = items[idx];
                    var sector = data.lSectors[shape.sector];
                    bool sameProps = isCeil == isCeiling
                        && (isCeil
                            ? (shape.Ceiling.Z == prevShape.Ceiling.Z && shape.Ceiling.W == prevShape.Ceiling.W && prev_sector.ceiling_texture == sector.ceiling_texture && prev_sector.ceiling_height == sector.ceiling_height)
                            : (shape.Floor.Z == prevShape.Floor.Z && shape.Floor.W == prevShape.Floor.W && prev_sector.floor_texture == sector.floor_texture && prev_sector.floor_height == sector.floor_height));

                    if (slice == sliceEnd + 1 && sameProps)
                    {
                        sliceEnd = slice;
                    }
                    else
                    {
                        spans.Add(new HorizontalShape
                        {
                            Top = scanlines[rowIndex],
                            Bottom = scanlines[rowIndex + 1],
                            Z = isCeiling ? prevShape.Ceiling.Z : prevShape.Floor.Z,
                            Height = isCeiling ? prevShape.Ceiling.W : prevShape.Floor.W,
                            Depth = prevShape.idepth,
                            SliceStart = sliceStart,
                            SliceEnd = sliceEnd + 1,
                            Texture = isCeiling ? prev_sector.ceiling_texture : prev_sector.floor_texture,
                            IsCeiling = isCeiling
                        });
                        (sliceStart, prevShape, isCeiling) = (slice, shape, isCeil);
                        prev_sector = data.lSectors[shape.sector];
                        sliceEnd = slice;
                    }
                }

                // último span de la fila
                spans.Add(new HorizontalShape
                {
                    Top = scanlines[rowIndex],
                    Bottom = scanlines[rowIndex + 1],
                    Z = isCeiling ? prevShape.Ceiling.Z : prevShape.Floor.Z,
                    Height = isCeiling ? prevShape.Ceiling.W : prevShape.Floor.W,
                    Depth = prevShape.idepth,
                    SliceStart = sliceStart,
                    SliceEnd = sliceEnd + 1,
                    Texture = isCeiling ? prev_sector.ceiling_texture : prev_sector.floor_texture,
                    IsCeiling = isCeiling
                });
            }

            return spans;
        }

        public void DrawHorizontalShapes(List<HorizontalShape> spans, DrawingHandleScreen handle)
        {
            foreach (var span in spans)
            {
                var left = span.SliceStart;
                var right = span.SliceEnd;
                var top = -span.Bottom;
                var bottom = -span.Top;
                var z = span.Z;
                if (span.Texture == "-") continue;
                var texture = CustomGameDataTextureManager.Instance.GetTexture(span.Texture);//IoCManager.Resolve<IResourceCache>().GetTexture("/Textures/Structures/Walls/solid.rsi/full.png"/*span.Texture*/);
                float tileSize = 32.0f;

                // 🟦 Coordenadas de pantalla
                var screenVerts = new Vector2[4]
                {
                    new Vector2(left, bottom),  // Bottom-Left
                    new Vector2(right, bottom),// Bottom-Right
                    new Vector2(right, top),   // Top-Right
                    new Vector2(left, top),    // Top-Left
                };

                // 🟧 Coordenadas de mundo proyectadas
                var worldVerts = new Vector2[4];
                for (int i = 0; i < 4; i++)
                    worldVerts[i] = ScreenToWorldAbsolute(screenVerts[i], z) / tileSize;

                var colorVerts = new Color[4];
                for (int i = 0; i < 4; i++)
                {
                    var worldPos = ScreenToWorldAbsolute(screenVerts[i], span.Z);
                    var distance = (worldPos - position).Length(); // ← tu posición en mundo

                    // Puedes ajustar estos valores para controlar el degradado
                    float fadeStart = 64f;
                    float fadeEnd = 800f;

                    // Calcular factor [0, 1]
                    float t = Clamp((distance - fadeStart) / (fadeEnd - fadeStart), 0f, 0.65f);
                    byte shade = (byte) (255 * (1.0f - t)); // más lejos = más oscuro
                    colorVerts[i] = new Color(shade, shade, shade);
                }

                //TODO: Implement Quad rendering
                //No textured primitives? oh no
                handle.DrawRect(new UIBox2(), span.IsCeiling ? Color.Red : Color.Blue);
                //Custom implementation in Raylib.
                // Dibujar quad con UVs por vértice
                //handle.DrawTexturedQuad(texture, screenVerts, worldVerts, colorVerts);
            }
        }
        public static float Clamp(float value, float min, float max)
        {
            if (value < min) return min;
            if (value > max) return max;
            return value;
        }
        public void Draw(DrawingHandleScreen handle)
        {
            WindowTransform = handle.GetTransform();

            slices = (int) (ScreenSize.X / 5.0f);
            fov = 75.0f * ScreenSize.X / ScreenSize.Y;

            const float rad = 5;
            var fov1 = new Quaternion2D(rotation.Angle - float.DegreesToRadians(fov) / 2.0f);
            var fov2 = new Quaternion2D(rotation.Angle + float.DegreesToRadians(fov) / 2.0f);
            var offset = new Vector2(rad / 2.0f, rad / 2.0f);
            var l1 = Quaternion2D.RotateVector(fov1, Vector2.UnitX);
            var l2 = Quaternion2D.RotateVector(fov2, Vector2.UnitX);
            float rfov = float.DegreesToRadians(fov);
            float projection_plane_distance = ((ScreenSize.Y / 2.0f) / MathF.Tan(rfov / 2.0f));
            var sx = ScreenSize.X / slices;
            var centerY = ScreenSize.Y / 2.0f;

            var pop = handle.GetTransform();
            //var texture = IoCManager.Resolve<IResourceCache>().GetTexture("/Textures/Structures/Walls/solid.rsi/full.png");

            handle.SetTransform(GetScreenTransform());

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
                    int idx = GetDepthIndex(r.idepth, 1, 2000, 1024);
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
                    var fpos = new Vector2(XScreen(e_rel_dir, out var iDist), e.position.Z);

                    var corrected = new Vector2(fpos.X, YScreen(fpos.Y + eyeZ, iDist)/* (fpos.Y + z_offset) * projection_scale / corrected_distance*/);
                    var scale = Vector2.One * YScreen(1.0f, iDist)/*projection_scale / corrected_distance*/;
                    EntityShapes[e.Id].ScreenPosition = corrected * new Vector2(1, -1.0f);
                    EntityShapes[e.Id].ScreenSize = new Vector2(64.0f, 57.0f) * YScreen(1.0f, iDist);
                    var pos = corrected * new Vector2(ScreenSize.X / slices, ScreenSize.Y / 64.0f) * UIScale;

                    EntityShapes[e.Id].iScreenDepth = iDist;
                    EntityShapes[e.Id].ScreenPositionFix = pos;
                    EntityShapes[e.Id].ScreenScale = scale;
                    EntityShapes[e.Id].ScreenRotation = (-e_rel_dir).ToAngle() - e.Angle;
                }


                //handle.DrawEntity(e.entity, pos, scale, e_rel_dir.ToAngle());
            }
            //Add them to the depth buckets
            foreach (var e in EntityShapes.Values)
            {
                int idx = GetDepthIndex(e.iScreenDepth, 1, 2000, 1024);
                if (!depth_buckets.TryGetValue(idx, out var list))
                {
                    list = new List<RenderItem>();
                    depth_buckets[idx] = list;
                }
                depth_buckets[idx].Add(new RenderItem(e));
            }

            //experimental horizontal spans
            var hspans = CalculateHorizontalShapes(ScreenShapes, 128);
            DrawHorizontalShapes(hspans, handle);
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
            var T = Matrix3Helpers.CreateTranslation((ScreenSize / 2.0f - position)) * UIScale * pop;
            handle.SetTransform(T);
            data.DrawFlat(handle);
            handle.DrawCircle(position, rad, Color.GreenYellow, true);
            handle.DrawCircle(position, rad, Color.Red, false);
            handle.DrawDottedLine(position, position + l1 * 50.0f, Color.AliceBlue);
            handle.DrawDottedLine(position, position + l2 * 50.0f, Color.AliceBlue);
            foreach (var e in Entities.Values)
            {
                handle.DrawCircle(new Vector2(e.position.X, e.position.Y) - offset * 2, rad * 2, Color.DarkRed, true);
            }

            foreach (var span in hspans)
            {
                if (span.IsCeiling) continue;
                var left = span.SliceStart;
                var right = span.SliceEnd;
                var top = -span.Bottom;
                var bottom = -span.Top;
                var rect = new UIBox2(left, top, right, bottom);

                var bl = ScreenToWorldAbsolute(new Vector2(rect.Left, rect.Top), span.Z);
                var tr = ScreenToWorldAbsolute(new Vector2(rect.Right, rect.Top), span.Z);
                handle.DrawLine(bl, tr, Color.DarkRed);
            }

            //handle.SetTransform(pop);

            Matrix3x2.Invert(GetScreenTransform(), out var iT);
            //Vector2 aim = Vector2.Transform(Raylib_cs.Raylib.GetMousePosition(), iT);
            Vector2 aim = new Vector2(); //TODO: Get mouse position
            if (currentSector != -1)
            {
                var Z = -data.lSectors[currentSector].floor_height - eyeZ;
                aim = ScreenToWorldRelative(aim.X, aim.Y, Z);
                aim = Quaternion2D.RotateVector(rotation, aim);
                float dist;
                var x = XScreen(aim, out dist);
                var y = YScreen(Z, dist);
                handle.SetTransform(GetScreenTransform());
                handle.DrawCircle(new Vector2(x, y), 15.0f, Color.Green, false);
                handle.SetTransform(T);
                handle.DrawCircle(position + aim, 5, Color.Red);
                handle.DrawLine(position, position + aim, Color.Red);
            }

            handle.SetTransform(pop);
        }
        private Vector2 ScreenToWorldAbsolute(Vector2 pos, float elevation)
        {
            var Z = -elevation - eyeZ;
            pos = ScreenToWorldRelative(pos.X, pos.Y, Z);
            pos = Quaternion2D.RotateVector(rotation, pos);
            return pos + position;
        }
    }
}
