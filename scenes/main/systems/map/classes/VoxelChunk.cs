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
// enums
public partial class VoxelChunk : MeshInstance3D {
	// signals
	// exports
	// consts
	// public vars
	public Vector3I Coord {get; set;}
	public Godot.ArrayMesh CubeMesh;
	public int[] Voxels = new int[Consts.Chunk.CubSize];
	public static ulong[][][] BitVoxels = new ulong[2][][]; // BitVoxels[VoxelType][FaceDirection][64]
	public System.Collections.Generic.Dictionary<Vector3I,Vector3I>[] Faces = [];
	public Godot.Collections.Dictionary GreedyFaces;
	public ShaderMaterial Material = new ShaderMaterial();

	bool is_empty = true;
	bool is_full = true;
	bool has_faces = false;
	// private vars
	// built-in override methods
		// Called when the node enters the scene tree for the first time.
		public override void _Ready() {
		this.GlobalPosition = new Godot.Vector3(Coord.X << 4, Coord.Y << 4, Coord.Z << 4);
		Material.Shader = GD.Load<Shader>("res://scenes/main/systems/map/shader/VoxelChunk.gdshader");
		this.MaterialOverride = Material;
		
		for (int VoxelType = 0; VoxelType < 2; VoxelType++) {
			BitVoxels[VoxelType] = new ulong[3][];
			for (int Axis = 0; Axis < 3; Axis++) {
				// GD.Print($"Type: {VoxelType}, Axis: {Axis}");
				BitVoxels[VoxelType][Axis] = new ulong[64];
			}
		}
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
			Faces = MakeGreedyFaces();
			// Faces = MakeFaces();
			Godot.Collections.Array MeshArray = MakeMesh();
			CubeMesh = new ArrayMesh();
			CubeMesh.AddSurfaceFromArrays(Mesh.PrimitiveType.Triangles, MeshArray);

			ApplyMesh();
		}

		float EndTime = (Godot.Time.GetTicksUsec() - StartTime) / 1000f;
		GD.PrintRich($"[color=Springgreen]VoxelChunk-[/color] Created Chunk in [color=gold]{EndTime}ms[/color]");
	}
	public static void SetVoxelBit(int VoxelType, Vector3I Coord) {
		int[] Index = [
			Coord.Y + Coord.Z * Consts.Chunk.Size + Coord.X * Consts.Chunk.SqSize,
			Coord.X + Coord.Z * Consts.Chunk.Size + Coord.Y * Consts.Chunk.SqSize,
			Coord.X + Coord.Y * Consts.Chunk.Size + Coord.Z * Consts.Chunk.SqSize
			];
		
		for (int i = 0; i < 3; i++) {
			int UlongIndex = Index[i] >> 6;
			int BitIndex = Index[i] % 64;
			ulong Bitmask = (ulong)1 << BitIndex;
			BitVoxels[VoxelType][i][UlongIndex] |= Bitmask;
		}
	}
	public static int GetUlongIndex(int Axis, Vector3I Coord){
		int Index = 0;

			switch (Axis) {
				case (int)AXIS.X:
					Index = Coord.Y + Coord.Z * Consts.Chunk.Size + Coord.X * Consts.Chunk.SqSize;
					break;
				
				case (int)AXIS.Y:
				 	Index = Coord.X + Coord.Z * Consts.Chunk.Size + Coord.Y * Consts.Chunk.SqSize;
					break;
				
				case (int)AXIS.Z:
					Index = Coord.X + Coord.Y * Consts.Chunk.Size + Coord.Z * Consts.Chunk.SqSize;
					break;
			}

		int UlongIndex = Index >> 6;
		return UlongIndex;
	}
	public static int GetBitIndex(int Axis, Vector3I Coord) {
		int Index = 0;

		switch (Axis) {
			case (int)AXIS.X:
				Index = Coord.Y + Coord.Z * Consts.Chunk.Size;
				break;
			
			case (int)AXIS.Y:
				Index = Coord.X + Coord.Z * Consts.Chunk.Size;
				break;
			
			case (int)AXIS.Z:
				Index = Coord.X + Coord.Y * Consts.Chunk.Size;
				break;
		}

		int BitIndex = Index % 64;
		return BitIndex;
	}
	// private methods
	private int[] MakeVoxels(FastNoiseLite Noise) {
		int[] TMPVoxels = new int[Consts.Chunk.CubSize];
		for (int x = 0; x < Consts.Chunk.Size; x++) {
			for (int z = 0; z < Consts.Chunk.Size; z++) {
				float PixelData = -Noise.GetNoise2D(x + Coord.X * Consts.Chunk.Size, z + Coord.Z * Consts.Chunk.Size);
				int TileHeight = (int)((PixelData + 1) * 0.5 * (Consts.World.Height - 1) + 1);
				int LocalTileHeight = Math.Min(TileHeight - Coord.Y * Consts.Chunk.Size, 15);
				// GD.Print($"TileHeight: {TileHeight}, LocalTileHeight: {LocalTileHeight}, Chunk Y: {Coord.Y}");
				for (int y = 0; y <= LocalTileHeight; y++) {
					TMPVoxels[x + z * Consts.Chunk.Size + y * Consts.Chunk.SqSize] = (int)VOXELTYPE.DIRT;
					SetVoxelBit((int)VOXELTYPE.DIRT, new Vector3I(x, y, z));
					is_empty = false;
				}
			}
		}
		return TMPVoxels;
	}

	private System.Collections.Generic.Dictionary<Vector3I,Vector3I>[] MakeFaces() {
		System.Collections.Generic.Dictionary<Vector3I,Vector3I>[] TMPFaces = [[], [], [], [], [], []];

		for (int x = 0; x < Consts.Chunk.Size - 1; x++) {
			for (int y = 0; y < Consts.Chunk.Size - 1; y++) {
				for (int z = 0; z < Consts.Chunk.Size; z++) {
					int index = x + z * Consts.Chunk.Size + y * Consts.Chunk.SqSize;
					if (Voxels[index] != 0) {
						Vector3I Coord = new(x, y, z);
						if (Voxels[index + 1] == 0) {
							TMPFaces[(int)DIRECTION.RIGHT].Add(Coord, Coord);
						}
						if (Voxels[index + Consts.Chunk.SqSize] == 0) {
							TMPFaces[(int)DIRECTION.UP].Add(Coord, Coord);
						}
						if (Voxels[index + Consts.Chunk.Size] == 0) {
							TMPFaces[(int)DIRECTION.BACK].Add(Coord, Coord);
						}
					} else {
						if (Voxels[index + 1] != 0) {
							Vector3I Coord = new(x + 1, y, z);
							TMPFaces[(int)DIRECTION.LEFT].Add(Coord, Coord);
						}
						if (Voxels[index + Consts.Chunk.SqSize] != 0) {
							Vector3I Coord = new(x, y + 1, z);
							TMPFaces[(int)DIRECTION.DOWN].Add(Coord, Coord);
						}
						if (Voxels[index + Consts.Chunk.Size] != 0) {
							Vector3I Coord = new(x, y, z + 1);
							TMPFaces[(int)DIRECTION.FORWARD].Add(Coord, Coord);
						}
						is_full = false;
					}
				}
			}
		}

		return TMPFaces;
	}
	private System.Collections.Generic.Dictionary<Vector3I,Vector3I>[] MakeGreedyFaces() {
		System.Collections.Generic.Dictionary<Vector3I,Vector3I>[] TMPFaces = [[],[],[],[],[],[]];

		for (int y = 0; y < Consts.Chunk.Size; y++) {
			for (int z = 0; z < Consts.Chunk.Size; z++) {
				for (int x = 0; x < Consts.Chunk.Size; x++) {

					Vector3I Coord = new(x, y, z);
					int UlongIndex = GetUlongIndex((int)AXIS.Y, Coord);
					int BitIndex = (x + z * Consts.Chunk.Size) % 64;
					
					ulong Ulong = BitVoxels[1][(int)DIRECTION.UP][UlongIndex];

					ulong AboveUlong = y < 15 ? BitVoxels[1][(int)DIRECTION.UP][UlongIndex + 4] : Ulong; // If Top Layer It Equals normal Ulong to always be true

					ulong Bitmask = 1UL << BitIndex;

					if ((Ulong & Bitmask) != 0 && (AboveUlong & Bitmask) == 0) {
						Vector3I StartingPosition = new Vector3I(x, y, z);
						Vector3I EndingPosition = StartingPosition;
						GD.Print($"Found start {StartingPosition}");
						ulong NextBitmask = Bitmask;

						for (int i = 15 - BitIndex % 16; i < 16; i++) {
							NextBitmask <<= 1;
							EndingPosition = new Vector3I(i, y, z);

							if ((Ulong & NextBitmask) == 0 || (AboveUlong & NextBitmask) != 0) {
								GD.Print($"Start: {StartingPosition}, End: {EndingPosition}");
								break;
							}
						}

						TMPFaces[(int)DIRECTION.UP].Add(StartingPosition, EndingPosition);
					}


					// int NeighborIndex1 = Index + 1;
					// if (x < Consts.Chunk.Size) {
					// 	if (VoxelCopy[Index] != 0 && VoxelCopy[NeighborIndex1] == 0) {

					// 	}
					}
				}
			}

		// 	int x_index = 16 + x;

		// 	if (VoxelCopy[x_index] != 0 && VoxelCopy[x_index + y * Consts.SqExtendedChunkSize] == 0) {
		// 		int StartingIndex = x;

		// 		while (x < 15) {
		// 			VoxelCopy[x_index] = 0;
					
		// 			int next_x_index = x_index + 1;
		// 			if (VoxelCopy[next_x_index] != 0 && VoxelCopy[next_x_index + y * Consts.SqExtendedChunkSize] == 0) {
		// 				x++;
		// 			}
		// 		}
		// 		while (z < 16) {

		// 		}

		// 	}
		// }
		return TMPFaces;
	}
	private Godot.Collections.Array MakeMesh() {

		int FaceAmount = 0;

		for (int dir = 0; dir < 6; dir++) {
			FaceAmount += Faces[dir].Count;
			GD.Print($"Dir Face Amount += {Faces[dir].Count}");
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
			System.Collections.Generic.Dictionary<Vector3I,Vector3I> DirList = Faces[dir];

			Color color = Color.Color8(0,255,0);
			if (dir > 3) {
				color = Color.Color8(0,0,255);
			} else if (dir < 2) {
				color = Color.Color8(255,0,0);
			}

			foreach (var(StartingPos, EndingPos) in DirList) {
				Godot.Vector3[][] MeshFace = CreateFace(dir,StartingPos,EndingPos);

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
