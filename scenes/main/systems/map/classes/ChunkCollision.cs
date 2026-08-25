using Godot;
using Godot.Collections;
using System;
namespace VoxelGame.Chunk;
using System.Data;
using VoxelGame.Consts;
[GlobalClass]
public partial class ChunkCollision : StaticBody3D {
    // signals
    // exports
    // consts
    // public vars
    public CollisionShape3D CollisionShape = new();
    public ConcavePolygonShape3D ConcaveShape = new();
    // private vars
    // built-in override methods
    public override void _Ready() {
        this.CollisionLayer = 1;
        this.CollisionMask = 1;

        CollisionShape.Shape = ConcaveShape;
        this.AddChild(CollisionShape);
    }
    // public methods
    public void SetCollision(Godot.Vector3[] Triangles) {
        if (Triangles.Length == 0) {
            CollisionShape.Disabled = true;
        } else {
            ConcaveShape.SetFaces(Triangles); // Expects an array of vertices where every 3 vertices make a triangle

            CollisionShape.Disabled = false;
        }
    }
    // private methods
}

