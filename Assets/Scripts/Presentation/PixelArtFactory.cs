using System.Collections.Generic;
using UnityEngine;

namespace GameDevStudio.Presentation
{
    public static class PixelArtFactory
    {
        public const int Ppu = 16;

        static readonly Dictionary<string, Sprite> Cache = new Dictionary<string, Sprite>();

        static readonly Color32[] HairDark = { C(42, 30, 24), C(72, 48, 36), C(28, 20, 16) };
        static readonly Color32[] HairBlack = { C(24, 22, 26), C(48, 46, 52), C(16, 14, 18) };
        static readonly Color32[] HairBlond = { C(196, 156, 72), C(224, 196, 110), C(140, 108, 48) };
        static readonly Color32[] HairRed = { C(156, 64, 40), C(196, 96, 58), C(110, 42, 28) };
        static readonly Color32[] HairBlue = { C(58, 72, 110), C(90, 108, 150), C(36, 46, 78) };
        static readonly Color32[] SkinLight = { C(236, 196, 158), C(214, 168, 130), C(255, 224, 190) };
        static readonly Color32[] SkinTan = { C(196, 140, 96), C(168, 112, 74), C(220, 168, 120) };
        static readonly Color32[] SkinDeep = { C(118, 78, 52), C(88, 56, 36), C(148, 102, 70) };

        static Color32 C(byte r, byte g, byte b, byte a = 255)
        {
            return new Color32(r, g, b, a);
        }

        public static Sprite FloorTile()
        {
            return Load("floor", new Vector2(0.5f, 0.5f)) ?? FloorFallback();
        }

        public static Sprite Wall()
        {
            return Load("wall", new Vector2(0.5f, 0.5f)) ?? WallFallback();
        }

        public static Sprite Desk()
        {
            return Load("desk", new Vector2(0.5f, 0f)) ?? DeskFallback();
        }

        public static Sprite EmptySlot()
        {
            return Load("empty", new Vector2(0.5f, 0f)) ?? EmptyFallback();
        }

        public static Sprite Door()
        {
            return Load("door", new Vector2(0.5f, 0f)) ?? Draw(16, 24, (x, y) =>
            {
                if (x == 0 || x == 15 || y == 0 || y == 23)
                {
                    return C(62, 42, 32);
                }

                return ((x / 4) & 1) == 0 ? C(110, 72, 42) : C(78, 50, 30);
            }, new Vector2(0.5f, 0f));
        }

        public static Sprite CoffeeMachine()
        {
            return Load("coffee", new Vector2(0.5f, 0f)) ?? Solid(14, 18, C(70, 74, 80));
        }

        public static Sprite Toilet()
        {
            return Load("toilet", new Vector2(0.5f, 0f)) ?? Solid(14, 16, C(230, 230, 236));
        }

        public static Sprite Sofa()
        {
            return Load("sofa", new Vector2(0.5f, 0f)) ?? Solid(22, 12, C(118, 58, 58));
        }

        public static Sprite Plant()
        {
            return Load("plant", new Vector2(0.5f, 0f));
        }

        public static Sprite Window()
        {
            return Load("window", new Vector2(0.5f, 0.5f));
        }

        public static Sprite Poster()
        {
            return Load("poster", new Vector2(0.5f, 0.5f));
        }

        public static Sprite Rug()
        {
            return Load("rug", new Vector2(0.5f, 0.5f));
        }

        public static Sprite SelectionPad()
        {
            return Load("pad", new Vector2(0.5f, 0.5f)) ?? Solid(16, 8, C(255, 220, 80, 180));
        }

        public static Sprite StatusPip(Color32 color)
        {
            Sprite pip = Load("pip", new Vector2(0.5f, 0.5f));
            return pip != null ? pip : Solid(4, 4, color);
        }

        public static Sprite Character(Color32 shirt, int seed)
        {
            Color32[] hair = HairSet(seed % 5);
            Color32[] skin = SkinSet((seed / 5) % 3);
            Color32 outline = C(22, 16, 14);
            Color32 pants = seed % 2 == 0 ? C(48, 54, 74) : C(62, 50, 48);
            Color32 pantsD = C(32, 34, 46);
            Color32 shoe = C(36, 30, 28);
            Color32 shirtD = Shade(shirt, 0.72f);
            Color32 shirtH = Shade(shirt, 1.18f);
            const int w = 16;
            const int h = 22;
            var buf = new Color32[w * h];
            // shoes
            Fill(buf, w, 4, 0, 6, 1, shoe);
            Fill(buf, w, 9, 0, 11, 1, shoe);
            // legs
            Fill(buf, w, 5, 2, 6, 6, pants);
            Fill(buf, w, 9, 2, 10, 6, pants);
            P(buf, w, 5, 2, pantsD);
            P(buf, w, 10, 2, pantsD);
            // torso
            Fill(buf, w, 4, 7, 11, 13, shirt);
            Fill(buf, w, 4, 7, 5, 8, shirtH);
            Fill(buf, w, 10, 11, 11, 13, shirtD);
            // belt
            Fill(buf, w, 4, 6, 11, 6, C(40, 32, 28));
            P(buf, w, 8, 6, C(196, 164, 72));
            // arms
            Fill(buf, w, 3, 8, 3, 12, shirtD);
            Fill(buf, w, 12, 8, 12, 12, shirt);
            Fill(buf, w, 2, 12, 3, 13, skin[0]);
            Fill(buf, w, 12, 12, 13, 13, skin[0]);
            // neck / head
            Fill(buf, w, 6, 14, 9, 14, skin[1]);
            Fill(buf, w, 5, 15, 10, 18, skin[0]);
            Fill(buf, w, 6, 15, 7, 16, skin[2]);
            // eyes
            P(buf, w, 6, 17, C(24, 22, 28));
            P(buf, w, 9, 17, C(24, 22, 28));
            P(buf, w, 7, 18, skin[2]);
            P(buf, w, 8, 15, C(160, 90, 90));
            // hair
            Fill(buf, w, 4, 18, 11, 20, hair[0]);
            Fill(buf, w, 5, 20, 10, 21, hair[1]);
            if (seed % 3 == 0)
            {
                Fill(buf, w, 4, 16, 5, 18, hair[0]);
                Fill(buf, w, 10, 16, 11, 18, hair[0]);
            }
            else if (seed % 3 == 1)
            {
                Fill(buf, w, 3, 19, 4, 20, hair[2]);
                Fill(buf, w, 11, 18, 12, 20, hair[0]);
            }
            else
            {
                Fill(buf, w, 5, 19, 10, 21, hair[1]);
                hline(buf, w, 4, 11, 18, hair[2]);
            }

            Outline(buf, w, h, outline);
            return FromBuffer(buf, w, h, new Vector2(0.5f, 0f));
        }

        static Color32[] HairSet(int i)
        {
            switch (i)
            {
                case 1: return HairBlack;
                case 2: return HairBlond;
                case 3: return HairRed;
                case 4: return HairBlue;
                default: return HairDark;
            }
        }

        static Color32[] SkinSet(int i)
        {
            if (i == 1)
            {
                return SkinTan;
            }

            return i == 2 ? SkinDeep : SkinLight;
        }

        static Color32 Shade(Color32 c, float m)
        {
            return new Color32(
                (byte)Mathf.Clamp(Mathf.RoundToInt(c.r * m), 0, 255),
                (byte)Mathf.Clamp(Mathf.RoundToInt(c.g * m), 0, 255),
                (byte)Mathf.Clamp(Mathf.RoundToInt(c.b * m), 0, 255),
                c.a);
        }

        static void Outline(Color32[] buf, int w, int h, Color32 outline)
        {
            var copy = (Color32[])buf.Clone();
            for (int y = 0; y < h; y++)
            {
                for (int x = 0; x < w; x++)
                {
                    if (copy[y * w + x].a == 0)
                    {
                        continue;
                    }

                    if (Empty(copy, w, h, x - 1, y) || Empty(copy, w, h, x + 1, y) ||
                        Empty(copy, w, h, x, y - 1) || Empty(copy, w, h, x, y + 1))
                    {
                        buf[y * w + x] = outline;
                    }
                }
            }
        }

        static bool Empty(Color32[] buf, int w, int h, int x, int y)
        {
            if ((uint)x >= (uint)w || (uint)y >= (uint)h)
            {
                return true;
            }

            return buf[y * w + x].a == 0;
        }

        static void Fill(Color32[] buf, int w, int x0, int y0, int x1, int y1, Color32 c)
        {
            for (int y = y0; y <= y1; y++)
            {
                for (int x = x0; x <= x1; x++)
                {
                    P(buf, w, x, y, c);
                }
            }
        }

        static void hline(Color32[] buf, int w, int x0, int x1, int y, Color32 c)
        {
            for (int x = x0; x <= x1; x++)
            {
                P(buf, w, x, y, c);
            }
        }

        static void P(Color32[] buf, int w, int x, int y, Color32 c)
        {
            if ((uint)x >= 16u || y < 0)
            {
                return;
            }

            int i = y * w + x;
            if ((uint)i < (uint)buf.Length)
            {
                buf[i] = c;
            }
        }

        public static Sprite Solid(int width, int height, Color32 fill)
        {
            return Draw(width, height, (x, y) => fill, new Vector2(0.5f, 0.5f));
        }

        static Sprite FloorFallback()
        {
            Color32 a = C(168, 118, 70);
            Color32 b = C(158, 108, 62);
            Color32 grout = C(112, 74, 42);
            return Draw(16, 16, (x, y) => y % 4 == 0 ? grout : ((x + y) & 2) == 0 ? a : b, new Vector2(0.5f, 0.5f));
        }

        static Sprite WallFallback()
        {
            Color32 plaster = C(214, 196, 168);
            Color32 rail = C(122, 86, 62);
            return Draw(16, 16, (x, y) => y >= 12 ? rail : plaster, new Vector2(0.5f, 0.5f));
        }

        static Sprite DeskFallback()
        {
            return Draw(22, 14, (x, y) =>
            {
                if (y <= 1)
                {
                    return C(62, 42, 30);
                }

                if (y <= 4)
                {
                    return C(132, 92, 52);
                }

                if (x >= 6 && x <= 16 && y >= 6 && y <= 12)
                {
                    return x == 6 || x == 16 || y == 6 || y == 12 ? C(40, 44, 48) : C(90, 190, 210);
                }

                return C(0, 0, 0, 0);
            }, new Vector2(0.5f, 0f));
        }

        static Sprite EmptyFallback()
        {
            Color32 dash = C(90, 70, 58, 180);
            return Draw(22, 14, (x, y) =>
            {
                bool border = x == 0 || y == 0 || x == 21 || y == 13;
                return border || ((x + y) % 4) == 0 ? dash : C(0, 0, 0, 0);
            }, new Vector2(0.5f, 0f));
        }

        static Sprite Load(string name, Vector2 pivot)
        {
            if (Cache.TryGetValue(name, out Sprite cached) && cached != null)
            {
                return cached;
            }

            Sprite sprite = Resources.Load<Sprite>("Office/" + name);
            if (sprite == null)
            {
                Texture2D tex = Resources.Load<Texture2D>("Office/" + name);
                if (tex != null)
                {
                    tex.filterMode = FilterMode.Point;
                    tex.wrapMode = TextureWrapMode.Clamp;
                    sprite = Sprite.Create(tex, new Rect(0f, 0f, tex.width, tex.height), pivot, Ppu, 0, SpriteMeshType.FullRect);
                }
            }

            if (sprite != null)
            {
                Cache[name] = sprite;
            }

            return sprite;
        }

        static Sprite FromBuffer(Color32[] pixels, int width, int height, Vector2 pivot)
        {
            var texture = new Texture2D(width, height, TextureFormat.RGBA32, false)
            {
                filterMode = FilterMode.Point,
                wrapMode = TextureWrapMode.Clamp
            };
            texture.SetPixels32(pixels);
            texture.Apply(false, false);
            return Sprite.Create(texture, new Rect(0f, 0f, width, height), pivot, Ppu, 0, SpriteMeshType.FullRect);
        }

        public static Sprite Draw(int width, int height, System.Func<int, int, Color32> plot, Vector2 pivot)
        {
            var pixels = new Color32[width * height];
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    pixels[y * width + x] = plot(x, y);
                }
            }

            return FromBuffer(pixels, width, height, pivot);
        }
    }
}
