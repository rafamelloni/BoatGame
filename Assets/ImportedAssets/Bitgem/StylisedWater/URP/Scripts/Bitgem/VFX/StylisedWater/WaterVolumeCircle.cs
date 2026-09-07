using UnityEngine;

namespace Bitgem.VFX.StylisedWater
{
    [AddComponentMenu("Bitgem/Water Volume (Circle)")]
    public class WaterVolumeCircle : WaterVolumeBase
    {
        #region Public fields

        [Min(0.1f)]
        public float Radius = 5f;

        [Range(1, MAX_TILES_Y)]
        public int Height = 1;

        #endregion

        #region Public methods

        protected override void GenerateTiles(ref bool[,,] _tiles)
        {
            var radiusInTiles = Radius / TileSize;
            var maxDiameter = Mathf.Min(MAX_TILES_X, MAX_TILES_Z);
            var diameterInTiles = Mathf.CeilToInt(radiusInTiles * 2f);

            if (diameterInTiles > maxDiameter)
            {
                Debug.LogWarning($"[WaterVolumeCircle] '{name}': con Radius {Radius} y Tile Size {TileSize} necesitarías {diameterInTiles} tiles de diámetro, pero el máximo de la grilla es {maxDiameter}. Subí el Tile Size a por lo menos {Radius * 2f / maxDiameter:0.##} para que entre entero (si no, se recorta).", this);
                diameterInTiles = maxDiameter;
                radiusInTiles = maxDiameter / 2f; // radio real que vas a ver, ya recortado
            }
            else if (diameterInTiles < 8)
            {
                Debug.LogWarning($"[WaterVolumeCircle] '{name}': solo {diameterInTiles} tiles de diámetro, se va a ver poligonal. Bajá el Tile Size a {Radius * 2f / 8f:0.##} o menos para que se vea más redondo.", this);
            }

            var maxY = Mathf.Clamp(Height, 1, MAX_TILES_Y);
            var center = (diameterInTiles - 1) * 0.5f;

            for (var x = 0; x < diameterInTiles; x++)
            {
                for (var z = 0; z < diameterInTiles; z++)
                {
                    var dx = x - center;
                    var dz = z - center;
                    if (Mathf.Sqrt(dx * dx + dz * dz) > radiusInTiles)
                    {
                        continue;
                    }

                    for (var y = 0; y < maxY; y++)
                    {
                        _tiles[x, y, z] = true;
                    }
                }
            }
        }

        public override void Validate()
        {
            Radius = Mathf.Max(Radius, 0.1f);
            Height = Mathf.Clamp(Height, 1, MAX_TILES_Y);
        }

        #endregion
    }
}