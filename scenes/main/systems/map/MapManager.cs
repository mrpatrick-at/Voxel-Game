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
   private readonly ConcurrentDictionary<Vector3I, ChunkState> ChunkStates = new();
   private readonly ConcurrentQueue<(Vector3I Coord, ChunkData Data)> PendingChunks = new();
   private readonly ConcurrentQueue<Vector3I> ChunksForRemoval = new();
   // Multithreading (pain)
   private readonly ConcurrentQueue<Vector3I> GenerationQueue = new();
   private readonly List<Thread> GenerationWorkers = [];
   private readonly CancellationTokenSource Cancellation = new();
   private readonly SemaphoreSlim GenerationSignal = new(0);
   int WorkerCount = Math.Max(1, System.Environment.ProcessorCount - 2);
   // Player Shi
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

         for (int i = 0; i < WorkerCount; i++) {
            Thread Worker = new(GenerationWorker);
            GenerationWorkers.Add(Worker);
            Worker.Start();
         }
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

      // Process Chunks Queued for Removal
      while (ChunksForRemoval.TryDequeue(out var Result)) {
         Renderer.RemoveChunk(Result);
         ChunkStates.Remove(Result, out _);
      }

      // Process Chunks Queued for Generation
      while (PendingChunks.TryDequeue(out var Result)) {
         Renderer.CreateChunk(Result.Coord, Result.Data.MeshArray);
         ChunkStates[Result.Coord] = ChunkState.Rendered;
      }
   }

   public override void _ExitTree() {
      Cancellation.Cancel();

      // Wake every worker so they can observe cancellation.
      for (int i = 0; i < GenerationWorkers.Count; i++)
         GenerationSignal.Release();

      foreach (Thread Worker in GenerationWorkers)
         Worker.Join();

      GenerationWorkers.Clear();

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
                  Renderer.CreateChunk(ChunkCoord, Data.MeshArray);
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

      // Queue Chunks for Removal that are outside RenderDistance
      foreach (Vector3I ChunkCoord in ChunkStates
      .Where(x => x.Value == ChunkState.Rendered)
      .Select(x => x.Key)
      .ToList()) {
         if (!ChunksInRenderDistance.Contains(ChunkCoord)) {

            QueueChunkRemoval(ChunkCoord);

            // Renderer.RemoveChunk(ChunkCoord);
            // ChunkStates.Remove(ChunkCoord, out _);
         }
      }

      // Queue Chunk Generation for Chunks in RenderDistance
      foreach (Vector3I ChunkCoord in ChunksInRenderDistance) { // IMPORTANT: MEMORY LEAK FOUND HERE
         if (ChunkStates.TryAdd(ChunkCoord, ChunkState.Generating)) {
            GenerationQueue.Enqueue(ChunkCoord);
            GenerationSignal.Release();
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
      ChunkStates[ChunkCoord] = ChunkState.Removing;
      ChunksForRemoval.Enqueue(ChunkCoord);
   }
   private void QueueChunkGeneration(Vector3I ChunkCoord) {
      try {
         ChunkData Data = GetChunkData(ChunkCoord, Noise);

         if (Data.HasFaces) {
            PendingChunks.Enqueue((ChunkCoord, Data));
         }

         ChunkStates[ChunkCoord] = ChunkState.Generated;

      }
      catch (Exception err) {
         ChunkStates.TryRemove(ChunkCoord, out _);

         GD.PrintErr($"Failed to Queue Chunk Generation for {ChunkCoord}: {err}");
      }
   }
   private void GenerationWorker() {
      try {
         while (true) {
            GenerationSignal.Wait(Cancellation.Token);

            if (!GenerationQueue.TryDequeue(out Vector3I Coord))
               continue;

            QueueChunkGeneration(Coord);
         }
      }
      catch (OperationCanceledException) {
         // Normal shutdown.
      }
   }
}

