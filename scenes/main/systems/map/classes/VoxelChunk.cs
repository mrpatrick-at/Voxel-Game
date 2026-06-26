using Godot;
using Godot.Collections;
using System;
using System.Collections.Generic;
using System.Diagnostics.Metrics;
using System.Linq;
using System.Numerics;
namespace VoxelGame.Chunk;
using VoxelGame.Consts;
[GlobalClass]
public partial class VoxelChunk : MeshInstance3D
{
	// enums
	// consts
	// exports
	// public vars
	public Vector3I Coord {get; set;}
	public Godot.ArrayMesh CubeMesh;
	public int[] Voxels = new int[Consts.Chunk.CubExtendedSize];
	public List<int>[] Faces = new List<int>[6];
	public Godot.Collections.Dictionary GreedyFaces;
	public ShaderMaterial Material = new ShaderMaterial();

	bool is_empty = true;
	bool is_full = true;
	bool has_faces = false;
	// private vars
	// onready vars
	// built-in override methods
		// Called when the node enters the scene tree for the first time.
		public override void _Ready() {
		this.GlobalPosition = new Godot.Vector3(Coord.X << 4, Coord.Y << 4, Coord.Z << 4);
		Material.Shader = GD.Load<Shader>("res://scenes/main/systems/map/shader/VoxelChunk.gdshader");
		this.MaterialOverride = Material;
		}
		
		// Called every frame. 'delta' is the elapsed time since the previous frame.
		public override void _Process(double delta) {

		}
	// public methods

	public void Generate(FastNoiseLite Noise) {
		ulong StartTime = Time.GetTicksUsec();
		GD.PrintRich($"[color=Springgreen]VoxelChunk-[/color] Chunk [color=gold]{Coord}[/color] called setup");

		Voxels = MakeVoxels(Noise);

		if (!is_empty) {
			Faces = MakeFaces();
			if (!is_full) {
				Godot.Collections.Array MeshArray = MakeMesh();
				CubeMesh = new ArrayMesh();
				CubeMesh.AddSurfaceFromArrays(Mesh.PrimitiveType.Triangles, MeshArray);

				ApplyMesh();
			}
		}

		float EndTime = (Godot.Time.GetTicksUsec() - StartTime) / 1000f;
		GD.PrintRich($"[color=Springgreen]VoxelChunk-[/color] Created Chunk in [color=gold]{EndTime}ms[/color]");
	}
	public static Godot.Vector3 IndexToVector3(int index) {
		int y = index / Consts.Chunk.SqExtendedSize;
		int remainder = index % Consts.Chunk.SqExtendedSize;

		int x = remainder % Consts.Chunk.ExtendedSize;
		int z = remainder / Consts.Chunk.ExtendedSize;

		return new Godot.Vector3(x, y, z);
	}
	// private methods
	private int[] MakeVoxels(FastNoiseLite Noise) {
		int[] TMPVoxels = new int[Consts.Chunk.CubExtendedSize];
		for (int x = 0; x < Consts.Chunk.ExtendedSize; x++) {
			for (int z = 0; z < Consts.Chunk.ExtendedSize; z++) {
				float PixelData = -Noise.GetNoise2D(x + Coord.X * Consts.Chunk.Size, z + Coord.Z * Consts.Chunk.Size);
				int TileHeight = (int)((PixelData + 1) * 0.5 * (Consts.World.Height - 1) + 1);
				int LocalTileHeight = Math.Min(TileHeight - Coord.Y * Consts.Chunk.Size, 17);
				// GD.Print($"TileHeight: {TileHeight}, LocalTileHeight: {LocalTileHeight}, Chunk Y: {Coord.Y}");
				for (int y = 0; y <= LocalTileHeight; y++) {
					TMPVoxels[x + z * Consts.Chunk.ExtendedSize + y * Consts.Chunk.SqExtendedSize] = (int)VOXELTYPE.DIRT;
					is_empty = false;
				}
			}
		}
		return TMPVoxels;
	}

	private List<int>[] MakeFaces() {
		List<int>[] TMPFaces = [[], [], [], [], [], []];

		for (int x = 0; x < Consts.Chunk.Size; x++) {
			for (int y = 0; y < Consts.Chunk.Size; y++) {
				for (int z = 0; z < Consts.Chunk.Size; z++) {
					int index = x + z * Consts.Chunk.ExtendedSize + y * Consts.Chunk.SqExtendedSize;
					if (Voxels[index] != 0) {
						if (Voxels[index + 1] == 0) {
							TMPFaces[(int)DIRECTION.RIGHT].Add(index);
						}
						if (Voxels[index + Consts.Chunk.SqExtendedSize] == 0) {
							TMPFaces[(int)DIRECTION.UP].Add(index);
						}
						if (Voxels[index + Consts.Chunk.ExtendedSize] == 0) {
							TMPFaces[(int)DIRECTION.BACK].Add(index);
						}
					} else {
						if (Voxels[index + 1] != 0) {
							TMPFaces[(int)DIRECTION.LEFT].Add(index + 1);
						}
						if (Voxels[index + Consts.Chunk.SqExtendedSize] != 0) {
							TMPFaces[(int)DIRECTION.DOWN].Add(index + Consts.Chunk.SqExtendedSize);
						}
						if (Voxels[index + Consts.Chunk.ExtendedSize] != 0) {
							TMPFaces[(int)DIRECTION.FORWARD].Add(index + Consts.Chunk.ExtendedSize);
						}
						is_full = false;
					}
				}
			}
		}

		return TMPFaces;
	}
	// private int[] MakeGreedyMesh() {
	// 	int[] TMPFaces = new int[6];
	// 	int[] VoxelCopy = (int[])Voxels.Clone();

	// 	for (int y = 0; y < Consts.ChunkSize; y++) {
	// 		for (int x = 0; x < Consts.ChunkSize; x++) {
	// 			for (int z = 0; z < Consts.ChunkSize; z++) {
	// 				int Index = x + z * Consts.ExtendedChunkSize + y * Consts.SqExtendedChunkSize;

	// 				int NeighborIndex1 = Index + 1;
	// 				if (x < Consts.ChunkSize) {
	// 					if (VoxelCopy[Index] != 0 && VoxelCopy[NeighborIndex1] == 0) {

	// 					}
	// 				}
	// 			}
	// 		}

	// 		int x_index = 16 + x;

	// 		if (VoxelCopy[x_index] != 0 && VoxelCopy[x_index + y * Consts.SqExtendedChunkSize] == 0) {
	// 			int StartingIndex = x;

	// 			while (x < 15) {
	// 				VoxelCopy[x_index] = 0;
					
	// 				int next_x_index = x_index + 1;
	// 				if (VoxelCopy[next_x_index] != 0 && VoxelCopy[next_x_index + y * Consts.SqExtendedChunkSize] == 0) {
	// 					x++;
	// 				}
	// 			}
	// 			while (z < 16) {

	// 			}

	// 		}
	// 	}
	// 	return TMPFaces;
	// }
	private Godot.Collections.Array MakeMesh() {

		int FaceAmount = 0;

		for (int dir = 0; dir < 6; dir++) {
			FaceAmount += Faces[dir].Count;
		}

		int VertexSize = FaceAmount * 4;
		Godot.Vector3[] VertexArray = new Godot.Vector3[VertexSize];
		Godot.Vector3[] NormalArray = new Godot.Vector3[VertexSize];
		Godot.Vector2[] UvArray = new Godot.Vector2[VertexSize];
		Godot.Color[] ColorArray = new Godot.Color[VertexSize];

		int IndicesSize = FaceAmount * 6;
		int[] IndicesArray = new int[IndicesSize];

		int Index = 0;

		for (int dir = 0; dir < 6; dir++) {
			List<int> DirList = Faces[dir];

			Color color = Color.Color8(0,255,0);
			if (dir > 3) {
				color = Color.Color8(0,0,255);
			} else if (dir < 2) {
				color = Color.Color8(255,0,0);
			}

			foreach (int IndexInt in DirList) {
				Godot.Vector3 coord = IndexToVector3(IndexInt);
				Godot.Vector3[][] MeshFace = CreateFace(dir,coord,coord);

				int IndexOffset = Index << 2;
				int IndicesIndex = IndexOffset + (Index << 1);

				int i = IndexOffset;

				foreach (Godot.Vector3 vertice in MeshFace[(int)MESH.VERTICES]) {
					VertexArray[i] = vertice;
					NormalArray[i] = vertice;
					UvArray[i] = Godot.Vector2.Zero;
					ColorArray[i] = color;
					i++;
				}
			IndicesArray[IndicesIndex] = IndexOffset;
			IndicesArray[IndicesIndex + 1] = IndexOffset + 1;
			IndicesArray[IndicesIndex + 2] = IndexOffset + 2;
			IndicesArray[IndicesIndex + 3] = IndexOffset;
			IndicesArray[IndicesIndex + 4] = IndexOffset + 2;
			IndicesArray[IndicesIndex + 5] = IndexOffset + 3;
			
			Index++;
			}
		}
		Godot.Collections.Array MeshArray = [];
		MeshArray.Resize((int)Mesh.ArrayType.Max);
		MeshArray[(int)Mesh.ArrayType.Vertex] = VertexArray;
		MeshArray[(int)Mesh.ArrayType.Normal] = NormalArray;
		MeshArray[(int)Mesh.ArrayType.TexUV] = UvArray;
		MeshArray[(int)Mesh.ArrayType.Index] = IndicesArray;
		MeshArray[(int)Mesh.ArrayType.Color] = ColorArray;

		return MeshArray;
	}

	private static Godot.Vector3[][] CreateFace(int dir, Godot.Vector3 StartingPosition, Godot.Vector3 EndingPosition) {
		Godot.Vector3[] DirectionArray = [
			Godot.Vector3.Right,
			Godot.Vector3.Left,
			Godot.Vector3.Up,
			Godot.Vector3.Down,
			Godot.Vector3.Back,
			Godot.Vector3.Forward,
		];
		Godot.Vector3[][] VerticesArray = [
			[
				StartingPosition + new Godot.Vector3(0.5F, -0.5F, -0.5F) * Consts.VoxelSize, // Bottom Left
				new Godot.Vector3(StartingPosition.X,StartingPosition.Y,EndingPosition.Z) + new Godot.Vector3(0.5F, -0.5F,  0.5F) * Consts.VoxelSize, // Bottom Right
				EndingPosition + new Godot.Vector3(0.5F,  0.5F,  0.5F) * Consts.VoxelSize, // Top Right
				new Godot.Vector3(EndingPosition.X,EndingPosition.Y,StartingPosition.Z) + new Godot.Vector3(0.5F,  0.5F, -0.5F) * Consts.VoxelSize, // Top Left
			],
			[
				StartingPosition + new Godot.Vector3(-0.5F, -0.5F, -0.5F) * Consts.VoxelSize, // Bottom Left
				new Godot.Vector3(EndingPosition.X,EndingPosition.Y,StartingPosition.Z) + new Godot.Vector3(-0.5F,  0.5F, -0.5F) * Consts.VoxelSize, // Top Left
				EndingPosition + new Godot.Vector3(-0.5F,  0.5F,  0.5F) * Consts.VoxelSize, // Top Right
				new Godot.Vector3(StartingPosition.X,StartingPosition.Y,EndingPosition.Z) + new Godot.Vector3(-0.5F, -0.5F,  0.5F) * Consts.VoxelSize // Bottom Right
			],
			[
				StartingPosition + new Godot.Vector3(-0.5F,  0.5F, -0.5F) * Consts.VoxelSize,
				new Godot.Vector3(EndingPosition.X,EndingPosition.Y,StartingPosition.Z) + new Godot.Vector3( 0.5F,  0.5F, -0.5F) * Consts.VoxelSize,
				EndingPosition + new Godot.Vector3( 0.5F,  0.5F,  0.5F) * Consts.VoxelSize,
				new Godot.Vector3(StartingPosition.X,StartingPosition.Y,EndingPosition.Z) + new Godot.Vector3(-0.5F,  0.5F,  0.5F) * Consts.VoxelSize
			],
			[
				StartingPosition + new Godot.Vector3(-0.5F, -0.5F,  -0.5F) * Consts.VoxelSize,
				new Godot.Vector3(StartingPosition.X,StartingPosition.Y,EndingPosition.Z) + new Godot.Vector3( -0.5F, -0.5F,  0.5F) * Consts.VoxelSize,
				EndingPosition + new Godot.Vector3( 0.5F, -0.5F, 0.5F) * Consts.VoxelSize,
				new Godot.Vector3(EndingPosition.X,EndingPosition.Y,StartingPosition.Z) + new Godot.Vector3(0.5F, -0.5F, -0.5F) * Consts.VoxelSize
			],
			[
				StartingPosition + new Godot.Vector3(-0.5F, -0.5F, 0.5F) * Consts.VoxelSize, // Bottom Left
				new Godot.Vector3(StartingPosition.X,EndingPosition.Y,StartingPosition.Z) + new Godot.Vector3(-0.5F,  0.5F, 0.5F) * Consts.VoxelSize, // Top Left
				EndingPosition + new Godot.Vector3(0.5F,  0.5F,  0.5F) * Consts.VoxelSize, // Top Right
				new Godot.Vector3(EndingPosition.X,StartingPosition.Y,EndingPosition.Z) + new Godot.Vector3(0.5F, -0.5F,  0.5F) * Consts.VoxelSize // Bottom Right
			],
			[
				StartingPosition + new Godot.Vector3(-0.5F, -0.5F, -0.5F) * Consts.VoxelSize, // Bottom Left
				new Godot.Vector3(EndingPosition.X,StartingPosition.Y,EndingPosition.Z) + new Godot.Vector3(0.5F, -0.5F,  -0.5F) * Consts.VoxelSize, // Bottom Right
				EndingPosition + new Godot.Vector3(0.5F,  0.5F,  -0.5F) * Consts.VoxelSize, // Top Right
				new Godot.Vector3(StartingPosition.X,EndingPosition.Y,StartingPosition.Z) + new Godot.Vector3(-0.5F,  0.5F, -0.5F) * Consts.VoxelSize, // Top Left
			]
		];
		Godot.Vector3[] Vertices = VerticesArray[dir];
		Godot.Vector3 Direction = DirectionArray[dir];
		Godot.Vector3[] normals = [
			Direction, Direction, Direction, Direction
		];

		Godot.Vector3[][] MeshFace = [
			[
				Vertices[0], Vertices[1], Vertices[2], Vertices[3]
			],
			[
				normals[0], normals[1], normals[2], normals[3]
			]
		];
		return MeshFace;
	}
	private void ApplyMesh() {
		this.Mesh = CubeMesh;
	}
}
