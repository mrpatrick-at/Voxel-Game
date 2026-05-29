using Godot;
using Godot.Collections;
using System;

public partial class voxel_chunk : Node
{
	/// enums
	/// consts
	/// exports
	/// public vars
	float cube_size = 1.0F;
	Godot.ArrayMesh cube_mesh;
	//var voxels:PackedByteArray = [];
	Godot.Collections.Dictionary faces;
	Godot.Collections.Array placeholder_uvs = [0,0,0,0,0,0];

	bool is_empty = true;
	bool is_full = true;
	bool has_faces = false;
	/// private vars
	/// onready vars
	// obj_ for node refrences
	/// built-in override methods
		// Called when the node enters the scene tree for the first time.
		public override void _Ready()
		{
		}
		
		// Called every frame. 'delta' is the elapsed time since the previous frame.
		public override void _Process(double delta)
		{
		}
	/// public methods
	/// private methods
}
