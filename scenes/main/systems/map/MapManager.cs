using Godot;
using Godot.Collections;
using System;
using System.Collections.Generic;
using System.Linq;
namespace VoxelGame.MapManager;
using VoxelGame.Consts;
using VoxelGame.Chunk;
using VoxelGame.ChunkGenerator;
using VoxelGame.ChunkRenderer;
using VoxelGame.NoiseGenerator;
using System.Threading.Tasks;
using System.Collections.Concurrent;
using System.Threading;

// enums
public enum ChunkState {
   Queued,
   Generating,
   Generated,
   Rendering,
   Rendered,
   Removing
}
[Tool]
public partial class MapManager : Node {
   // Signals
   [Signal] public delegate void NoiseUpdateEventHandler(int Seed, FastNoiseLite Noise);
   // exports
   [Export] public int RenderDistance = 8;
   // consts
   // public vars
   public int Seed = 0;
   public FastNoiseLite Noise = new();
   private ChunkRenderer Renderer = new();
   // Chunk Storage
   private readonly ConcurrentDictionary<Vector3I, Lazy<ChunkData>> DataChunks = new();
   private readonly ConcurrentDictionary<Vector3I, ChunkState> ChunkStates = new(); // bool rn does nothing :)
   private readonly ConcurrentQueue<(Vector3I Coord, ChunkData Data)> PendingChunks = new();
   private readonly ConcurrentQueue<Vector3I> ChunksForRemoval = new();
   // public PackedScene PlayerScene;
   public CharacterBody3D Player;
   public Vector3I CurrentPlayerChunk = Vector3I.Zero;
   // private vars
   // built-in override methods
   public override void _Ready() {
      GD.Randomize();
      if (!Engine.IsEditorHint()) {
         // PlayerScene = GD.Load<PackedScene>("res://scenes/player/Player.cs");
         // Player = PlayerScene.Instantiate<CharacterBody3D>();
         Player = GetNode<CharacterBody3D>("../Player");
      }
      this.AddChild(Renderer);
      MakeMap(true);
   }

   public override void _Process(double delta) {
      if (Engine.IsEditorHint()) {
         return;
      }

      Vector3I NewPlayerChunk = WorldPosToChunkCoord(Player.Position);

      if (NewPlayerChunk != CurrentPlayerChunk) { // NOTE: If Player Spawns In Chunk (0, 0, 0) Map Wont Load. FIX LATER
         CurrentPlayerChunk = NewPlayerChunk;
         UpdateRenderedChunks(NewPlayerChunk);
         GD.Print($"World Pos: {Player.Position}, Chunk Pos: {NewPlayerChunk}");
      }

      // // Process Chunks Queued for Removal
      // while (ChunksForRemoval.TryDequeue(out var Result)) {
      //    Renderer.RemoveChunk(Result);
      //    ChunkStates.Remove(Result, out _);
      // }

      // Process Chunks Queued for Generation
      while (PendingChunks.TryDequeue(out var Result)) {
         ArrayMesh CubeMesh = ChunkGenerator.MakeCubeMesh(Result.Data.MeshArray);
         Renderer.CreateChunk(Result.Coord, CubeMesh);
         ChunkStates[Result.Coord] = ChunkState.Rendered;
      }
   }

   public override void _ExitTree() {
      ClearChunks(false);
   }

   public void _OnGeneratePressed() {
      MakeMap(true);
   }

   public void _OnLoadPressed() {
      MakeMap(false);
   }

   // public methods
   public void MakeMap(bool IsGenrating) {
      ulong StartTime = Time.GetTicksUsec();
      GD.PrintRich($"[color=Yellow]MapManager-[/color] Started making Map");

      ClearChunks(IsGenrating);

      // Make Noise
      Seed = (int)GD.Randi();
      Noise = NoiseGenerator.MakeHillsNoise(Seed);

      if (Engine.IsEditorHint()) {
         for (int x = -RenderDistance; x < RenderDistance; x++) {
            for (int z = -RenderDistance; z < RenderDistance; z++) {
               for (int y = -RenderDistance; y < RenderDistance; y++) {
                  Vector3I ChunkCoord = new(x, y, z);

                  ChunkData Data = GetChunkData(ChunkCoord, Noise);
                  ArrayMesh CubeMesh = ChunkGenerator.MakeCubeMesh(Data.MeshArray);
                  Renderer.CreateChunk(ChunkCoord, CubeMesh);
               }
            }
         }
      } else {
         // Set Player Pos
         Vector2I SpawnCoord2D = new(GD.RandRange(-1000, 1000), GD.RandRange(-1000, 1000));

         float PixelData = -Noise.GetNoise2Dv(SpawnCoord2D);
         int SpawnHeight = (int)((PixelData + 1) * 0.5 * (Consts.World.Height - 1) + 1);

         Player.Position = new Vector3(SpawnCoord2D.X, SpawnHeight, SpawnCoord2D.Y);

         Vector3 SpawnCoord = new(SpawnCoord2D.X, SpawnHeight, SpawnCoord2D.Y);
         GD.PrintRich($"[color=Yellow]MapManager-[/color] Spawned Player at: [color=gold]{SpawnCoord}[/color]");
      }

      EmitSignal(SignalName.NoiseUpdate, Seed, Noise); // For Debug Noise UI


      float EndTime = (Godot.Time.GetTicksUsec() - StartTime) / 1000f;
      GD.PrintRich($"[color=Yellow]MapManager-[/color] Made Map with Render Distance of [color=gold]{RenderDistance}[/color] in [color=gold]{EndTime}ms[/color]");
   }
   public static Vector3I WorldPosToChunkCoord(Vector3 Position) {
      return new Vector3I(
          (int)Mathf.Floor((Position.X + 0.5) / Consts.Chunk.Size),
          (int)Mathf.Floor((Position.Y + 0.5) / Consts.Chunk.Size),
          (int)Mathf.Floor((Position.Z + 0.5) / Consts.Chunk.Size)
          );
   }
   public static IEnumerable<Vector3I> GetChunkRadius(Vector3I CenterChunk, int Radius) {
      for (int x = -Radius; x <= Radius; x++) {
         for (int y = -Radius; y <= Radius; y++) {
            for (int z = -Radius; z <= Radius; z++) {
               yield return CenterChunk + new Vector3I(x, y, z);
            }
         }
      }
   }
   // private methods
   private void ClearChunks(bool IsGenrating) {
      if (IsGenrating) {
         DataChunks.Clear();
      }

      Renderer.Clear();

      GD.PrintRich($"[color=Yellow]MapManager-[/color] Cleared Chunks");
   }
   private void UpdateRenderedChunks(Vector3I CenterChunk) {
      ulong StartTime = Time.GetTicksUsec();
      HashSet<Vector3I> ChunksInRenderDistance = [.. GetChunkRadius(CenterChunk, RenderDistance)];

      // Queue Chunk Removal for Chunks outside RenderDistance
      foreach (Vector3I ChunkCoord in Renderer.RenderedChunks.Keys) {
         if (!ChunksInRenderDistance.Contains(ChunkCoord)) {
            // WorkerThreadPool.AddTask(
            // Callable.From(() => QueueChunkRemoval(ChunkCoord))
            // );
            Renderer.RemoveChunk(ChunkCoord);
            ChunkStates.Remove(ChunkCoord, out _);
         }
      }

      // Queue Chunk Generation for Chunks in RenderDistance
      foreach (Vector3I ChunkCoord in ChunksInRenderDistance) {
         if (!ChunkStates.ContainsKey(ChunkCoord)) {
            WorkerThreadPool.AddTask(
            Callable.From(() => QueueChunkGeneration(ChunkCoord))
            );
         }
      }

      // Unload Unused ChunkData
      foreach (Vector3I ChunkCoord in DataChunks.Keys) {
         if (!ChunksInRenderDistance.Contains(ChunkCoord)) {
            DataChunks.TryRemove(ChunkCoord, out _);
         }
      }

      float EndTime = (Godot.Time.GetTicksUsec() - StartTime) / 1000f;
      GD.PrintRich($"[color=Yellow]MapManager-[/color] UpdateRenderedChunks took [color=gold]{EndTime}[/color]s");
   }

   private ChunkData GetChunkData(Vector3I ChunkCoord, FastNoiseLite Noise) {
      Lazy<ChunkData> LazyData = DataChunks.GetOrAdd(
         ChunkCoord,
         Coord => new Lazy<ChunkData>(
           () => ChunkGenerator.MakeChunkData(Coord, Noise),
           LazyThreadSafetyMode.ExecutionAndPublication
         ));

      return LazyData.Value;
   }
   // Async shi
   private void QueueChunkRemoval(Vector3I ChunkCoord) {
      try {
         ChunkStates[ChunkCoord] = ChunkState.Removing;
         ChunksForRemoval.Enqueue(ChunkCoord);
      }
      catch (Exception err) {
         GD.PrintErr($"Failed to Queue Chunk Removal for {ChunkCoord}: {err}");
      }
   }
   private void QueueChunkGeneration(Vector3I ChunkCoord) {
      try {
         ChunkStates[ChunkCoord] = ChunkState.Generating;
         // GD.Print($"Coord: {ChunkCoord} ChunkState: {ChunkStates[ChunkCoord]}");
         ChunkData Data = GetChunkData(ChunkCoord, Noise);

         if (Data.HasFaces) {
            ChunkStates[ChunkCoord] = ChunkState.Generated;
            PendingChunks.Enqueue((ChunkCoord, Data));
         }
      }
      catch (Exception err) {
         ChunkStates.TryRemove(ChunkCoord, out _);

         GD.PrintErr($"Failed to Queue Chunk Generation for {ChunkCoord}: {err}");
      }
   }
}

