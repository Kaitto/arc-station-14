using Robust.Client.Graphics;
using Robust.Client.ResourceManagement;
using SixLabors.ImageSharp.PixelFormats;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Text;
using Color = Robust.Shared.Maths.Color;

namespace Content.Client.Arcade.FPS
{
    public partial struct GameData
    {
        public class WADHeader
        {
            public string MagicNumber { get; set; } = "";
            public uint NumLumps { get; set; }
            public uint DirOffset { get; set; }
        }

        public class WADLump
        {
            public string Name { get; set; } = "";
            public uint Offset { get; set; }
            public int Size { get; set; }
        }
        public class WADMapPatch
        {
            public int OriginX { get; set; }
            public int OriginY { get; set; }
            public int Patch { get; set; } //PNAMES index
            public int StepDir{ get; set; } //unused
            public int Colormap { get; set; } //unused
        }
        public class WADMapTexture
        {
            public string Name { get; set; } = "";
            public bool Masked { get; set; }
            public int Width { get; set; }
            public int Height { get; set; }
            public int ColumnDirectory { get; set; } //unused
            public int PatchCount { get; set; }
            public List<WADMapPatch> Patches { get; set; } = new();

            public static WADMapTexture FromLump(byte[] lump, int offset)
            {
                var map=new WADMapTexture
                {
                    Name = GetCString(lump, offset + 0, 8),
                    Masked = BitConverter.ToInt32(lump, offset + 8) == 1,
                    Width = BitConverter.ToInt16(lump, offset + 12),
                    Height = BitConverter.ToInt16(lump, offset + 14),
                    ColumnDirectory = BitConverter.ToInt32(lump, offset + 16),
                    PatchCount = BitConverter.ToInt16(lump, offset + 20)
                };
                for (var i = offset + 22; i < (offset + 22 + map.PatchCount * 10); i += 10)
                {
                    map.Patches.Add(
                        new WADMapPatch
                        {
                            OriginX = BitConverter.ToInt16(lump, i + 0),
                            OriginY = BitConverter.ToInt16(lump, i + 2),
                            Patch = BitConverter.ToInt16(lump, i + 4),
                            StepDir = BitConverter.ToInt16(lump, i + 8),
                            //Colormap = BitConverter.ToInt16(lump, i + 10), //indexing errors for the very last patch
                        }
                    );
                }
                return map;
            }
        }
        public class WADPatch
        {
            public int Width { get; set; }
            public int Height { get; set; }
            public int Leftoffset { get; set; }
            public int Topoffset { get; set; }
            public List<int> Columnofs { get; set; } = new();
            public List<List<WADPosts>> Posts= new ();
            
            public static WADPatch FromLump(byte[] lump)
            {
                var patch = new WADPatch
                {
                    Width = BitConverter.ToInt16(lump, 0),
                    Height = BitConverter.ToInt16(lump, 2),
                    Leftoffset = BitConverter.ToInt16(lump, 4),
                    Topoffset = BitConverter.ToInt16(lump, 6),
                };
                for (var i = 8; i < 8 + patch.Width * 4; i += 4)
                {
                    patch.Columnofs.Add(BitConverter.ToInt32(lump, i));
                    patch.Posts.Add(new());
                }
                for (int idx = 0; idx < patch.Columnofs.Count; idx++)
                {
                    var colOffset = patch.Columnofs[idx];
                    while (true)
                    {
                        byte topdelta = lump[colOffset];
                        if (topdelta == 0xFF) break;

                        byte length = lump[colOffset + 1];
                        byte unused1 = lump[colOffset + 2];
                        byte[] data = lump.Skip(colOffset + 3).Take(length).ToArray();
                        byte unused2 = lump[colOffset + 3 + length];

                        patch.Posts[idx].Add(new WADPosts
                        {
                            Topdelta = topdelta,
                            Length = length,
                            Unused1 = unused1,
                            Data = new List<byte>(data),
                            Unused2 = unused2
                        });

                        colOffset += 4 + length;
                    }
                }
                return patch;
            }
            
            public void Render(Rgba32[] palette, int offset_x, int offset_y, Action<int, int , byte, byte, byte> fn)
            {
                for (int x = 0; x < Width; x++)
                {
                    var px = x + offset_x;
                    var posts = Posts[x];
                    foreach (var post in posts)
                    {
                        for (int y = 0; y < post.Length; y++)
                        {
                            var py = post.Topdelta + y + offset_y;
                            var c = palette[post.Data[y]];
                            //TODO: Fix Color types
                            fn.Invoke(px, py, c.R, c.G, c.B);
                        }
                    }   
                }
                //Raylib_cs.Raylib.DrawRectangleLines(
                //    offset_x,
                //    offset_y,
                //    Width,
                //    Height,
                //    Raylib_cs.Color.Red
                //);
            }
            public void Draw(Rgba32[] palette, int offset_x, int offset_y)
            {
                Render(palette, offset_x, offset_y, (px, py, r, g, b) =>
                {
                    //TODO: Implement Robust texture creation
                    //Raylib Implementation
                    //Raylib_cs.Raylib.DrawPixel(px, py, new Raylib_cs.Color(r, g, b));
                });
            }
            public void Draw(Rgba32[] palette)
            {
                Draw(palette, Leftoffset, Topoffset);
            }
        }
        public class WADPosts
        {
            public byte Topdelta { get; set; }
            public byte Length { get; set; }
            public byte Unused1 { get; set; } //padding
            public List<byte> Data { get; set; } = new();
            public byte Unused2 { get; set; } //padding
        }
        private WADHeader wad_header = new();
        private Dictionary<string, WADLump> wad_directories = new();
        private Dictionary<string, byte[]> wad_lumps = new();
        private List<Rgba32[]> PlayPal = new();
        private List<byte[]> ColorMap = new();
        private Dictionary<string, WADMapTexture> TextureMaps = new();
        private List<string> PatchNames = new();

        public void LoadWad(string path)
        {
            //probably forbidden
            //var stream = new FileStream(path, FileMode.Open, FileAccess.Read);
        }
        public void LoadWad(Stream stream)
        {
            stream.Seek(0, SeekOrigin.Begin);
            var reader = new BinaryReader(stream);

            byte[] Data = reader.ReadBytes(12);
            wad_header = new WADHeader
            {
                MagicNumber = GetCString(Data, 0, 4),
                NumLumps = BitConverter.ToUInt32(Data, 4),
                DirOffset = BitConverter.ToUInt32(Data, 8)
            };

            stream.Seek(wad_header.DirOffset, SeekOrigin.Begin);
            //add support for multiple maps in one wad. this code assumes each lump has a unique name
            for (var i = 0; i < wad_header.NumLumps; i++)
            {
                Data = reader.ReadBytes(16);
                var dir = new WADLump
                {
                    Offset = BitConverter.ToUInt32(Data, 0),
                    Size = BitConverter.ToInt32(Data, 4),
                    Name = GetCString(Data, 8, 8),
                };
                if (!wad_directories.ContainsKey(dir.Name)) wad_directories[dir.Name] = dir;
            }

            foreach (var entry in wad_directories.Values)
            {
                stream.Seek(entry.Offset, SeekOrigin.Begin);
                wad_lumps[entry.Name] = reader.ReadBytes(entry.Size);
                //System.Console.WriteLine($"Loaded lump: {entry.Name} -- {entry.Size} bytes");
            }
        }
        public static string GetCString(byte[] buffer, int offset, int count)
        {
            int end = offset + count;
            for (int i = offset; i < end; i++)
            {
                if (buffer[i] == 0)
                {
                    count = i - offset;
                    break;
                }
            }
            return Encoding.ASCII.GetString(buffer, offset, count);
        }
        public void LoadWadLineDefs()
        {
            lLines.Clear();
            var lump = wad_lumps["LINEDEFS"];
            for (var i = 0; i < lump.Count(); i += 14)
            {
                /*
                 linedefs.append({
                "start_vertex": int.from_bytes(linedefs_data[i:i+2], byteorder="little"),
                "end_vertex": int.from_bytes(linedefs_data[i+2:i+4], byteorder="little"),
                "flags": int.from_bytes(linedefs_data[i+4:i+6], byteorder="little"),
                "line_type": int.from_bytes(linedefs_data[i+6:i+8], byteorder="little"),
                "sector_tag": int.from_bytes(linedefs_data[i+8:i+10], byteorder="little"),
                "front_sidedef": int.from_bytes(linedefs_data[i+10:i+12], byteorder="little"),
                "back_sidedef": int.from_bytes(linedefs_data[i+12:i+14], byteorder="little"),
                   })
                 */
                var line = new LineDef(
                        BitConverter.ToUInt16(lump, i + 0),
                        BitConverter.ToUInt16(lump, i + 2),
                        BitConverter.ToUInt16(lump, i + 10),
                        BitConverter.ToUInt16(lump, i + 12)
                    );
                lLines.Add(line);
            }
        }
        public void LoadWadVertexes()
        {
            lVertex.Clear();
            var lump = wad_lumps["VERTEXES"];
            for (var i = 0; i < lump.Count(); i += 4)
            {
                var vertex = new Vector2(
                        BitConverter.ToInt16(lump, i + 0),
                        BitConverter.ToInt16(lump, i + 2)
                    );
                lVertex.Add(vertex);
            }
        }
        public void LoadWadSides()
        {
            lSides.Clear();
            var lump = wad_lumps["SIDEDEFS"];
            for (var i = 0; i < lump.Count(); i += 30)
            {
                var o = new SideDef(
                        BitConverter.ToInt16(lump, i + 0),
                        BitConverter.ToInt16(lump, i + 2),
                        GetCString(lump, i + 4, 8),
                        GetCString(lump, i + 12, 8),
                        GetCString(lump, i + 20, 8),
                        BitConverter.ToInt16(lump, i + 28)
                    );
                lSides.Add(o);
            }
        }
        public void LoadWadSectors()
        {
            lSectors.Clear();
            var lump = wad_lumps["SECTORS"];
            for (var i = 0; i < lump.Count(); i += 26)
            {
                var o = new Sector(
                        BitConverter.ToInt16(lump, i + 0),
                        BitConverter.ToInt16(lump, i + 2),
                        GetCString(lump, i + 4, 8),
                        GetCString(lump, i + 12, 8),
                        BitConverter.ToInt16(lump, i + 20),
                        BitConverter.ToInt16(lump, i + 22),
                        BitConverter.ToInt16(lump, i + 24)
                    );
                lSectors.Add(o);
            }
        }

        public void LoadTextures()
        {
            var lump = wad_lumps["PNAMES"];
            var n_patches = BitConverter.ToInt32(lump, 0);
            for (var i = 0; i < n_patches * 8; i += 8)
            {
                PatchNames.Add(GetCString(lump, 4 + i, 8));
            }

            lump = wad_lumps["TEXTURE1"];
            var num_textures = BitConverter.ToInt32(lump, 0);
            var texture_offsets = new List<int>();
            for (var i = 0; i < num_textures * 4; i += 4)
            {
                texture_offsets.Add(BitConverter.ToInt32(lump, 4 + i));
            }

            foreach (var offset in texture_offsets)
            {
                var map = WADMapTexture.FromLump(lump, offset);
                TextureMaps[map.Name] = map;
            }

            if (wad_lumps.ContainsKey("TEXTURE2"))
            {
                lump = wad_lumps["TEXTURE2"];
                num_textures = BitConverter.ToInt32(lump, 0);
                texture_offsets = new List<int>();
                for (var i = 0; i < num_textures * 4; i += 4)
                {
                    texture_offsets.Add(BitConverter.ToInt32(lump, 4 + i));
                }
                foreach (var offset in texture_offsets)
                {
                    var map = WADMapTexture.FromLump(lump, offset);
                    TextureMaps[map.Name] = map;
                }
            }


            lump = wad_lumps["PLAYPAL"];
            for (var i = 0; i < lump.Length; i += 256 * 3)
            {
                var col = new Rgba32[256];
                for (var idx = 0; idx < 256; idx++)
                {
                    col[idx] = new Rgba32(
                        lump[i + idx * 3 + 0],
                        lump[i + idx * 3 + 1],
                        lump[i + idx * 3 + 2]
                    );
                }
                PlayPal.Add(col);
            }
            //Console.WriteLine($"Loaded {playpal.Count} Palettes");
            lump = wad_lumps["COLORMAP"];
            for (var i = 0; i < lump.Length; i += 256)
            {
                var col = new byte[256];
                for (var idx = 0; idx < 256; idx++)
                {
                    col[idx] = lump[i + idx];
                }
                ColorMap.Add(col);
            }
            //Console.WriteLine($"Loaded {ColorMap.Count} ColorMaps");
        }

        public Action? DrawPatch;
    }
    
}
