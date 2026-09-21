using Godot;
namespace VoxelGame.scenes.main.systems.map;

public sealed class NoiseGenerator(int Seed) {

   private readonly FastNoiseLite ContinentsNoise = MakeContinentsNoise(Seed);
   private readonly FastNoiseLite HillsNoise = MakeHillsNoise(Seed);

   // Public Funcs
   public int[] MakeChunkHeightMap(Vector2I SliceCoord) {
      int[] Heightmap = new int[Consts.Chunk.SqExtendedSize];

      for (int x = 0; x < Consts.Chunk.ExtendedSize; x++) {
         for (int y = 0; y < Consts.Chunk.ExtendedSize; y++) {
            Vector2I BlockCoord = new(x + SliceCoord.X * Consts.Chunk.Size, y + SliceCoord.Y * Consts.Chunk.Size);

            Heightmap[GetHeightmapIndex(x, y)] = GetBlockHeight(BlockCoord); ;
         }
      }

      return Heightmap;
   }

   public int GetBlockHeight(Vector2I Coord) {
      float PixelData = -HillsNoise.GetNoise2Dv(Coord);

      int TileHeight = (int)((PixelData + 1) * 0.5 * (Consts.World.Height - 1) + 1);

      return TileHeight;
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

