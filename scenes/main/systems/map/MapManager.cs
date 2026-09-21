using Godot;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Collections.Concurrent;
using System.Threading;

using VoxelGame.scenes.main.systems.threading;
using System.Diagnostics;

namespace VoxelGame.scenes.main.systems.map;

// enums
public enum ChunkState {
   Generating,
   Pending,
   Rendered,
   Removing
}

[Tool]
public sealed partial class MapManager : Node {
   // Signals
   [Signal] public delegate void NoiseUpdateEventHandler(int Seed, FastNoiseLite Noise);
   // exports
   // consts
   // public vars
   public int RenderDistance = 32;
   public int Seed = 0;
   public NoiseGenerator Noise;
   private ChunkRenderer Renderer = new();
   // Chunk Storage
   private readonly ConcurrentDictionary<Vector2I, Lazy<int[]>> Heightmaps = new();
   private readonly ConcurrentDictionary<Vector3I, Lazy<ChunkData>> DataChunks = new();

   private readonly ConcurrentDictionary<Vector3I, ChunkState> ChunkStates = new();
   private readonly ConcurrentQueue<(Vector3I Coord, ChunkData Data)> PendingChunks = new();
   private readonly ConcurrentQueue<Vector3I> ChunksForRemoval = new();
   // Multithreading (pain)
   private WorkerPool Workers;
   private StreamingWorker<Vector3I> ChunkStreamer;
   int WorkerCount = Math.Min(4, Math.Max(1, System.Environment.ProcessorCount - 3));
   // Player Shi
   public CharacterBody3D Player;
   public Vector3I PlayerChunk = Vector3I.Zero;
   // private vars
   // built-in override methods
   public override void _Ready() {
      GD.Randomize();
      if (!Engine.IsEditorHint()) {
         // PlayerScene = GD.Load<PackedScene>("res://scenes/player/Player.cs");
         // Player = PlayerScene.Instantiate<CharacterBody3D>();
         Player = GetNode<CharacterBody3D>("../Player");

         Workers = new WorkerPool(WorkerCount);

         ChunkStreamer = new StreamingWorker<Vector3I>(
            "Chunk-Streaming",
            UpdateRenderedChunks
        );
      }

      this.AddChild(Renderer);
      MakeMap(true);
   }

   public override void _Process(double delta) {
      if (Engine.IsEditorHint()) {
         return;
      }

      Vector3I NewPlayerChunk = WorldPosToChunkCoord(Player.Position);

      if (NewPlayerChunk != PlayerChunk) { // NOTE: If Player Spawns In Chunk (0, 0, 0) Map Wont Load. FIX LATER
         PlayerChunk = NewPlayerChunk;

         // ChunksInRenderDistance = [.. GetChunkRadius(PlayerChunk, RenderDistance)];

         // UpdateRenderedChunks(NewPlayerChunk);
         ChunkStreamer.Signal(PlayerChunk);
         GD.Print($"World Pos: {Player.Position}, Chunk Pos: {PlayerChunk}");
      }

      const int MaxRemovalsPerFrame = 2;
      const int MaxGenerationsPerFrame = 1;

      for (int i = 0; i < MaxRemovalsPerFrame; i++) {
         if (!ChunksForRemoval.TryDequeue(out var result))
            break;

         Renderer.RemoveChunk(result);
         ChunkStates.Remove(result, out _);
      }

      for (int i = 0; i < MaxGenerationsPerFrame; i++) {
         if (!PendingChunks.TryDequeue(out var result))
            break;

         Renderer.CreateChunk(result.Coord, result.Data.MeshArray);
         ChunkStates[result.Coord] = ChunkState.Rendered;
      }
   }

   public override void _ExitTree() {
      ClearChunks(false);

      ChunkStreamer?.Dispose();
      Workers?.Dispose();
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
      Noise = new NoiseGenerator(Seed);

      if (Engine.IsEditorHint()) {
         for (int x = -RenderDistance; x < RenderDistance; x++) {
            for (int z = -RenderDistance; z < RenderDistance; z++) {
               int[] Heightmap = Noise.MakeChunkHeightmap(new(x, z));

               for (int y = -RenderDistance; y < RenderDistance; y++) {
                  Vector3I ChunkCoord = new(x, y, z);

                  ChunkData Data = GetChunkData(ChunkCoord, Heightmap);
                  Renderer.CreateChunk(ChunkCoord, Data.MeshArray);
               }
            }
         }
      } else {
         // Set Player Pos
         Vector2I SpawnCoord2D = new(GD.RandRange(-1000, 1000), GD.RandRange(-1000, 1000));

         int SpawnHeight = Noise.GetBlockHeight(SpawnCoord2D);

         Vector3 SpawnCoord = new(SpawnCoord2D.X, SpawnHeight, SpawnCoord2D.Y);

         Player.Position = SpawnCoord;

         GD.PrintRich($"[color=Yellow]MapManager-[/color] Spawned Player at: [color=gold]{SpawnCoord}[/color]");
      }

      // EmitSignal(SignalName.NoiseUpdate, Seed, Noise); // For Debug Noise UI


      float EndTime = (Godot.Time.GetTicksUsec() - StartTime) / 1000f;
      GD.PrintRich($"[color=Yellow]MapManager-[/color] Made Map with Render Distance of [color=gold]{RenderDistance}[/color] in [color=gold]{EndTime}ms[/color]");
   }

   // Helpers
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

   public void ClearChunks(bool IsGenrating) {
      if (IsGenrating) {
         DataChunks.Clear();
         Heightmaps.Clear();
      }

      Renderer.Clear();

      GD.PrintRich($"[color=Yellow]MapManager-[/color] Cleared Chunks");
   }

   // Chunk Management
   private void UpdateRenderedChunks(Vector3I CenterChunk) {
      // ulong StartTime = Time.GetTicksUsec();
      HashSet<Vector3I> ChunksInRenderDistance = [.. GetChunkRadius(CenterChunk, RenderDistance)];

      // int epoch = Interlocked.Increment(ref _renderEpoch);

      // Queue Chunks for Removal that are outside RenderDistance
      foreach (Vector3I ChunkCoord in ChunkStates
      .Where(x => x.Value == ChunkState.Rendered)
      .Select(x => x.Key)
      .Where(x => !ChunksInRenderDistance.Contains(x))
      .OrderBy(c => {
         var dx = c.X - CenterChunk.X;
         var dy = c.Y - CenterChunk.Y;
         var dz = c.Z - CenterChunk.Z;

         return dx * dx + dy * dy + dz * dz;
      })) {
         QueueChunkRemoval(ChunkCoord);

         // Renderer.RemoveChunk(ChunkCoord);
         // ChunkStates.Remove(ChunkCoord, out _);
      }

      // Queue Chunk Generation for Chunks in RenderDistance
      foreach (Vector3I ChunkCoord in ChunksInRenderDistance
      .OrderBy(c => c.DistanceSquaredTo(CenterChunk))) {
         if (ChunkStates.TryAdd(ChunkCoord, ChunkState.Generating)) {
            Workers.Enqueue(() => QueueChunkGeneration(ChunkCoord));
         }
      }

      // Unload Unused ChunkData
      foreach (Vector3I ChunkCoord in DataChunks.Keys) {
         if (!ChunksInRenderDistance.Contains(ChunkCoord)) {
            DataChunks.TryRemove(ChunkCoord, out _);
         }
      }

      // Unload Unused Heightmaps
      foreach (Vector2I SliceCoord in Heightmaps.Keys) {
         if (!ChunksInRenderDistance.Contains(new(SliceCoord.X, PlayerChunk.Y, SliceCoord.Y))) {
            Heightmaps.TryRemove(SliceCoord, out _);
         }
      }

      // float EndTime = (Godot.Time.GetTicksUsec() - StartTime) / 1000f;
      // GD.PrintRich($"[color=Yellow]MapManager-[/color] UpdateRenderedChunks took [color=gold]{EndTime}[/color]s");
   }

   // Chunk Queueing
   private void QueueChunkRemoval(Vector3I ChunkCoord) {
      ChunkStates[ChunkCoord] = ChunkState.Removing;
      ChunksForRemoval.Enqueue(ChunkCoord);
   }

   private void QueueChunkGeneration(Vector3I ChunkCoord) {
      try {
         // Chunk no Longer Wanted
         // if (!ChunksInRenderDistance.Contains(ChunkCoord)) {
         //    ChunkStates.TryRemove(ChunkCoord, out _);
         //    return;
         // }

         int[] Heightmap = GetChunkHeightmap(new(ChunkCoord.X, ChunkCoord.Z));

         // Player may have moved while generating the Heightmap.
         // if (!ChunksInRenderDistance.Contains(ChunkCoord)) {
         //    ChunkStates.TryRemove(ChunkCoord, out _);
         //    return;
         // }

         ChunkData Data = GetChunkData(ChunkCoord, Heightmap);

         // Nothing to render, so don't leave the chunk in a pending state.
         if (!Data.HasFaces) {
            ChunkStates.TryRemove(ChunkCoord, out _);
            return;
         }

         PendingChunks.Enqueue((ChunkCoord, Data));
         ChunkStates[ChunkCoord] = ChunkState.Pending;
      }
      catch (Exception err) {
         ChunkStates.TryRemove(ChunkCoord, out _);

         GD.PrintErr($"Failed to Queue Chunk Generation for {ChunkCoord}: {err}");
      }
   }

   // Chunk Storage Management
   private int[] GetChunkHeightmap(Vector2I SliceCoord) {
      Lazy<int[]> LazyData = Heightmaps.GetOrAdd(
         SliceCoord,
         Coord => new Lazy<int[]>(
           () => Noise.MakeChunkHeightmap(SliceCoord),
           LazyThreadSafetyMode.ExecutionAndPublication
         ));

      return LazyData.Value;
   }

   private ChunkData GetChunkData(Vector3I ChunkCoord, int[] Heightmap) {
      Lazy<ChunkData> LazyData = DataChunks.GetOrAdd(
         ChunkCoord,
         Coord => new Lazy<ChunkData>(
           () => ChunkGenerator.MakeChunkData(Coord, Heightmap),
           LazyThreadSafetyMode.ExecutionAndPublication
         ));

      return LazyData.Value;
   }
}

