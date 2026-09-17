using Godot;
namespace VoxelGame.scenes.main.systems.map;
public static class NoiseGenerator {
   // signals
   // exports
   // consts
   // public vars
   // private vars
   // public methods
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
   public static FastNoiseLite MakeCaveNoise(int Seed) {
      FastNoiseLite Noise = new() {
         NoiseType = FastNoiseLite.NoiseTypeEnum.SimplexSmooth,
         FractalType = FastNoiseLite.FractalTypeEnum.Ridged,
         FractalOctaves = 1,
         Seed = Seed,
         Frequency = 0.005F
      };

      return Noise;
   }
   // private methods
}

