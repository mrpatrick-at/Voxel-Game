using Godot;
using Godot.Collections;
using System;
using System.Collections.Generic;
using System.Linq;
namespace VoxelGame.MapManager;
using VoxelGame.Chunk;
using VoxelGame.Consts;
[Tool]
public partial class MapManager : Node
{
	// enums
	// consts
	// exports
	// public vars
	public int Seed = 0;
	public FastNoiseLite Noise = new();
	public System.Collections.Generic.Dictionary<Vector3I, VoxelChunk> Chunks = new();
	// private vars
	// onready vars
	// built-in override methods
		// Called when the node enters the scene tree for the first time.
		public override void _Ready() {
			GD.Randomize();
			// if (Engine.IsEditorHint()) {
				MakeMap(true);
			// }

		}
		
		// Called every frame. 'delta' is the elapsed time since the previous frame.
		public override void _Process(double delta) {
			
		}

		public void OnGeneratePressed() {

		}
	// public methods
	public void MakeMap(bool IsEditor) {
		ulong StartTime = Time.GetTicksUsec();
		GD.PrintRich($"[color=Yellow]MapManager-[/color] Started making Map");

		ClearChildren();
		
		Seed = (int)GD.Randi();
		Noise = MakeNoise();


		float PreChunkTime = (Godot.Time.GetTicksUsec() - StartTime) / 1000f;
		GD.PrintRich($"[color=Yellow]MapManager-[/color] Finished Pre Chunk Operations in [color=gold]{PreChunkTime}ms[/color]");

		for (int x = 0; x < Consts.World.ChunkLength; x++) {
			for (int z = 0; z < Consts.World.ChunkWidth; z++) {
				for (int y = 0; y < Consts.World.ChunkHeight; y++) {
					Vector3I ChunkCoord = new(x,y,z);
					VoxelChunk Chunk = new() {Coord = ChunkCoord};
					this.AddChild(Chunk);
					Chunk.Generate(Noise);

					Chunks[ChunkCoord] = Chunk;
				}
			}
		}

		float EndTime = (Godot.Time.GetTicksUsec() - StartTime) / 1000f;
		GD.PrintRich($"[color=Yellow]MapManager-[/color] Created Map of size [color=gold]{new Vector3I(Consts.World.ChunkLength,Consts.World.ChunkHeight,Consts.World.ChunkWidth)}[/color] in [color=gold]{EndTime}ms[/color]");
	}
	// private methods
	private void ClearChildren() {
		VoxelChunk[] Children = this.GetChildren().OfType<VoxelChunk>().ToArray();
		foreach (VoxelChunk Child in Children) {
			this.RemoveChild(Child);
			Child.QueueFree();
		}
		GD.PrintRich($"[color=Yellow]MapManager-[/color] Deleted [color=gold]{Children.Length}[/color] children");
	}
	private FastNoiseLite MakeNoise() {
		FastNoiseLite TmpNoise = new FastNoiseLite();
		TmpNoise.NoiseType = FastNoiseLite.NoiseTypeEnum.SimplexSmooth;
		TmpNoise.FractalType = FastNoiseLite.FractalTypeEnum.Ridged;
		TmpNoise.FractalOctaves = 1;
		TmpNoise.Seed = Seed;
		TmpNoise.Frequency = 0.0025F;

		return TmpNoise;
	}
}

