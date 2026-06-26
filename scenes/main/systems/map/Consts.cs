using Godot;
using Godot.Collections;
using System;
using System.Drawing;
namespace VoxelGame.Consts;
// enum
enum DIRECTION : int {
    RIGHT = 0,
    LEFT = 1,
    UP = 2,
    DOWN = 3,
    BACK = 4,
    FORWARD = 5,
}
enum VOXELTYPE : int {
    AIR = 0,
    DIRT = 1,
}
enum MESH : int {
    VERTICES = 0,
    Normals = 1,
    UVS = 2,
    INDICES = 3,
}
public struct Consts {
public static float VoxelSize = 1;
    public struct Chunk {
        public static int Size = 16;
        public static int ExtendedSize = Size + 2;
        public static int SqExtendedSize = ExtendedSize * ExtendedSize;
        public static int CubExtendedSize = SqExtendedSize * ExtendedSize;
    }
    public struct World {
        public static int ChunkWidth = 6;
        public static int ChunkHeight = 6;
        public static int ChunkLength = 6;
        public static int Width = ChunkWidth * Chunk.Size;
        public static int Height = ChunkHeight * Chunk.Size;
        public static int Length = ChunkLength * Chunk.Size;
    }
}
