using UnityEngine;

namespace GameDevStudio.Core
{
    public static class OfficeGrid
    {
        public static Vector3 Origin(int tilesX, int tilesY)
        {
            return new Vector3(-tilesX * 0.5f + 0.5f, -tilesY * 0.5f, 0f);
        }

        public static Vector3 World(int tilesX, int tilesY, float tileX, float tileY)
        {
            return Origin(tilesX, tilesY) + new Vector3(tileX, tileY, 0f);
        }
    }
}
