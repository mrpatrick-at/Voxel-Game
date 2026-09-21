using Godot;
namespace VoxelGame.scenes.main.systems.map;

public sealed class NoiseGenerator(int Seed) {

   private readonly FastNoiseLite ContinentsNoise = MakeContinentsNoise(Seed);
   private readonly FastNoiseLite HillsNoise = MakeHillsNoise(Seed);

   // Public Funcs
   public int[] MakeChunkHeightMap(Vector2I Coord) {
      int[] Heightmap = new int[Consts.Chunk.SqExtendedSize];

      for (int x = 0; x < Consts.Chunk.ExtendedSize; x++) {
         for (int y = 0; y < Consts.Chunk.ExtendedSize; y++) {

            float PixelData = -HillsNoise.GetNoise2D(x + Coord.X * Consts.Chunk.Size, y + Coord.Y * Consts.Chunk.Size);

            int TileHeight = (int)((PixelData + 1) * 0.5 * (Consts.World.Height - 1) + 1);

            Heightmap[GetHeightmapIndex(x, y)] = TileHeight;

            // int LocalTileHeight = Math.Min(TileHeight - Coord.Y * Consts.Chunk.Size, 17);

            // for (int y = 0; y <= LocalTileHeight; y++) {
            //    int Block = (LocalTileHeight - y) switch {
            //       0 => (int)Consts.Voxel.Type.Grass,
            //       < 3 => (int)Consts.Voxel.Type.Dirt,
            //       _ => (int)Consts.Voxel.Type.Stone,
            //    };

            //    Heightmap[GetVoxelIndex(x, y, z)] = Block;
            // }
         }
      }

      return Heightmap;
   }

   // Helpers
   private static int GetHeightmapIndex(int x, int y) {
      return x + Consts.Chunk.ExtendedSize * y;
   }

   // Noises
   public static FastNoiseLite MakeContinentsNoise(int Seed) {
      FastNoiseLite Noise = new() {
         NoiseType = FastNoiseLite.NoiseTypeEnum.SimplexSmooth,
         FractalType = FastNoiseLite.FractalTypeEnum.Fbm,
         FractalOctaves = 2,
         Seed = Seed,
         Frequency = 0.001F
      };

      return Noise;
   }

   public static FastNoiseLite MakeHillsNoise(int Seed) {
      FastNoiseLite Noise = new() {
         NoiseType = FastNoiseLite.NoiseTypeEnum.SimplexSmooth,
         FractalType = FastNoiseLite.FractalTypeEnum.Ridged,
         FractalOctaves = 1,
         Seed = Seed,
         Frequency = 0.0025F
      };

      return Noise;
   }
}

