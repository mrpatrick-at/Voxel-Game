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
	public static ulong[][][] BitVoxels = new ulong[2][][]; // BitVoxels[VoxelType][Axis][64]
	public System.Collections.Generic.Dictionary<Vector3I,Vector3I>[][] Faces;
	public Godot.Collections.Dictionary GreedyFaces;
	public ShaderMaterial Material = new();

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
		}
		
		// Called every frame. 'delta' is the elapsed time since the previous frame.
		public override void _Process(double delta) {

		}
	// public methods

	public void Generate(FastNoiseLite Noise) {
		ulong StartTime = Time.GetTicksUsec();
		GD.PrintRich($"[color=Springgreen]VoxelChunk-[/color] Chunk [color=gold]{Coord}[/color] called setup");

		BitVoxels = MakeBitVoxels(Noise);

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
	// private methods
	private ulong[][][] MakeBitVoxels(FastNoiseLite Noise) {
		ulong[][][] TmpBitVoxels = new ulong[Consts.Voxel.Amount][][];

		for (int VoxelType = 0; VoxelType < Consts.Voxel.Amount; VoxelType++) {
			TmpBitVoxels[VoxelType] = new ulong[3][];

			for (int Axis = 0; Axis < 3; Axis++) {
				TmpBitVoxels[VoxelType][Axis] = new ulong[72];
			}
		}

		for (int x = 0; x < Consts.Chunk.Size; x++) {
			for (int z = 0; z < Consts.Chunk.Size; z++) {

				float PixelData = -Noise.GetNoise2D(x + Coord.X * Consts.Chunk.Size, z + Coord.Z * Consts.Chunk.Size);

				int TileHeight = (int)((PixelData + 1) * 0.5 * (Consts.World.Height - 1) + 1);

				int LocalTileHeight = TileHeight - Coord.Y * Consts.Chunk.Size;

				for (int y = 0; y <= LocalTileHeight; y++) {
					is_empty = false;
					Vector3I VoxelCoord = new(x, y, z);

						for (int Axis = 0; Axis < 3; Axis++) {
						int UlongIndex = GetUlongIndex(Axis, VoxelCoord);

						// if (UlongIndex == -1) {
						// 	GD.Print("Bad Coord");
						// 	continue;
						// }

						int BitIndex = GetBitIndex(Axis, VoxelCoord);
						ulong Bitmask = (ulong)1 << BitIndex;
						TmpBitVoxels[(int)Consts.Voxel.Type.DIRT][Axis][UlongIndex] |= Bitmask;
					}
				}

			}
		}

		return TmpBitVoxels;
	}
	private static int GetUlongIndex(int Axis, Vector3I Coord){
		bool IsGood;

		switch (Axis) {
			case (int)AXIS.X:
				IsGood = Coord.Y < Consts.Chunk.Size && Coord.Z < Consts.Chunk.Size;
				return IsGood ? (Coord.Z >> 2) + (Coord.X << 2) : -1;
			
			case (int)AXIS.Y:
				IsGood = Coord.X < Consts.Chunk.Size && Coord.Z < Consts.Chunk.Size;
				return IsGood ? (Coord.Z >> 2) + (Coord.Y << 2) : -1;
			
			default: // Axis Z
				IsGood = Coord.X < Consts.Chunk.Size && Coord.Y < Consts.Chunk.Size;
				return IsGood ? (Coord.Y >> 2) + (Coord.Z << 2) : -1;
		}
	}
	private static int GetBitIndex(int Axis, Vector3I Coord) {
		int BitIndex = 0;

		switch (Axis) {
			case (int)AXIS.X:
				BitIndex = Coord.Y + ((Coord.Z % 4) << 4);
				break;
			
			case (int)AXIS.Y:
				BitIndex = Coord.X + ((Coord.Z % 4) << 4);
				break;
			
			case (int)AXIS.Z:
				BitIndex = Coord.X + ((Coord.Y % 4) << 4);
				break;
		}

		return BitIndex;
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
	private System.Collections.Generic.Dictionary<Vector3I,Vector3I>[][] MakeGreedyFaces() {
		System.Collections.Generic.Dictionary<Vector3I,Vector3I>[][] TMPFaces = new System.Collections.Generic.Dictionary<Vector3I,Vector3I>[Consts.Voxel.Amount][];

		for (int VoxelType = 0; VoxelType < Consts.Voxel.Amount; VoxelType++) {
			TMPFaces[VoxelType] = new System.Collections.Generic.Dictionary<Vector3I,Vector3I>[6];
			GD.Print(TMPFaces[VoxelType]);
			for (int Dir = 0; Dir < 6; Dir++) {
				TMPFaces[VoxelType][Dir] = new System.Collections.Generic.Dictionary<Vector3I,Vector3I>();
				GD.Print(TMPFaces[VoxelType][Dir]);
				int Axis = Dir / 2;

				for (int LayerIndex = 0; LayerIndex < Consts.Chunk.Size; LayerIndex++) {
					for (int LayerUlongIndex = 0; LayerUlongIndex < 4; LayerUlongIndex++) {

						int UlongIndex = LayerUlongIndex + (LayerIndex << 2);
						ulong Ulong = BitVoxels[VoxelType][Axis][UlongIndex];

						ulong ComparisonUlong;

						if ((Dir & 1) == 0) {
							ComparisonUlong = LayerIndex < 15 ? BitVoxels[VoxelType][Axis][UlongIndex + 4] : 0UL;
						} else {
							ComparisonUlong = LayerIndex > 0 ? BitVoxels[VoxelType][Axis][UlongIndex - 4] : 0UL;
						}
						
						ulong VisibleFaces = Ulong & ~ComparisonUlong; // All Faces Visible
						
						for (int BitIndex = 0; BitIndex < 64; BitIndex++) {

							ulong Bitmask = 1UL << BitIndex;

							if ((VisibleFaces & Bitmask) != 0) {
								int StartingI = BitIndex % 16;
								int StartingN = (BitIndex / 16) + (LayerUlongIndex * 4);
								Vector3I StartingPosition = GetPosition(StartingI, LayerIndex, StartingN, Axis);
								// GD.Print($"Found start {StartingPosition}");
								
								ulong CountedBits = Bitmask;
								int EndingI = StartingI;

								while (EndingI < 15) {
									ulong NextBitmask = 1UL << (BitIndex + (EndingI - StartingI) + 1);

									if ((VisibleFaces & NextBitmask) == 0) {
										break;
									}
									CountedBits |= NextBitmask;

									EndingI++;
								}

								Vector3I EndingPosition = GetPosition(EndingI, LayerIndex, StartingN, Axis);
								GD.Print($"Start: {StartingPosition}, End: {EndingPosition}");

								TMPFaces[VoxelType][Dir].Add(StartingPosition, EndingPosition);

								VisibleFaces &= ~CountedBits;
							}
						}
					}
				}
			}
		}

		return TMPFaces;
	}
	private static Vector3I GetPosition(int StartingI, int LayerIndex, int StartingN, int Axis) {
		Vector3I StartingPos;

		if (Axis == 0) { // Is X Axis
			StartingPos = new(LayerIndex, StartingI, StartingN);
		} else if (Axis == 1) { // Is Y Axis
			StartingPos = new(StartingI, LayerIndex, StartingN);
		} else { // Is Z Axis
			StartingPos = new(StartingI, StartingN, LayerIndex);
		}

		return StartingPos;
	}
	private Godot.Collections.Array MakeMesh() {

		int FaceAmount = 0;

		for (int VoxelType = 0; VoxelType < Consts.Voxel.Amount; VoxelType++) {
			for (int dir = 0; dir < 6; dir++) {
				FaceAmount += Faces[VoxelType][dir].Count;
			}
		}

		int VertexSize = FaceAmount * 4;
		Godot.Vector3[] VertexArray = new Godot.Vector3[VertexSize];
		Godot.Vector3[] NormalArray = new Godot.Vector3[VertexSize];
		Godot.Vector2[] UvArray = new Godot.Vector2[VertexSize];
		Godot.Color[] ColorArray = new Godot.Color[VertexSize];

		int IndicesSize = FaceAmount * 6;
		int[] IndicesArray = new int[IndicesSize];

		int Index = 0;
		for (int VoxelType = 0; VoxelType < Consts.Voxel.Amount; VoxelType++) {
			for (int dir = 0; dir < 6; dir++) {

				Color color = Color.Color8(0,255,0);
				if (dir > 3) {
					color = Color.Color8(0,0,255);
				} else if (dir < 2) {
					color = Color.Color8(255,0,0);
				}

				foreach (var(StartingPos, EndingPos) in Faces[VoxelType][dir]) {
					Godot.Vector3[][] MeshFace = CreateFace(dir,StartingPos,EndingPos);

					int IndexOffset = Index << 2;
					int IndicesIndex = IndexOffset + (Index << 1);

					int i = IndexOffset;

					Godot.Vector2[] TmpUvs = [new(0, 0), new(1, 0), new(0, 1), new(1, 1)];
					for (int n = 0; n < 4; n++) {
						VertexArray[i] = MeshFace[(int)MESH.VERTICES][n];
						NormalArray[i] = MeshFace[(int)MESH.Normals][n];
						UvArray[i] = TmpUvs[n];
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
				StartingPosition + new Godot.Vector3(0.5F, -0.5F, -0.5F) * Consts.Voxel.Size, // Bottom Left
				new Godot.Vector3(StartingPosition.X,StartingPosition.Y,EndingPosition.Z) + new Godot.Vector3(0.5F, -0.5F,  0.5F) * Consts.Voxel.Size, // Bottom Right
				EndingPosition + new Godot.Vector3(0.5F,  0.5F,  0.5F) * Consts.Voxel.Size, // Top Right
				new Godot.Vector3(EndingPosition.X,EndingPosition.Y,StartingPosition.Z) + new Godot.Vector3(0.5F,  0.5F, -0.5F) * Consts.Voxel.Size, // Top Left
			],
			[
				StartingPosition + new Godot.Vector3(-0.5F, -0.5F, -0.5F) * Consts.Voxel.Size, // Bottom Left
				new Godot.Vector3(EndingPosition.X,EndingPosition.Y,StartingPosition.Z) + new Godot.Vector3(-0.5F,  0.5F, -0.5F) * Consts.Voxel.Size, // Top Left
				EndingPosition + new Godot.Vector3(-0.5F,  0.5F,  0.5F) * Consts.Voxel.Size, // Top Right
				new Godot.Vector3(StartingPosition.X,StartingPosition.Y,EndingPosition.Z) + new Godot.Vector3(-0.5F, -0.5F,  0.5F) * Consts.Voxel.Size // Bottom Right
			],
			[
				StartingPosition + new Godot.Vector3(-0.5F,  0.5F, -0.5F) * Consts.Voxel.Size,
				new Godot.Vector3(EndingPosition.X,EndingPosition.Y,StartingPosition.Z) + new Godot.Vector3( 0.5F,  0.5F, -0.5F) * Consts.Voxel.Size,
				EndingPosition + new Godot.Vector3( 0.5F,  0.5F,  0.5F) * Consts.Voxel.Size,
				new Godot.Vector3(StartingPosition.X,StartingPosition.Y,EndingPosition.Z) + new Godot.Vector3(-0.5F,  0.5F,  0.5F) * Consts.Voxel.Size
			],
			[
				StartingPosition + new Godot.Vector3(-0.5F, -0.5F,  -0.5F) * Consts.Voxel.Size,
				new Godot.Vector3(StartingPosition.X,StartingPosition.Y,EndingPosition.Z) + new Godot.Vector3( -0.5F, -0.5F,  0.5F) * Consts.Voxel.Size,
				EndingPosition + new Godot.Vector3( 0.5F, -0.5F, 0.5F) * Consts.Voxel.Size,
				new Godot.Vector3(EndingPosition.X,EndingPosition.Y,StartingPosition.Z) + new Godot.Vector3(0.5F, -0.5F, -0.5F) * Consts.Voxel.Size
			],
			[
				StartingPosition + new Godot.Vector3(-0.5F, -0.5F, 0.5F) * Consts.Voxel.Size, // Bottom Left
				new Godot.Vector3(StartingPosition.X,EndingPosition.Y,StartingPosition.Z) + new Godot.Vector3(-0.5F,  0.5F, 0.5F) * Consts.Voxel.Size, // Top Left
				EndingPosition + new Godot.Vector3(0.5F,  0.5F,  0.5F) * Consts.Voxel.Size, // Top Right
				new Godot.Vector3(EndingPosition.X,StartingPosition.Y,EndingPosition.Z) + new Godot.Vector3(0.5F, -0.5F,  0.5F) * Consts.Voxel.Size // Bottom Right
			],
			[
				StartingPosition + new Godot.Vector3(-0.5F, -0.5F, -0.5F) * Consts.Voxel.Size, // Bottom Left
				new Godot.Vector3(EndingPosition.X,StartingPosition.Y,EndingPosition.Z) + new Godot.Vector3(0.5F, -0.5F,  -0.5F) * Consts.Voxel.Size, // Bottom Right
				EndingPosition + new Godot.Vector3(0.5F,  0.5F,  -0.5F) * Consts.Voxel.Size, // Top Right
				new Godot.Vector3(StartingPosition.X,EndingPosition.Y,StartingPosition.Z) + new Godot.Vector3(-0.5F,  0.5F, -0.5F) * Consts.Voxel.Size, // Top Left
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
			],
		];
		return MeshFace;
	}
	private void ApplyMesh() {
		this.Mesh = CubeMesh;
	}
}
