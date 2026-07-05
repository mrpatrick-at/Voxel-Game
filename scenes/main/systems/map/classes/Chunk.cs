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
	public Godot.ArrayMesh CubeMesh {get; set;}
	// public static ulong[][][] BitVoxels; // BitVoxels[VoxelType][Axis][64]
	// public System.Collections.Generic.Dictionary<Vector3I,Vector3I>[][] Faces;
	// public System.Collections.Generic.Dictionary<Vector3I,Vector2I>[] FaceLengths = [[],[],[],[],[],[]];
	// public ShaderMaterial Material = new();
	// bool is_empty = true;
	
	// private vars
	// built-in override methods
	public override void _Ready() {
		ulong StartTime = Time.GetTicksUsec();
		GD.PrintRich($"[color=Springgreen]VoxelChunk-[/color] VoxelChunk [color=gold]{Coord}[/color] Created");

		this.GlobalPosition = new Godot.Vector3(Coord.X << 4, Coord.Y << 4, Coord.Z << 4);
		ApplyMesh();

		float EndTime = (Godot.Time.GetTicksUsec() - StartTime) / 1000f;
		GD.PrintRich($"[color=Springgreen]VoxelChunk-[/color] Created VoxelChunk in [color=gold]{EndTime}ms[/color]");
	}
		
	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta) {

	}
	// public methods
	// private methods
	private void ApplyMesh() {
		this.Mesh = CubeMesh;

		// StaticBody3D StaticBody = new();
		// CollisionShape3D CollisionShape = new();
		// ConvexPolygonShape3D ChunkCollison = CubeMesh.CreateConvexShape();
		// CollisionShape.Shape = ChunkCollison;
		// StaticBody.SetCollisionLayerValue(1, true);
		// StaticBody.SetCollisionMaskValue(0, true);

		// this.AddChild(StaticBody);
		// StaticBody.AddChild(CollisionShape);
	}
}
public partial class DataChunk {
	// signals
	// exports
	// consts
	// public vars
	public Godot.ArrayMesh CubeMesh = new();
	public static ulong[][][] BitVoxels; // BitVoxels[VoxelType][Axis][64]
	public System.Collections.Generic.Dictionary<Vector3I,Vector3I>[][] Faces;
	public System.Collections.Generic.Dictionary<Vector3I,Vector2I>[] FaceLengths = [[],[],[],[],[],[]];
	// private vars
	private bool is_empty = true;
	private bool HasFaces = false;
	// built-in override methods
	// public methods
	public void Generate(FastNoiseLite Noise, Vector3I Coord) {
		ulong StartTime = Time.GetTicksUsec();
		GD.PrintRich($"[color=Springgreen]DataChunk-[/color] Chunk [color=gold]{Coord}[/color] Created");
		
		BitVoxels = MakeBitVoxels(Noise, Coord);

		if (!is_empty) {
			Faces = MakeGreedyFaces();
			
			if (HasFaces) {
				Godot.Collections.Array MeshArray = MakeMesh();
				Mesh.ArrayFormat FormatFlags = Mesh.ArrayFormat.FormatVertex
											| Mesh.ArrayFormat.FormatNormal
											| Mesh.ArrayFormat.FormatTexUV
											| Mesh.ArrayFormat.FormatIndex
											| Mesh.ArrayFormat.FormatColor
											| Mesh.ArrayFormat.FormatCustom0;

				int Custom0FormatShift = (int)Mesh.ArrayCustomFormat.RgbaFloat << (int)Mesh.ArrayFormat.FormatCustom0Shift;
				FormatFlags |= (Mesh.ArrayFormat)Custom0FormatShift;

				CubeMesh.AddSurfaceFromArrays(Mesh.PrimitiveType.Triangles, MeshArray, flags: FormatFlags);
				}
		}

		float EndTime = (Godot.Time.GetTicksUsec() - StartTime) / 1000f;
		GD.PrintRich($"[color=Springgreen]DataChunk-[/color] Created Chunk in [color=gold]{EndTime}ms[/color]");
	}
	// private methods
	private ulong[][][] MakeBitVoxels(FastNoiseLite Noise, Vector3I Coord) {
		ulong[][][] TmpBitVoxels = new ulong[Consts.Voxel.Amount][][];

		for (int VoxelType = 0; VoxelType < Consts.Voxel.Amount; VoxelType++) {
			TmpBitVoxels[VoxelType] = new ulong[3][];

			for (int Axis = 0; Axis < 3; Axis++) {
				TmpBitVoxels[VoxelType][Axis] = new ulong[72];
			}
		}

		for (int x = -1; x <= Consts.Chunk.Size; x++) {
			for (int z = -1; z <= Consts.Chunk.Size; z++) {

				float PixelData = -Noise.GetNoise2D(x + Coord.X * Consts.Chunk.Size, z + Coord.Z * Consts.Chunk.Size);

				int TileHeight = (int)((PixelData + 1) * 0.5 * (Consts.World.Height - 1) + 1);

				int LocalTileHeight = Math.Min(TileHeight - Coord.Y * Consts.Chunk.Size, Consts.Chunk.Size);

				for (int y = -1; y <= LocalTileHeight; y++) {
					Vector3I VoxelCoord = new(x, y, z);

					for (int Axis = 0; Axis < 3; Axis++) {
						if (IsCoordGood(Axis, VoxelCoord)) {
							int UlongIndex = GetUlongIndex(Axis, VoxelCoord);
							is_empty = false;

							int BitIndex = GetBitIndex(Axis, VoxelCoord);
							ulong Bitmask = (ulong)1 << BitIndex;
							int Block;

							if (y == LocalTileHeight) {
								Block = (int)Consts.Voxel.Type.Grass;
							} else if (y == LocalTileHeight - 1) {
								Block = (int)Consts.Voxel.Type.Dirt;
							} else {
								Block = (int)Consts.Voxel.Type.Stone;
							}

							TmpBitVoxels[Block][Axis][UlongIndex] |= Bitmask;
						}
					}
				}
			}
		}
		return TmpBitVoxels;
	}
	private static bool IsCoordGood(int Axis, Vector3I VoxelCoord) {
		switch (Axis) {
			case (int)AXIS.X:
				return VoxelCoord.Y is >= 0 and < 16 && VoxelCoord.Z is >= 0 and < 16;
			
			case (int)AXIS.Y:
				return VoxelCoord.X is >= 0 and < 16 && VoxelCoord.Z is >= 0 and < 16;
			
			default: // Axis Z
				return VoxelCoord.X is >= 0 and < 16 && VoxelCoord.Y is >= 0 and < 16;
		}
	}
	private static int GetUlongIndex(int Axis, Vector3I VoxelCoord) { // returns -1 if number invalid

		switch (Axis) {
			case (int)AXIS.X:
				return (VoxelCoord.Y >> 2) + ((VoxelCoord.X + 1) << 2);
			
			case (int)AXIS.Y:
				return (VoxelCoord.Z >> 2) + ((VoxelCoord.Y + 1) << 2);
			
			default: // Axis Z
				return (VoxelCoord.X >> 2) + ((VoxelCoord.Z + 1) << 2);
		}
	}
	private static int GetBitIndex(int Axis, Vector3I VoxelCoord) {
		switch (Axis) {
			case (int)AXIS.X:
				return VoxelCoord.Z + ((VoxelCoord.Y % 4) << 4);
			
			case (int)AXIS.Y:
				return VoxelCoord.X + ((VoxelCoord.Z % 4) << 4);
			
			default:
				return VoxelCoord.Y + ((VoxelCoord.X % 4) << 4);
		}
	}
	private System.Collections.Generic.Dictionary<Vector3I,Vector3I>[][] MakeGreedyFaces() {
		System.Collections.Generic.Dictionary<Vector3I,Vector3I>[][] TMPFaces = new System.Collections.Generic.Dictionary<Vector3I,Vector3I>[Consts.Voxel.Amount][];

		for (int VoxelType = 0; VoxelType < Consts.Voxel.Amount; VoxelType++) {
			TMPFaces[VoxelType] = new System.Collections.Generic.Dictionary<Vector3I,Vector3I>[6];

			for (int Dir = 0; Dir < 6; Dir++) {
				TMPFaces[VoxelType][Dir] = new System.Collections.Generic.Dictionary<Vector3I,Vector3I>();
				int Axis = Dir / 2;

				for (int LayerIndex = 0; LayerIndex < Consts.Chunk.Size; LayerIndex++) {
					ulong[] VisibleFaces = new ulong[4];
					for (int LayerUlongIndex = 0; LayerUlongIndex < 4; LayerUlongIndex++) {

						int UlongIndex = LayerUlongIndex + (LayerIndex << 2) + 4;

						ulong Ulong = BitVoxels[VoxelType][Axis][UlongIndex];

						int ComparisonUlongIndex = (Dir & 1) == 0 ? UlongIndex + 4 : UlongIndex -4;
						// ulong ComparisonUlong = BitVoxels[VoxelType][Axis][ComparisonUlongIndex];
						ulong ComparisonUlong;

						if ((Dir & 1) == 0) {
							ComparisonUlong = BitVoxels[VoxelType][Axis][ComparisonUlongIndex];
						} else {
							ComparisonUlong = BitVoxels[VoxelType][Axis][ComparisonUlongIndex];
						}
						VisibleFaces[LayerUlongIndex] = Ulong & ~ComparisonUlong; // All Faces Visible
						
						// if (Ulong != 0UL){
						// GD.Print($"UlongIndex: {UlongIndex}, Ulong: {Ulong}, ComparisonUlongIndex: {ComparisonUlongIndex}, ComparisonUlong: {ComparisonUlong}. VisibleFaces: {VisibleFaces[LayerUlongIndex]}");
						// }
					}

					for (int FaceIndex = 0; FaceIndex < 256; FaceIndex++) {

						int UlongIndex = FaceIndex / 64;
						int BitIndex = FaceIndex % 64;
						ulong Bitmask = 1UL << (BitIndex);

						if ((VisibleFaces[UlongIndex] & Bitmask) != 0) {
							int StartingI = FaceIndex % 16;
							int StartingN = FaceIndex / 16;
							Vector3I StartingPosition = GetPosition(StartingI, LayerIndex, StartingN, Axis);
							// GD.Print($"Found start {StartingPosition}");
							
							int NextI = StartingI + 1;

							while (NextI < 16) {
								ulong NextBitmask = Bitmask << (NextI - StartingI);

								if ((VisibleFaces[UlongIndex] & NextBitmask) == 0) {
									break;
								}
								VisibleFaces[UlongIndex] &= ~NextBitmask;

								NextI++;
							}
							int EndingI = NextI - 1;

							ulong CountedBits = 0UL;

							for (int Shift = StartingI; Shift < EndingI + 1; Shift++) {
								CountedBits |= 1UL << Shift;
							}

							int NextN = StartingN + 1;
							
							while (NextN < 16) {
								int LoopUlongIndex = NextN / 4;

								int RowIndex = NextN % 4;
								ulong NextBitmask = CountedBits << (16 * RowIndex);

								if ((VisibleFaces[LoopUlongIndex] & NextBitmask) != NextBitmask) {
									break;
								}
								VisibleFaces[LoopUlongIndex] &= ~NextBitmask;

								NextN++;
							}
							int EndingN = NextN - 1;

							Vector3I EndingPosition = GetPosition(EndingI, LayerIndex, EndingN, Axis);
							// GD.Print($"Start: {StartingPosition}, End: {EndingPosition}");
							HasFaces = true;

							TMPFaces[VoxelType][Dir].Add(StartingPosition, EndingPosition);
							
							Vector2I TilingData = (Dir & 1) == 0 ? new(EndingI - StartingI + 1, EndingN - StartingN + 1): new(EndingN - StartingN + 1, EndingI - StartingI + 1);

							FaceLengths[Dir].Add(StartingPosition, TilingData);
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
			StartingPos = new(LayerIndex, StartingN, StartingI);
		} else if (Axis == 1) { // Is Y Axis
			StartingPos = new(StartingI, LayerIndex, StartingN);
		} else { // Is Z Axis
			StartingPos = new(StartingN, StartingI, LayerIndex);
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
		float[] Custom0Array = new float[VertexSize << 2];

		int IndicesSize = FaceAmount * 6;
		int[] IndicesArray = new int[IndicesSize];

		int Index = 0;
		for (int VoxelType = 0; VoxelType < Consts.Voxel.Amount; VoxelType++) {


			for (int Dir = 0; Dir < 6; Dir++) {

				Color color = Color.Color8(0,255,0);
				if (Dir > 3) {
					color = Color.Color8(0,0,255);
				} else if (Dir < 2) {
					color = Color.Color8(255,0,0);
				}


				foreach (var(StartingPos, EndingPos) in Faces[VoxelType][Dir]) {
					Godot.Vector3[][] MeshFace = CreateFace(Dir,StartingPos,EndingPos);
					Vector2I FaceLength = FaceLengths[Dir][StartingPos];

					int IndexOffset = Index << 2;
					int IndicesIndex = IndexOffset + (Index << 1);

					Godot.Vector2[] TmpUvs = [new(0, 0), new(1, 0), new(1, 1), new(0, 1)];

					for (int i = 0; i < 4; i++) {
						int ArrayIndex = i + IndexOffset;
						VertexArray[ArrayIndex] = MeshFace[(int)MESH.VERTICES][i];
						NormalArray[ArrayIndex] = MeshFace[(int)MESH.Normals][i];
						UvArray[ArrayIndex] = TmpUvs[i];
						ColorArray[ArrayIndex] = color;

						int CustomArrayIndex = ArrayIndex << 2; // TODO: VERY IMPORTANT !!!!!! ADD FUNC THAT GETS DISTANCE OF VECS AND PASSES IT TO SHADER :)
							Custom0Array[CustomArrayIndex] = (float)VoxelType;
							Custom0Array[CustomArrayIndex + 1] = (float)FaceLength.X; // Face Length X
							Custom0Array[CustomArrayIndex + 2] = (float)FaceLength.Y; // Face Length Y
							// Custom0Array[CustomArrayIndex + n + 3] = (float)VoxelType; // placeholder
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
		MeshArray[(int)Mesh.ArrayType.Custom0] = Custom0Array;
		
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
}