using UnityEngine;

namespace GameDevStudio.Presentation
{
    public static class PixelArtFactory
    {
        public const int Ppu = 16;

        public static Sprite Solid(int width, int height, Color32 fill)
        {
            return Draw(width, height, (x, y) => fill, new Vector2(0.5f, 0.5f));
        }

        public static Sprite Outlined(int width, int height, Color32 fill, Color32 outline)
        {
            return Draw(width, height, (x, y) =>
            {
                if (x == 0 || y == 0 || x == width - 1 || y == height - 1)
                {
                    return outline;
                }

                return fill;
            }, new Vector2(0.5f, 0.5f));
        }

        public static Sprite FloorTile()
        {
            Color32 a = new Color32(196, 162, 110, 255);
            Color32 b = new Color32(184, 148, 96, 255);
            Color32 grout = new Color32(150, 118, 74, 255);
            return Draw(16, 16, (x, y) =>
            {
                if (x == 0 || y == 0)
                {
                    return grout;
                }

                return ((x + y) & 2) == 0 ? a : b;
            }, new Vector2(0.5f, 0.5f));
        }

        public static Sprite Wall()
        {
            Color32 brick = new Color32(118, 86, 74, 255);
            Color32 mortar = new Color32(86, 62, 54, 255);
            Color32 trim = new Color32(72, 52, 46, 255);
            return Draw(16, 16, (x, y) =>
            {
                if (y == 0 || y == 15)
                {
                    return trim;
                }

                int row = y / 4;
                int ox = (row & 1) == 0 ? 0 : 4;
                if (y % 4 == 0 || (x + ox) % 8 == 0)
                {
                    return mortar;
                }

                return brick;
            }, new Vector2(0.5f, 0.5f));
        }

        public static Sprite Desk()
        {
            return Draw(22, 14, (x, y) =>
            {
                if (y <= 1)
                {
                    return new Color32(62, 42, 30, 255);
                }

                if (y <= 4)
                {
                    return new Color32(132, 92, 52, 255);
                }

                if (x >= 6 && x <= 16 && y >= 6 && y <= 12)
                {
                    if (x == 6 || x == 16 || y == 6 || y == 12)
                    {
                        return new Color32(40, 44, 48, 255);
                    }

                    return new Color32(90, 190, 210, 255);
                }

                if ((x <= 2 || x >= 19) && y <= 8)
                {
                    return new Color32(78, 54, 36, 255);
                }

                return new Color32(0, 0, 0, 0);
            }, new Vector2(0.5f, 0f));
        }

        public static Sprite EmptySlot()
        {
            Color32 dash = new Color32(90, 70, 58, 180);
            return Draw(22, 14, (x, y) =>
            {
                bool border = x == 0 || y == 0 || x == 21 || y == 13;
                bool dashLine = ((x + y) % 4) == 0;
                if (border || dashLine)
                {
                    return dash;
                }

                return new Color32(0, 0, 0, 0);
            }, new Vector2(0.5f, 0f));
        }

        public static Sprite Character(Color32 shirt)
        {
            Color32 skin = new Color32(232, 190, 150, 255);
            Color32 hair = new Color32(48, 36, 32, 255);
            Color32 pants = new Color32(52, 58, 78, 255);
            Color32 outline = new Color32(28, 22, 20, 255);
            return Draw(12, 16, (x, y) =>
            {
                if (x == 0 || y == 0 || x == 11 || y == 15)
                {
                    return new Color32(0, 0, 0, 0);
                }

                bool body = x >= 3 && x <= 8 && y >= 2 && y <= 13;
                if (!body)
                {
                    return new Color32(0, 0, 0, 0);
                }

                if (y >= 11)
                {
                    return x == 3 || x == 8 || y == 13 ? outline : hair;
                }

                if (y >= 8)
                {
                    return x == 3 || x == 8 ? outline : skin;
                }

                if (y >= 4)
                {
                    return shirt;
                }

                return pants;
            }, new Vector2(0.5f, 0f));
        }

        public static Sprite Door()
        {
            Color32 frame = new Color32(62, 42, 32, 255);
            Color32 wood = new Color32(110, 72, 42, 255);
            Color32 dark = new Color32(78, 50, 30, 255);
            return Draw(16, 24, (x, y) =>
            {
                if (x == 0 || x == 15 || y == 0 || y == 23)
                {
                    return frame;
                }

                if (x >= 11 && x <= 12 && y >= 10 && y <= 12)
                {
                    return new Color32(200, 170, 70, 255);
                }

                return ((x / 4) & 1) == 0 ? wood : dark;
            }, new Vector2(0.5f, 0f));
        }

        public static Sprite CoffeeMachine()
        {
            return Draw(14, 18, (x, y) =>
            {
                if (y <= 1)
                {
                    return new Color32(48, 40, 36, 255);
                }

                if (x <= 1 || x >= 12 || y >= 16)
                {
                    return new Color32(36, 32, 30, 255);
                }

                if (y >= 10)
                {
                    return new Color32(70, 74, 80, 255);
                }

                if (x >= 5 && x <= 8 && y >= 4 && y <= 8)
                {
                    return new Color32(42, 26, 18, 255);
                }

                if (x >= 6 && x <= 7 && y >= 8 && y <= 9)
                {
                    return new Color32(210, 150, 70, 255);
                }

                return new Color32(88, 92, 98, 255);
            }, new Vector2(0.5f, 0f));
        }

        public static Sprite Toilet()
        {
            return Draw(14, 16, (x, y) =>
            {
                if (y <= 1)
                {
                    return new Color32(90, 90, 96, 255);
                }

                if (x >= 3 && x <= 10 && y >= 2 && y <= 8)
                {
                    return new Color32(230, 230, 236, 255);
                }

                if (x >= 4 && x <= 9 && y >= 9 && y <= 14)
                {
                    return x == 4 || x == 9 || y == 14 ? new Color32(180, 180, 190, 255) : new Color32(210, 210, 220, 255);
                }

                return new Color32(0, 0, 0, 0);
            }, new Vector2(0.5f, 0f));
        }

        public static Sprite Sofa()
        {
            return Draw(22, 12, (x, y) =>
            {
                if (y <= 1)
                {
                    return new Color32(52, 36, 32, 255);
                }

                if (y <= 5)
                {
                    return new Color32(118, 58, 58, 255);
                }

                if ((x <= 3 || x >= 18) && y <= 10)
                {
                    return new Color32(96, 44, 44, 255);
                }

                if (y <= 9)
                {
                    return new Color32(140, 70, 70, 255);
                }

                return new Color32(0, 0, 0, 0);
            }, new Vector2(0.5f, 0f));
        }

        public static Sprite SelectionPad()
        {
            Color32 ring = new Color32(255, 220, 80, 180);
            return Draw(16, 8, (x, y) =>
            {
                bool edge = x == 0 || y == 0 || x == 15 || y == 7;
                return edge ? ring : new Color32(255, 220, 80, 40);
            }, new Vector2(0.5f, 0.5f));
        }

        public static Sprite StatusPip(Color32 color)
        {
            return Solid(4, 4, color);
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

            var texture = new Texture2D(width, height, TextureFormat.RGBA32, false)
            {
                filterMode = FilterMode.Point,
                wrapMode = TextureWrapMode.Clamp
            };
            texture.SetPixels32(pixels);
            texture.Apply(false, false);
            return Sprite.Create(texture, new Rect(0f, 0f, width, height), pivot, Ppu);
        }
    }
}
