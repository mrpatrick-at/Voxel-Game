using Godot;
using Godot.Collections;
using System;
using System.Collections.Generic;
using System.Linq;
namespace VoxelGame.MapManager;
using VoxelGame.Consts;
using VoxelGame.Chunk;
[Tool]
// enums
public partial class MapManager : Node {
	// Signals
	[Signal]
    public delegate void NoiseUpdateEventHandler(int Seed, FastNoiseLite Noise);
	// exports
	// consts
	// public vars
	public int Seed = 0;
	public FastNoiseLite Noise = new();
	public System.Collections.Generic.Dictionary<Vector3I, ChunkData> DataChunks = [];
	public System.Collections.Generic.Dictionary<Vector3I, VoxelChunk> VoxelChunks = [];
	// private vars
	// built-in override methods
		// Called when the node enters the scene tree for the first time.
		public override void _Ready() {
			GD.Randomize();
			if (Engine.IsEditorHint()) {
				MakeMap(true);
			}

		}
		
		// Called every frame. 'delta' is the elapsed time since the previous frame.
		public override void _Process(double delta) {
			
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

        // ShaderMaterial ChunkMaterial = new(){
        //     Shader = GD.Load<Shader>("res://scenes/main/systems/map/shader/VoxelChunk.gdshader")
        // };
        // Texture2D TextureAtlas = GD.Load<Texture2D>("res://assets/textures/TextureAtlas.png");
		// (ChunkMaterial as ShaderMaterial).SetShaderParameter("TextureAtlas", TextureAtlas);

		for (int x = 0; x < Consts.World.ChunkLength; x++) {
			for (int z = 0; z < Consts.World.ChunkWidth; z++) {
				for (int y = 0; y < Consts.World.ChunkHeight; y++) {
					Vector3I ChunkCoord = new(x,y,z);
					
					MakeChunk(Noise, ChunkCoord);
				}
			}
		}

		float EndTime = (Godot.Time.GetTicksUsec() - StartTime) / 1000f;
		GD.PrintRich($"[color=Yellow]MapManager-[/color] Created Map of size [color=gold]{new Vector3I(Consts.World.ChunkLength,Consts.World.ChunkHeight,Consts.World.ChunkWidth)}[/color] in [color=gold]{EndTime}ms[/color]");
	}
	public void MakeChunk(FastNoiseLite Noise, Vector3I ChunkCoord) {
		ChunkData Data;
		if (DataChunks.ContainsKey(ChunkCoord)) {
			Data = DataChunks[ChunkCoord];
		} else {
			Data = MakeChunkData.Generate(Noise, ChunkCoord);
			DataChunks[ChunkCoord] = Data;
		}

		VoxelChunk Chunk = new() {
			Coord = ChunkCoord,
			CubeMesh = Data.CubeMesh,
			StaticBody = Data.StaticBody,
			HasFaces = Data.HasFaces,
		};

		this.AddChild(Chunk);
		VoxelChunks[ChunkCoord] = Chunk;
	}
	// private methods
	private void ClearChunks(bool IsGenrating) {
		VoxelChunk[] Children = [.. this.GetChildren().OfType<VoxelChunk>()];
		foreach (VoxelChunk Child in Children) {
			Child.Delete(IsGenrating);
			this.RemoveChild(Child);
			Child.QueueFree();
		}
		VoxelChunks = [];

		if (IsGenrating) {
			DataChunks = [];
		}
		
		GD.PrintRich($"[color=Yellow]MapManager-[/color] Deleted [color=gold]{Children.Length}[/color] children");
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

