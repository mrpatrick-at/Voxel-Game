using Godot;
using Godot.Collections;
using System;
using System.Collections.Generic;
using System.Linq;
namespace VoxelGame.MapManager;
using VoxelGame.Consts;
using VoxelGame.Chunk;
using VoxelGame.ChunkGenerator;
[Tool]
// enums
public partial class MapManager : Node {
    // Signals
    [Signal] public delegate void NoiseUpdateEventHandler(int Seed, FastNoiseLite Noise);
    // exports
    [Export] public int RenderDistance = 4;
    // consts
    // public vars
    public int Seed = 0;
    public FastNoiseLite Noise = new();
    public System.Collections.Generic.Dictionary<Vector3I, ChunkData> DataChunks = [];
    public System.Collections.Generic.Dictionary<Vector3I, VoxelChunk> VoxelChunks = [];
    public Queue<VoxelChunk> IdleChunks = [];
    public CharacterBody3D Player;
    public Vector3I CurrentPlayerChunk = Vector3I.Zero;
    // private vars
    // built-in override methods
    // Called when the node enters the scene tree for the first time.
    public override void _Ready() {
        GD.Randomize();
        if (Engine.IsEditorHint()) {
            MakeMap(true);
        } else {
            Player = GetNode<CharacterBody3D>("../Player");
            UpdateRenderedChunks(CurrentPlayerChunk);
        }

    }

    // Called every frame. 'delta' is the elapsed time since the previous frame.
    public override void _Process(double delta) {
        if (Engine.IsEditorHint()) {
            return;
        }
        Vector3I NewPlayerChunk = WorldPosToChunkCoord(Player.Position);
        if (NewPlayerChunk != CurrentPlayerChunk) {
            CurrentPlayerChunk = NewPlayerChunk;
            UpdateRenderedChunks(NewPlayerChunk);
        }
        // GD.Print($"World Pos: {Player.Position}, Chunk Pos: {NewPlayerChunk}");
    }
    public override void _ExitTree() {
        ClearChunks(true);
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

        Seed = (int)GD.Randi();
        Noise = MakeNoise();

        EmitSignal(SignalName.NoiseUpdate, Seed, Noise);

        float PreChunkTime = (Godot.Time.GetTicksUsec() - StartTime) / 1000f;
        GD.PrintRich($"[color=Yellow]MapManager-[/color] Finished Pre Chunk Operations in [color=gold]{PreChunkTime}ms[/color]");

        for (int x = 0; x < Consts.World.ChunkLength; x++) {
            for (int z = 0; z < Consts.World.ChunkWidth; z++) {
                for (int y = 0; y < Consts.World.ChunkHeight; y++) {
                    Vector3I ChunkCoord = new(x, y, z);

                    VoxelChunk Chunk = LoadChunk(ChunkCoord);
                }
            }
        }

        float EndTime = (Godot.Time.GetTicksUsec() - StartTime) / 1000f;
        GD.PrintRich($"[color=Yellow]MapManager-[/color] Created Map of size [color=gold]{new Vector3I(Consts.World.ChunkLength, Consts.World.ChunkHeight, Consts.World.ChunkWidth)}[/color] in [color=gold]{EndTime}ms[/color]");
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
        int ChunkAmount = VoxelChunks.Values.Count;
        if (IsGenrating) {
            DataChunks.Clear();
        }

        foreach (VoxelChunk Chunk in VoxelChunks.Values) {
            this.RemoveChild(Chunk);
            Chunk.QueueFree();
        }

        if (IdleChunks.Count > 0) {
            foreach (VoxelChunk Chunk in IdleChunks) {
                this.RemoveChild(Chunk);
                Chunk.QueueFree();
            }
            IdleChunks.Clear();
        }

        VoxelChunks.Clear();

        GD.PrintRich($"[color=Yellow]MapManager-[/color] Deleted [color=gold]{ChunkAmount}[/color] children");
    }
    private ChunkData GetChunkData(FastNoiseLite Noise, Vector3I ChunkCoord) {
        ChunkData Data;
        if (DataChunks.ContainsKey(ChunkCoord)) {
            Data = DataChunks[ChunkCoord];
        } else {
            Data = ChunkGenerator.MakeChunkData(Noise, ChunkCoord);
            DataChunks[ChunkCoord] = Data;
        }
        return Data;
    }
    private VoxelChunk LoadChunk(Vector3I ChunkCoord) {
        if (VoxelChunks.ContainsKey(ChunkCoord)) {
            GD.PrintErr($"ERROR LOADING CHUNK! Chunk {ChunkCoord} is already loaded");
            return VoxelChunks[ChunkCoord];
        }

        ChunkData Data = GetChunkData(Noise, ChunkCoord);

        if (IdleChunks.Count > 0) {
            // Repurpose Unloaded Chunk Node
            VoxelChunk Chunk = IdleChunks.Dequeue();

            Chunk.SetChunkData(ChunkCoord, Data.CubeMesh, Data.Triangles, Data.HasFaces);

            VoxelChunks[ChunkCoord] = Chunk;
            Chunk.Reload();
            return Chunk;
        } else {
            VoxelChunk Chunk = new() {
                Coord = ChunkCoord,
                CubeMesh = Data.CubeMesh,
                Triangles = Data.Triangles,
                HasFaces = Data.HasFaces,
            };
            VoxelChunks[ChunkCoord] = Chunk;
            this.AddChild(Chunk);
            Chunk.Reload();
            return Chunk;
        }
    }
    private void UnloadChunk(Vector3I ChunkCoord) {
        if (!VoxelChunks.ContainsKey(ChunkCoord)) {
            GD.PrintErr($"ERROR UNLOADING CHUNK! Chunk {ChunkCoord} is not Loaded");
        } else {
            VoxelChunk Chunk = VoxelChunks[ChunkCoord];
            // Chunk.Coord = Vector3I.Zero;
            // Chunk.CubeMesh = new ArrayMesh();
            // Chunk.Triangles = null;
            // Chunk.HasFaces = false;
            VoxelChunks.Remove(ChunkCoord);
            IdleChunks.Enqueue(Chunk);
            // Chunk.Reload();
        }
    }
    private void UpdateRenderedChunks(Vector3I CenterChunk) {
        HashSet<Vector3I> ChunksInRenderDistance = [.. GetChunkRadius(CenterChunk, RenderDistance)];

        // Unload Chunks out of RenderDistance
        foreach (Vector3I ChunkCoord in VoxelChunks.Keys) {
            if (!ChunksInRenderDistance.Contains(ChunkCoord)) {
                UnloadChunk(ChunkCoord);
            }
        }

        // Load Chunks in RenderDistance""
        foreach (Vector3I ChunkCoord in ChunksInRenderDistance) {
            if (!VoxelChunks.ContainsKey(ChunkCoord)) {
                LoadChunk(ChunkCoord);
            }
        }
    }

    private FastNoiseLite MakeNoise() {
        FastNoiseLite TmpNoise = new() {
            NoiseType = FastNoiseLite.NoiseTypeEnum.SimplexSmooth,
            FractalType = FastNoiseLite.FractalTypeEnum.Ridged,
            FractalOctaves = 1,
            Seed = Seed,
            Frequency = 0.0025F
        };

        return TmpNoise;
    }
}

