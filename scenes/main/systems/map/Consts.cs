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
enum AXIS : int {
    X = 0,
    Y = 1,
    Z = 2,
}
enum MESH : int {
    VERTICES = 0,
    Normals = 1,
    UVS = 2,
    INDICES = 3,
}
public struct Consts {
    public struct Voxel {
        public static float Size = 1;
        public enum Type : int {
            AIR = 0,
            DIRT = 1,
            Stone = 2,
        }
        public static int Amount = Enum.GetNames(typeof(Consts.Voxel.Type)).Length;

    }
    public struct Chunk {
        public static int Size = 16;
        public static int ExtendedSize = 18;
        public static int SqSize = Size * Size;
        public static int CubSize = SqSize * Size;
    }
    public struct World {
        public static int ChunkWidth = 2;
        public static int ChunkHeight = 2;
        public static int ChunkLength = 2;
        public static int Width = ChunkWidth * Chunk.Size;
        public static int Height = ChunkHeight * Chunk.Size;
        public static int Length = ChunkLength * Chunk.Size;
    }
}
