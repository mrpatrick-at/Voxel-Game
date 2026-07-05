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
public readonly struct Consts {
    public readonly struct Voxel {
        public static readonly float Size = 1;
        public enum Type : int {
            Stone = 0,
            Dirt = 1,
            Grass = 2,
        }
        public static readonly int Amount = Enum.GetNames(typeof(Consts.Voxel.Type)).Length;

    }
    public readonly struct Chunk {
        public static readonly int Size = 16;
        public static readonly int ExtendedSize = 18;
        public static readonly int SqSize = Size * Size;
        public static readonly int CubSize = SqSize * Size;
    }
    public readonly struct World {
        public static readonly int ChunkWidth = 8;
        public static readonly int ChunkHeight = 4;
        public static readonly int ChunkLength = 8;
        public static readonly int Width = ChunkWidth * Chunk.Size;
        public static readonly int Height = ChunkHeight * Chunk.Size;
        public static readonly int Length = ChunkLength * Chunk.Size;
    }
}

public partial class AssetPreloader : Node {

}

