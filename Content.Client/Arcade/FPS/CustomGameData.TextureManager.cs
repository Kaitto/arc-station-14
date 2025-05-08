using Content.Client.Resources;
using Robust.Client.Graphics;
using Robust.Client.ResourceManagement;
using SixLabors.ImageSharp.PixelFormats;
using System.Linq;

namespace Content.Client.Arcade.FPS
{
    public class CustomGameDataTextureManager
    {
        public static CustomGameDataTextureManager Instance = new();
        private List<string> LoadingTextures = new();
        private Dictionary<string, Texture?> textureCache = new Dictionary<string, Texture?>();
        private List<Action<DrawingHandleScreen>> TargetTextures = new();
        private Texture fallback;

        public CustomGameDataTextureManager()
        {
            fallback = IoCManager.Resolve<IResourceCache>().GetTexture("/Textures/Interface/Nano/square_black.png");
        }
        public void BuildTextures(DrawingHandleScreen handle)
        {
            foreach (var texture in TargetTextures)
            {
                texture.Invoke(handle);
            }
            TargetTextures.Clear();
            LoadingTextures.Clear();
        }
        public bool IsReady(string path) => LoadingTextures.Contains(path);
        public bool HasTexture(string path) => textureCache.ContainsKey(path);
        public void SetTexture(string path, Texture? tx)
        {
            textureCache[path] = tx;
        }
        public Texture GetTexture(string path)
        {
            if (textureCache.TryGetValue(path, out var t)) return t ?? fallback;

            //fallback.
            //TODO: Map some SS14 Textures as a fallback method
            return fallback;//textureCache[path];
        }

        public Texture CreateTextureFromArray(Vector2i size, Rgba32[] arr)
        {
            var span = new ReadOnlySpan<Rgba32>(arr, 0, size.X * size.Y);
            var owned_texture = IoCManager.Resolve<IClyde>().CreateBlankTexture<SixLabors.ImageSharp.PixelFormats.Rgba32>(size);
            owned_texture.SetSubImage<Rgba32>(Vector2i.Zero, size, span);
            return owned_texture;
        }
    }
    public partial struct GameData
    {
        public Texture? GetFlatTexture(string name)
        {
            var resman = CustomGameDataTextureManager.Instance;
            if (resman.HasTexture(name)) return resman.GetTexture(name);
            if (!wad_lumps.ContainsKey(name))
            {
                //Console.Write($"There's no lump called {name}...");
                //Console.WriteLine($"fallback");
                resman.SetTexture(name, null);
                return null;
            }
            var lump = wad_lumps[name];
            var raw = lump.Take(64 * 64).ToArray();

            //TODO: Add Robust implementation
            //raylib implementation
            //var img = Raylib_cs.Raylib.GenImageColor(64, 64, Raylib_cs.Color.White);

            //for (int i = 0; i < 64 * 64; i++)
            //{
            //    var idx = raw[i];
            //    var c = new Raylib_cs.Color(PlayPal[0][idx].R, PlayPal[0][idx].G, PlayPal[0][idx].B);
            //    Raylib_cs.Raylib.ImageDrawPixel(ref img, i % 64, i / 64, c);
            //}

            //Raylib_cs.Texture2D tx = Raylib_cs.Raylib.LoadTextureFromImage(img);
            //Raylib_cs.Raylib.UnloadImage(img);

            //resman.SetTexture(name, new Texture(tx));

            return resman.GetTexture(name);
        }
        
        public Texture? GetTexture(string name)
        {
            var resman = CustomGameDataTextureManager.Instance;
            //if (TextureCache.ContainsKey(name)) return TextureCache[name];
            if (resman.HasTexture(name)) return resman.GetTexture(name);
            if (!TextureMaps.ContainsKey(name))
            {
                //Console.Write($"There's no texture map called {name}...");
                //if (PatchNames.Contains(name)) Console.WriteLine($"it is a patch!");
                //if (wad_lumps.ContainsKey(name)) Console.WriteLine($"there's a lump!");
                //Console.WriteLine($"fallback");

                resman.SetTexture(name, null);
                return null;
            };
            var textureMap = TextureMaps[name];
            //Console.WriteLine($"Loading texture {textureMap.Name}");
            var self = this;
            //TODO: implement Robust texture creation
            var clyde = IoCManager.Resolve<IClyde>();

            //var tx= _clyde.CreateBlankTexture<SixLabors.ImageSharp.PixelFormats.Rgb24>(new Vector2i(textureMap.Width, textureMap.Height), $"FPS_{textureMap.Name}");

            //I HATE THIS. I NEED RAW TEXTURES

            //var tx = clyde.CreateBlankTexture<Rgba32>(new Vector2i(textureMap.Width, textureMap.Height), $"FPS_{textureMap}");

            //var tx = clyde.CreateRenderTarget(
            //    new Vector2i(textureMap.Width, textureMap.Height),
            //    new RenderTargetFormatParameters(RenderTargetColorFormat.Rgba8Srgb),name: $"FPS_{textureMap}"
            //);
            Rgba32[] raw = new Rgba32[textureMap.Width * textureMap.Height];
            foreach (var map_p in textureMap.Patches)
            {
                var patch_name = self.PatchNames[map_p.Patch].ToUpper();
                if (!self.wad_lumps.ContainsKey(patch_name))
                {
                    //Console.WriteLine($"Missing Patch {patch_name} for TextureMap {textureMap.Name}"); //no console here
                    continue;
                }
                var lump = self.wad_lumps[patch_name];
                var patch = WADPatch.FromLump(lump);
                //Console.WriteLine($"** Patch {patch_name} @ {map_p.OriginX}:{map_p.OriginY}");
                patch.Render(self.PlayPal[0], map_p.OriginX, map_p.OriginY, (x, y, r, g, b) =>
                {
                    //handle.DrawCircle(new System.Numerics.Vector2(x, y), 1.0f, new Color(r, g, b));
                    raw[x % textureMap.Width + (y % textureMap.Height) * textureMap.Width] = new Rgba32 { A = 255, R = r, G = g, B = b };
                });
            }
            var tx = resman.CreateTextureFromArray(new Vector2i(textureMap.Width, textureMap.Height), raw);
            resman.SetTexture(name, tx);
            //raylib implementation
            //var img = Raylib_cs.Raylib.GenImageColor(textureMap.Width, textureMap.Height, Raylib_cs.Color.White);

            //foreach (var map_p in textureMap.Patches)
            //{
            //    var patch_name = PatchNames[map_p.Patch].ToUpper();
            //    if (!wad_lumps.ContainsKey(patch_name))
            //    {
            //        Console.WriteLine($"Missing Patch {patch_name} for TextureMap {textureMap.Name}");
            //        continue;
            //    }
            //    var lump = wad_lumps[patch_name];
            //    var patch = WADPatch.FromLump(lump);
            //    //Console.WriteLine($"** Patch {patch_name} @ {map_p.OriginX}:{map_p.OriginY}");

            //    patch.Render(self.PlayPal[0], map_p.OriginX, map_p.OriginY, (x, y, r, g, b) =>
            //    {
            //        Raylib_cs.Raylib.ImageDrawPixel(ref img, x, y, new Raylib_cs.Color(r, g, b));
            //    });
            //}
            //Raylib_cs.Texture2D tx = Raylib_cs.Raylib.LoadTextureFromImage(img);
            //Raylib_cs.Raylib.UnloadImage(img);

            //resman.SetTexture(name, new Texture(tx));
            //return resman.GetTexture(""); //return fallback
            return tx;
        }
    }
}
