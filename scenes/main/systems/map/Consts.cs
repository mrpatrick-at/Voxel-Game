using Godot;
using Godot.Collections;
using System;
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
public static int ChunkSize = 16;
public static int ExtendedChunkSize = ChunkSize + 2;
public static int SqExtendedChunkSize = ExtendedChunkSize * ExtendedChunkSize;
public static int CubExtendedChunkSize = SqExtendedChunkSize * ExtendedChunkSize;
public static int WorldHeight = 80;
}
