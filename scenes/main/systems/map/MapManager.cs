using Godot;
using Godot.Collections;
using System;
using System.Collections.Generic;
using System.Linq;
namespace VoxelGame.MapManager;
using VoxelGame.Consts;
using VoxelGame.Chunk;
using VoxelGame.ChunkGenerator;
using VoxelGame.NoiseGenerator;
using System.Threading.Tasks;
using System.Collections.Concurrent;
using System.Threading;

[Tool]
// enums
public partial class MapManager : Node {
    // Signals
    [Signal] public delegate void NoiseUpdateEventHandler(int Seed, FastNoiseLite Noise);
    // exports
    [Export] public int RenderDistance = 8;
    // consts
    // public vars
    public int Seed = 0;
    public FastNoiseLite Noise = new();
    // public System.Collections.Generic.Dictionary<Vector3I, ChunkData> DataChunks = [];
    private readonly ConcurrentDictionary<Vector3I, Lazy<ChunkData>> DataChunks = new();
    public System.Collections.Generic.Dictionary<Vector3I, VoxelChunk> VoxelChunks = [];
    public Queue<VoxelChunk> IdleChunks = [];
    private readonly ConcurrentQueue<(Vector3I Coord, ChunkData Data)> PendingChunks = new();
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
        }

        // Process Pending Chunks
        while (PendingChunks.TryDequeue(out var Result)) {
            ApplyChunkData(Result.Coord, Result.Data);
        }
        // GD.Print($"World Pos: {Player.Position}, Chunk Pos: {NewPlayerChunk}");
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

                        ChunkData Data = GetChunkData(Noise, ChunkCoord);
                        ApplyChunkData(ChunkCoord, Data);
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

        EmitSignal(SignalName.NoiseUpdate, Seed, Noise); // I forgor why I added this


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
        int ChunkAmount = VoxelChunks.Values.Count + IdleChunks.Count;
        if (IsGenrating) {
            DataChunks.Clear();
        }

        foreach (VoxelChunk Chunk in VoxelChunks.Values) {
            this.RemoveChild(Chunk);
            Chunk.QueueFree();
        }

        if (IdleChunks.Count > 0) {
            foreach (VoxelChunk Chunk in IdleChunks) {
                if (Chunk.GetParent() == this) {
                    RemoveChild(Chunk);
                }
                Chunk.QueueFree();
            }
            IdleChunks.Clear();
        }

        VoxelChunks.Clear();

        GD.PrintRich($"[color=Yellow]MapManager-[/color] Deleted [color=gold]{ChunkAmount}[/color] Chunks");
    }
    private void UpdateRenderedChunks(Vector3I CenterChunk) {
        ulong StartTime = Time.GetTicksUsec();
        HashSet<Vector3I> ChunksInRenderDistance = [.. GetChunkRadius(CenterChunk, RenderDistance)];

        // Mark Chunks out of RenderDistance as Idle
        foreach (Vector3I ChunkCoord in VoxelChunks.Keys) {
            if (!ChunksInRenderDistance.Contains(ChunkCoord)) {
                MarkChunkIdle(ChunkCoord);
            }
        }

        // Load Chunks in RenderDistance
        foreach (Vector3I ChunkCoord in ChunksInRenderDistance) {
            if (!VoxelChunks.ContainsKey(ChunkCoord)) {
                WorkerThreadPool.AddTask(
                    Callable.From(() => LoadChunk(ChunkCoord))
                );
            }
        }

        foreach (Vector3I ChunkCoord in DataChunks.Keys) {
            if (!ChunksInRenderDistance.Contains(ChunkCoord)) {
                DataChunks.TryRemove(ChunkCoord, out _);
            }
        }
        float EndTime = (Godot.Time.GetTicksUsec() - StartTime) / 1000f;
        GD.PrintRich($"[color=Yellow]MapManager-[/color] UpdateRenderedChunks took [color=gold]{EndTime}[/color]s");
    }
    private void MarkChunkIdle(Vector3I ChunkCoord) {
        if (!VoxelChunks.TryGetValue(ChunkCoord, out VoxelChunk Chunk)) {
            GD.PrintErr($"ERROR UNLOADING CHUNK! Chunk {ChunkCoord} is not Loaded");
        } else {
            VoxelChunks.Remove(ChunkCoord);
            IdleChunks.Enqueue(Chunk);
            this.RemoveChild(Chunk);
        }
    }
    private void LoadChunk(Vector3I ChunkCoord) {
        try {
            ChunkData Data = GetChunkData(Noise, ChunkCoord);

            PendingChunks.Enqueue((ChunkCoord, Data));
        }
        catch (Exception err) {
            GD.PrintErr($"Failed generating chunk {ChunkCoord}: {err}");
        }
    }
    private void ApplyChunkData(Vector3I ChunkCoord, ChunkData Data) {
        if (VoxelChunks.ContainsKey(ChunkCoord)) {
            GD.PrintErr($"ERROR LOADING CHUNK! Chunk {ChunkCoord} is already loaded");
            return;
        }

        if (Data.HasFaces) {
            VoxelChunk Chunk;

            if (IdleChunks.Count > 0) {
                Chunk = IdleChunks.Dequeue();

                Chunk.SetChunkData(ChunkCoord, Data.CubeMesh, Data.Triangles, Data.HasFaces);

            } else {
                Chunk = new() {
                    Coord = ChunkCoord,
                    CubeMesh = Data.CubeMesh,
                    Triangles = Data.Triangles,
                    HasFaces = Data.HasFaces,
                };
            }
            this.AddChild(Chunk);
            VoxelChunks[ChunkCoord] = Chunk;
            Chunk.Reload();
        }
    }
    private ChunkData GetChunkData(FastNoiseLite Noise, Vector3I ChunkCoord) {
        Lazy<ChunkData> lazyData = DataChunks.GetOrAdd(
            ChunkCoord,
            Coord => new Lazy<ChunkData>(
                () => ChunkGenerator.MakeChunkData(Noise, Coord),
                LazyThreadSafetyMode.ExecutionAndPublication
            )
        );

        return lazyData.Value;
    }
}

