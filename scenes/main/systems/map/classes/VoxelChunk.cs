using Godot;
using Godot.Collections;
using System;
using System.Collections.Generic;
using System.Diagnostics.Metrics;
using System.Linq;
using System.Numerics;
using Voxel.Consts;
[GlobalClass]
public partial class VoxelChunk : MeshInstance3D
{
	// enums
	// consts
	// exports
	// public vars
	public Vector3I ChunkCoord = Vector3I.Zero;
	public Godot.ArrayMesh CubeMesh;
	public int[] Voxels = new int[Consts.CubExtendedChunkSize];
	public List<int>[] Faces = new List<int>[6];
	public Godot.Collections.Dictionary GreedyFaces;

	bool is_empty = true;
	bool is_full = true;
	bool has_faces = false;
	// private vars
	// onready vars
	// built-in override methods
		// Called when the node enters the scene tree for the first time.
		public override void _Ready() {

		}
		
		// Called every frame. 'delta' is the elapsed time since the previous frame.
		public override void _Process(double delta) {

		}
	// public methods
	public void setup(Vector3I TMPChunkCoord) {
		ChunkCoord = TMPChunkCoord;
		this.GlobalPosition = new Godot.Vector3(ChunkCoord.X << 4, ChunkCoord.Y << 4, ChunkCoord.Z << 4);
	}

	public void generate(FastNoiseLite noise) {
		ulong StartTime = Time.GetTicksUsec();
		GD.PrintRich($"[color=Springgreen]VoxelChunk-[/color] Chunk [color=gold]{ChunkCoord}[/color] called setup");

		Voxels = MakeVoxels(noise);

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
		int y = index / Consts.SqExtendedChunkSize;
		int remainder = index % Consts.SqExtendedChunkSize;

		int x = remainder % Consts.ExtendedChunkSize;
		int z = remainder / Consts.ExtendedChunkSize;

		return new Godot.Vector3(x, y, z);
	}
	// private methods
	private int[] MakeVoxels(FastNoiseLite noise) {
		int[] TMPVoxels = new int[Consts.CubExtendedChunkSize];
		for (int x = 0; x < Consts.ExtendedChunkSize; x++) {
			for (int z = 0; z < Consts.ExtendedChunkSize; z++) {
				float PixelData = -noise.GetNoise2D(x + ChunkCoord.X * Consts.ChunkSize, z + ChunkCoord.Z * Consts.ChunkSize);
				int TileHeight = (int)((PixelData + 1) * 0.5 * (Consts.WorldHeight - 1) + 1);
				int LocalTileHeight = Math.Min(TileHeight - ChunkCoord.Y * Consts.ChunkSize, 17);
				// GD.Print($"TileHeight: {TileHeight}, LocalTileHeight: {LocalTileHeight}, Chunk Y: {ChunkCoord.Y}");
				if (LocalTileHeight >= 18) {
					GD.Print($"HEY IT HAPPANED {LocalTileHeight}");
				}
				for (int y = 0; y < LocalTileHeight; y++) {
					TMPVoxels[x + z * Consts.ExtendedChunkSize + y * Consts.SqExtendedChunkSize] = (int)VOXELTYPE.DIRT;
					is_empty = false;
				}
			}
		}
		return TMPVoxels;
	}

	private List<int>[] MakeFaces() {
		List<int>[] TMPFaces = [new(), new(), new(), new(), new(), new() ];

		for (int x = 0; x < Consts.ChunkSize; x++) {
			for (int y = 0; y < Consts.ChunkSize; y++) {
				for (int z = 0; z < Consts.ChunkSize; z++) {
					int index = x + z * Consts.ExtendedChunkSize + y * Consts.SqExtendedChunkSize;
					if (Voxels[index] != 0) {
						if (Voxels[index + 1] == 0) {
							TMPFaces[(int)DIRECTION.RIGHT].Add(index);
						}
						if (Voxels[index + Consts.SqExtendedChunkSize] == 0) {
							TMPFaces[(int)DIRECTION.UP].Add(index);
						}
						if (Voxels[index + Consts.ExtendedChunkSize] == 0) {
							TMPFaces[(int)DIRECTION.BACK].Add(index);
						}
					} else {
						if (Voxels[index + 1] != 0) {
							TMPFaces[(int)DIRECTION.LEFT].Add(index + 1);
						}
						if (Voxels[index + Consts.SqExtendedChunkSize] != 0) {
							TMPFaces[(int)DIRECTION.DOWN].Add(index + Consts.SqExtendedChunkSize);
						}
						if (Voxels[index + Consts.ExtendedChunkSize] != 0) {
							TMPFaces[(int)DIRECTION.FORWARD].Add(index + Consts.ExtendedChunkSize);
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

		int DirSize = 0;

		for (int dir = 0; dir < 6; dir++) {
			DirSize += Faces[dir].Count;
		}
		// foreach (List<int> dir in Faces) {
		// 	foreach (int Face in dir) {
		// 		DirSize++;
		// 		GD.Print("Face COun");
		// 	}
		// }
		Godot.Vector3[] VertexArray = new Godot.Vector3[DirSize * 4];
		Godot.Vector3[] NormalArray = new Godot.Vector3[DirSize * 4];
		Godot.Vector2[] UvArray = new Godot.Vector2[DirSize * 4];
		int[] IndicesArray = new int[DirSize * 6];

		int Index = 0;

		for (int dir = 0; dir < 6; dir++) {
			List<int> DirList = Faces[dir];
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

		return MeshArray;
	}

	private Godot.Vector3[][] CreateFace(int dir, Godot.Vector3 StartingPosition, Godot.Vector3 EndingPosition) {
		Godot.Vector3[] DirectionArray = new Godot.Vector3[] {
			Godot.Vector3.Right,
			Godot.Vector3.Left,
			Godot.Vector3.Up,
			Godot.Vector3.Down,
			Godot.Vector3.Back,
			Godot.Vector3.Forward,
		};
		Godot.Vector3[][] VerticesArray = new Godot.Vector3[][] {
			new Godot.Vector3[] {
				StartingPosition + new Godot.Vector3(0.5F, -0.5F, -0.5F) * Consts.VoxelSize, // Bottom Left
				new Godot.Vector3(StartingPosition.X,StartingPosition.Y,EndingPosition.Z) + new Godot.Vector3(0.5F, -0.5F,  0.5F) * Consts.VoxelSize, // Bottom Right
				EndingPosition + new Godot.Vector3(0.5F,  0.5F,  0.5F) * Consts.VoxelSize, // Top Right
				new Godot.Vector3(EndingPosition.X,EndingPosition.Y,StartingPosition.Z) + new Godot.Vector3(0.5F,  0.5F, -0.5F) * Consts.VoxelSize, // Top Left
			},
			new Godot.Vector3[] {
				StartingPosition + new Godot.Vector3(-0.5F, -0.5F, -0.5F) * Consts.VoxelSize, // Bottom Left
				new Godot.Vector3(EndingPosition.X,EndingPosition.Y,StartingPosition.Z) + new Godot.Vector3(-0.5F,  0.5F, -0.5F) * Consts.VoxelSize, // Top Left
				EndingPosition + new Godot.Vector3(-0.5F,  0.5F,  0.5F) * Consts.VoxelSize, // Top Right
				new Godot.Vector3(StartingPosition.X,StartingPosition.Y,EndingPosition.Z) + new Godot.Vector3(-0.5F, -0.5F,  0.5F) * Consts.VoxelSize // Bottom Right
			},
			new Godot.Vector3[] {
				StartingPosition + new Godot.Vector3(-0.5F,  0.5F, -0.5F) * Consts.VoxelSize,
				new Godot.Vector3(EndingPosition.X,EndingPosition.Y,StartingPosition.Z) + new Godot.Vector3( 0.5F,  0.5F, -0.5F) * Consts.VoxelSize,
				EndingPosition + new Godot.Vector3( 0.5F,  0.5F,  0.5F) * Consts.VoxelSize,
				new Godot.Vector3(StartingPosition.X,StartingPosition.Y,EndingPosition.Z) + new Godot.Vector3(-0.5F,  0.5F,  0.5F) * Consts.VoxelSize
			},
			new Godot.Vector3[] {
				StartingPosition + new Godot.Vector3(-0.5F, -0.5F,  -0.5F) * Consts.VoxelSize,
				new Godot.Vector3(StartingPosition.X,StartingPosition.Y,EndingPosition.Z) + new Godot.Vector3( -0.5F, -0.5F,  0.5F) * Consts.VoxelSize,
				EndingPosition + new Godot.Vector3( 0.5F, -0.5F, 0.5F) * Consts.VoxelSize,
				new Godot.Vector3(EndingPosition.X,EndingPosition.Y,StartingPosition.Z) + new Godot.Vector3(0.5F, -0.5F, -0.5F) * Consts.VoxelSize
			},
			new Godot.Vector3[] {
				StartingPosition + new Godot.Vector3(-0.5F, -0.5F, 0.5F) * Consts.VoxelSize, // Bottom Left
				new Godot.Vector3(StartingPosition.X,EndingPosition.Y,StartingPosition.Z) + new Godot.Vector3(-0.5F,  0.5F, 0.5F) * Consts.VoxelSize, // Top Left
				EndingPosition + new Godot.Vector3(0.5F,  0.5F,  0.5F) * Consts.VoxelSize, // Top Right
				new Godot.Vector3(EndingPosition.X,StartingPosition.Y,EndingPosition.Z) + new Godot.Vector3(0.5F, -0.5F,  0.5F) * Consts.VoxelSize // Bottom Right
			},
			new Godot.Vector3[] {
				StartingPosition + new Godot.Vector3(-0.5F, -0.5F, -0.5F) * Consts.VoxelSize, // Bottom Left
				new Godot.Vector3(EndingPosition.X,StartingPosition.Y,EndingPosition.Z) + new Godot.Vector3(0.5F, -0.5F,  -0.5F) * Consts.VoxelSize, // Bottom Right
				EndingPosition + new Godot.Vector3(0.5F,  0.5F,  -0.5F) * Consts.VoxelSize, // Top Right
				new Godot.Vector3(StartingPosition.X,EndingPosition.Y,StartingPosition.Z) + new Godot.Vector3(-0.5F,  0.5F, -0.5F) * Consts.VoxelSize, // Top Left
			},
		};
		Godot.Vector3[] Vertices = VerticesArray[dir];
		Godot.Vector3 Direction = DirectionArray[dir];
		Godot.Vector3[] normals = new Godot.Vector3[] {
			Direction, Direction, Direction, Direction
		};

		Godot.Vector3[][] MeshFace = new Godot.Vector3[][] {
			new Godot.Vector3[] {
				Vertices[0], Vertices[1], Vertices[2], Vertices[3]
			},
			new Godot.Vector3[] {
				normals[0], normals[1], normals[2], normals[3]
			}
		};
		return MeshFace;
	}
	private void ApplyMesh() {
		this.Mesh = CubeMesh;
	}
}
