using Godot;
using System;
using System.Runtime.CompilerServices;
using System.Collections.Generic;

namespace VoxelGame.scenes.main.systems.map;

// enums
public static class ChunkGenerator {
   public static ChunkData MakeChunkData(Vector3I ChunkCoord, int[] Heightmap) {
      // ulong StartTime = Time.GetTicksUsec();
      // GD.PrintRich($"[color=Lime]ChunkGenerator-[/color] Started generating Chunk: [color=Gold]{ChunkCoord}");

      (int[] Voxels, bool HasFaces) = MakeVoxelData(ChunkCoord, Heightmap);

      Godot.Collections.Array MeshArray = [];
      Vector3[] Triangles = [];

      if (HasFaces) {

         ulong[] BitVoxels = MakeBitVoxels(Voxels);

         List<FaceData> FaceList = MakeGreedyFaces(BitVoxels);

         (MeshArray, Triangles) = MakeMesh(FaceList);
      }

      ChunkData Data = new(Voxels, MeshArray, Triangles, HasFaces);

      // float EndTime = (Godot.Time.GetTicksUsec() - StartTime) / 1000f;
      // GD.PrintRich($"[color=Lime]ChunkGenerator-[/color] Generated Chunk [color=gold]{ChunkCoord}[/color] in [color=gold]{EndTime}ms[/color]");

      return Data;
   }

   // private methods
   private static (int[], bool) MakeVoxelData(Vector3I ChunkCoord, int[] Heightmap) {
      int[] Voxels = new int[Consts.Chunk.CubExtendedSize];
      bool HasBlocks = false;
      bool HasAir = false;

      int ChunkBaseY = ChunkCoord.Y * Consts.Chunk.Size;

      for (int z = 0; z < Consts.Chunk.ExtendedSize; z++) {
         int HeightmapOffset = z * Consts.Chunk.ExtendedSize;
         for (int x = 0; x < Consts.Chunk.ExtendedSize; x++) {
            // Get Local Tile Height
            int Height = Heightmap[x + HeightmapOffset];

            int LocalHeight = Math.Min(Height - ChunkBaseY, 17);

            // No Blocks in Tile
            if (LocalHeight < 0) {
               HasAir = true;
               continue;
            }

            // Check if Blocks
            if (x > 0 && x < 17 && z > 0 && z < 17) {
               HasBlocks |= LocalHeight > 0;
               HasAir |= LocalHeight < 17;
            }

            int Index = GetVoxelIndex(x, 0, z);

            // Write Voxel Data
            for (int y = 0; y <= LocalHeight; y++) {
               int Depth = LocalHeight - y;

               int Voxel =
                  Depth == 0 ?
                  (int)Consts.Voxel.Type.Grass
                     : Depth < 3 ?
                        (int)Consts.Voxel.Type.Dirt :
                        (int)Consts.Voxel.Type.Stone;

               Voxels[Index + y] = Voxel;
            }
         }
      }
      return (Voxels, HasBlocks && HasAir);
   }

   [MethodImpl(MethodImplOptions.AggressiveInlining)]
   private static int GetVoxelIndex(int x, int y, int z) {
      return y + Consts.Chunk.ExtendedSize * (x + Consts.Chunk.ExtendedSize * z);
   }

   private static ulong[] MakeBitVoxels(int[] Voxels) {
      ulong[] BitVoxels = new ulong[Consts.Voxel.BitVoxelAmount];

      for (int LayerIndex = 0; LayerIndex < Consts.Chunk.ExtendedSize; LayerIndex++) {
         for (int IndicesOffset = 0; IndicesOffset < 256; IndicesOffset++) {
            int I = IndicesOffset & 15;
            int N = IndicesOffset >> 4;

            int UlongIndex = (IndicesOffset >> 6) + (LayerIndex << 2);

            int BitIndex = IndicesOffset & 63;
            ulong Bitmask = 1UL << BitIndex;

            int J = I + 1;
            int K = N + 1;

            for (int Axis = 0; Axis < 3; Axis++) {
               Vector3I Pos = GetPosition(J, LayerIndex, K, Axis);
               // GD.Print($"Coord: {Pos}, Axis: {Axis}");
               int VoxelType = Voxels[GetVoxelIndex(Pos.X, Pos.Y, Pos.Z)];

               if (VoxelType != 0) {
                  int Index = GetBitVoxelIndex(VoxelType, Axis, UlongIndex);
                  BitVoxels[Index] |= Bitmask;
               }

            }
         }
      }
      return BitVoxels;
   }

   [MethodImpl(MethodImplOptions.AggressiveInlining)]
   private static int GetBitVoxelIndex(int VoxelType, int Axis, int UlongIndex) {
      return VoxelType * 216 + Axis * 72 + UlongIndex;
   }

   private static List<FaceData> MakeGreedyFaces(ulong[] BitVoxels) {
      List<FaceData> FaceList = [];

      for (int VoxelType = 0; VoxelType < Consts.Voxel.Amount; VoxelType++) {

         for (int Dir = 0; Dir < 6; Dir++) {
            int Axis = Dir / 2;

            for (int LayerIndex = 0; LayerIndex < Consts.Chunk.Size; LayerIndex++) {
               ulong[] VisibleFaces = new ulong[4];
               for (int LayerUlongIndex = 0; LayerUlongIndex < 4; LayerUlongIndex++) {

                  int UlongIndex = LayerUlongIndex + (LayerIndex << 2) + 4;
                  int ComparisonUlongIndex = (Dir & 1) == 0 ? UlongIndex + 4 : UlongIndex - 4;

                  ulong Ulong = BitVoxels[GetBitVoxelIndex(VoxelType, Axis, UlongIndex)];
                  ulong ComparisonUlong = 0UL;
                  for (int LoopVoxelType = 0; LoopVoxelType < Consts.Voxel.Amount; LoopVoxelType++) {
                     ComparisonUlong |= BitVoxels[GetBitVoxelIndex(LoopVoxelType, Axis, ComparisonUlongIndex)];
                  }

                  VisibleFaces[LayerUlongIndex] = Ulong & ~ComparisonUlong; // All Faces Visible

                  while (VisibleFaces[LayerUlongIndex] != 0) {
                     int BitIndex = System.Numerics.BitOperations.TrailingZeroCount(VisibleFaces[LayerUlongIndex]);
                     ulong BitMask = 1UL << BitIndex;

                     int IndicesOffset = (LayerUlongIndex << 6) + BitIndex;

                     int StartingI = IndicesOffset & 15;
                     int StartingN = IndicesOffset >> 4;
                     Vector3I StartingPosition = GetPosition(StartingI, LayerIndex, StartingN, Axis);

                     int NextI = StartingI + 1;

                     // Horizontal Greedy Expansion
                     while (NextI < Consts.Chunk.Size) {
                        ulong NextBitmask = BitMask << (NextI - StartingI);

                        if ((VisibleFaces[LayerUlongIndex] & NextBitmask) == 0) {
                           break;
                        }
                        VisibleFaces[LayerUlongIndex] &= ~NextBitmask;

                        NextI++;
                     }
                     int EndingI = NextI - 1;

                     ulong CountedBits = 0UL;
                     for (int Shift = StartingI; Shift < EndingI + 1; Shift++) {
                        CountedBits |= 1UL << Shift;
                     }

                     // Vertical Greedy Expansion
                     int NextN = StartingN + 1;
                     while (NextN < Consts.Chunk.Size) {
                        int LoopUlongIndex = NextN >> 2;
                        int RowIndex = NextN & 3;
                        ulong NextBitmask = CountedBits << (RowIndex << 4);

                        if ((VisibleFaces[LoopUlongIndex] & NextBitmask) != NextBitmask) {
                           break;
                        }
                        VisibleFaces[LoopUlongIndex] &= ~NextBitmask;

                        NextN++;
                     }
                     int EndingN = NextN - 1;

                     // Clear the starting bit itself (since greedy expansion clears the rest of the quad)
                     VisibleFaces[LayerUlongIndex] &= ~BitMask;

                     Vector3I EndingPosition = GetPosition(EndingI, LayerIndex, EndingN, Axis);
                     FaceList.Add(new(VoxelType, Dir, StartingPosition, EndingPosition));
                  }
               }
            }
         }
      }

      return FaceList;
   }

   [MethodImpl(MethodImplOptions.AggressiveInlining)]
   private static Vector3I GetPosition(int StartingI, int LayerIndex, int StartingN, int Axis) {
      return Axis switch {
         (int)AXIS.X => new(LayerIndex, StartingN, StartingI),
         (int)AXIS.Y => new(StartingI, LayerIndex, StartingN),
         // Axis Z
         _ => new(StartingN, StartingI, LayerIndex),
      };
   }

   private static (Godot.Collections.Array MeshArray, Vector3[] Triangles) MakeMesh(List<FaceData> FaceList) {

      int FaceAmount = FaceList.Count;

      // Set Array Sizes
      int VertexSize = FaceAmount * 4;
      Godot.Vector3[] VertexArray = new Godot.Vector3[VertexSize];
      Godot.Vector3[] NormalArray = new Godot.Vector3[VertexSize];
      Godot.Vector2[] UvArray = new Godot.Vector2[VertexSize];
      float[] Custom0Array = new float[VertexSize << 2];

      int IndicesSize = FaceAmount * 6;
      int[] IndicesArray = new int[IndicesSize];
      Vector3[] Triangles = new Vector3[IndicesSize];

      // TODO: Make Better or smthin idk
      Godot.Vector2[] TmpUvs = [new(0, 0), new(1, 0), new(1, 1), new(0, 1)];

      // For every Face in Chunk
      for (int Index = 0; Index < FaceAmount; Index++) {
         FaceData Face = FaceList[Index];

         // Get Face Data
         (Godot.Vector3[] FaceVertexArray, Godot.Vector3[] FaceNormalArray) = CreateFace(Face.Direction, Face.StartingPos, Face.EndingPos);
         // Get Face Tiling Data for Shader
         Vector2I FaceLength = GetTilingData(Face.Direction, Face.StartingPos, Face.EndingPos);

         int VertexOffset = Index * 4; // For every Corner of the Face
         int IndicesOffset = Index * 6; // For Every Point of the Face

         for (int i = 0; i < 4; i++) {
            // Write Vertices, Normals and UV's for the Face
            int ArrayIndex = VertexOffset + i;
            VertexArray[ArrayIndex] = FaceVertexArray[i];
            NormalArray[ArrayIndex] = FaceNormalArray[i];
            UvArray[ArrayIndex] = TmpUvs[i];

            // Write Tiling Data for Shader
            int CustomArrayIndex = ArrayIndex * 4;
            Custom0Array[CustomArrayIndex] = (float)Face.VoxelType - 1;
            Custom0Array[CustomArrayIndex + 1] = (float)FaceLength.X; // Face Length X
            Custom0Array[CustomArrayIndex + 2] = (float)FaceLength.Y; // Face Length Y
                                                                      // Custom0Array[CustomArrayIndex + 3] = (float)VoxelType; // placeholder
         }

         // Write Indices for the Face
         IndicesArray[IndicesOffset] = VertexOffset;
         IndicesArray[IndicesOffset + 1] = VertexOffset + 1;
         IndicesArray[IndicesOffset + 2] = VertexOffset + 2;
         IndicesArray[IndicesOffset + 3] = VertexOffset;
         IndicesArray[IndicesOffset + 4] = VertexOffset + 2;
         IndicesArray[IndicesOffset + 5] = VertexOffset + 3;

         // Write Triangles for the Face
         Triangles[IndicesOffset] = VertexArray[VertexOffset];
         Triangles[IndicesOffset + 1] = VertexArray[VertexOffset + 1];
         Triangles[IndicesOffset + 2] = VertexArray[VertexOffset + 2];
         Triangles[IndicesOffset + 3] = VertexArray[VertexOffset];
         Triangles[IndicesOffset + 4] = VertexArray[VertexOffset + 2];
         Triangles[IndicesOffset + 5] = VertexArray[VertexOffset + 3];
      }
      // Combine all Data into 1 Array
      Godot.Collections.Array MeshArray = [];
      MeshArray.Resize((int)Mesh.ArrayType.Max);
      MeshArray[(int)Mesh.ArrayType.Vertex] = VertexArray;
      MeshArray[(int)Mesh.ArrayType.Normal] = NormalArray;
      MeshArray[(int)Mesh.ArrayType.TexUV] = UvArray;
      MeshArray[(int)Mesh.ArrayType.Index] = IndicesArray;
      // MeshArray[(int)Mesh.ArrayType.Color] = ColorArray;
      MeshArray[(int)Mesh.ArrayType.Custom0] = Custom0Array;

      return (MeshArray, Triangles);
   }

   [MethodImpl(MethodImplOptions.AggressiveInlining)]
   private static Vector2I GetTilingData(int Direction, Vector3I StartingPos, Vector3I EndingPos) {
      Vector2I GetFaceDimensions(Vector3I FaceStart, Vector3I FaceEnd) => (Direction / 2) switch {
         0 => new Vector2I(FaceEnd.Z - FaceStart.Z + 1, FaceEnd.Y - FaceStart.Y + 1),
         1 => new Vector2I(FaceEnd.X - FaceStart.X + 1, FaceEnd.Z - FaceStart.Z + 1),
         _ => new Vector2I(FaceEnd.Y - FaceStart.Y + 1, FaceEnd.X - FaceStart.X + 1)
      };

      Vector2I FaceDimensions = GetFaceDimensions(StartingPos, EndingPos);

      return (Direction & 1) == 0 ? FaceDimensions : new(FaceDimensions.Y, FaceDimensions.X);
   }

   private static (Godot.Vector3[] VertexArray, Godot.Vector3[] NormalArray) CreateFace(int dir, Godot.Vector3 StartingPosition, Godot.Vector3 EndingPosition) {
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
      Godot.Vector3[] VertexArray = VerticesArray[dir];
      Godot.Vector3 Direction = DirectionArray[dir];
      Godot.Vector3[] NormalArray = [
          Direction, Direction, Direction, Direction
      ];

      return (VertexArray, NormalArray);
   }
}
public readonly struct FaceData(int VoxelType, int direction, Vector3I startingPos, Vector3I endingPos) {
   public int VoxelType { get; } = VoxelType;
   public int Direction { get; } = direction;
   public Vector3I StartingPos { get; } = startingPos;
   public Vector3I EndingPos { get; } = endingPos;
}
public readonly struct ChunkData(int[] voxels, Godot.Collections.Array meshArray, Vector3[] triangles, bool hasFaces) {
   public int[] Voxels { get; } = voxels;
   public Godot.Collections.Array MeshArray { get; } = meshArray;
   public Vector3[] Triangles { get; } = triangles;
   public bool HasFaces { get; } = hasFaces;
}

