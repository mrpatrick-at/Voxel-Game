@tool
extends MeshInstance3D
class_name VoxelChunk
## enums
## consts
## exports
@export var cube_size: float = 1.0
## public vars
var cube_mesh: ArrayMesh
var voxels:PackedByteArray = []
var byte_voxel_array:PackedByteArray = []
var faces:Dictionary[int,PackedVector3Array] = {}
var placeholder_uvs:Array = [0,0,0,0,0,0]

var is_empty:bool = true
var is_full:bool = true
var has_faces:bool = false
## private vars
## onready vars
# obj_ for node refrences
## built-in override methods
## public methods

func setup(chunk_coord:Vector3i, chunk_size:int, height_map:PackedByteArray) -> void:
	var start_time := Time.get_ticks_usec()
	print("Voxel_Chunk- Chunk %s Called Setup"%chunk_coord)
	
	global_position = Vector3(chunk_coord.x * chunk_size, chunk_coord.y * chunk_size, chunk_coord.z * chunk_size)
	
	voxels = make_voxels(chunk_coord, chunk_size, height_map)
	
	if !is_empty and !is_full:
		
		faces = check_faces(chunk_size)
		
		if has_faces:
			generate_mesh()
	
	var time_taken := (Time.get_ticks_usec() - start_time) / 1000.0
	print("Voxel_Chunk- Chunk %s Made in: %s msec"%[chunk_coord,time_taken])

func make_voxels(chunk_coord:Vector3i, chunk_size:int, height_map:PackedByteArray) -> Array:
	#var start_time := Time.get_ticks_usec()
	
	var voxel_array:PackedByteArray = []
	voxel_array.resize((chunk_size + 2) * (chunk_size + 2) * (chunk_size + 2))
	
	for x:int in chunk_size + 2:
		for y:int in chunk_size + 2:
			for z:int in chunk_size + 2:
				if y < height_map[x + z * (chunk_size + 2)] - chunk_coord.y * (chunk_size):
					#if bitboard.get_voxel(x, y, z) == true:
						#print("OH NOOO")
					#bitboard.set_voxel(x, y, z, true)
					voxel_array[x + (y * (chunk_size + 2)) + (z * (chunk_size + 2) * (chunk_size + 2))] = 1
					is_empty = false
					continue
				is_full = false
	#var time_taken := (Time.get_ticks_usec() - start_time) / 1000.0
	#print("Voxel_Chunk- Chunk Data Made in: %s msec"%time_taken)
	return voxel_array

func check_faces(chunk_size:int) -> Dictionary:
	var start_time := Time.get_ticks_usec()
	var face_data:Dictionary[int,PackedVector3Array] = {
		Constants.DIRECTION.RIGHT : [],
		Constants.DIRECTION.LEFT : [],
		Constants.DIRECTION.UP : [],
		Constants.DIRECTION.DOWN : [],
		Constants.DIRECTION.BACK : [],
		Constants.DIRECTION.FORWARD : [],
	}
	
	var coord_array:PackedByteArray = []
	
	for x:int in range(chunk_size):
		for y:int in range(chunk_size):
			for z:int in range(chunk_size):
				var coord:int = x + (y * (chunk_size + 2)) + (z * (chunk_size + 2) * (chunk_size + 2))
				if voxels[x + (y * (chunk_size + 2)) + (z * (chunk_size + 2) * (chunk_size + 2))] == 0:
					
					if voxels[(x + 1) + (y * (chunk_size + 2)) + (z * (chunk_size + 2) * (chunk_size + 2))] != 0:
						face_data[Constants.DIRECTION.LEFT].append(Vector3(x + 1,y,z))
						has_faces = true
				
					if voxels[x + ((y + 1) * (chunk_size + 2)) + (z * (chunk_size + 2) * (chunk_size + 2))] != 0:
						face_data[Constants.DIRECTION.DOWN].append(Vector3(x,y + 1,z))
						has_faces = true
					
					if voxels[x + (y * (chunk_size + 2)) + ((z + 1) * (chunk_size + 2) * (chunk_size + 2))] != 0:
						face_data[Constants.DIRECTION.FORWARD].append(Vector3(x,y,z + 1))
						has_faces = true
					
					continue
				
				if voxels[(x + 1) + (y * (chunk_size + 2)) + (z * (chunk_size + 2) * (chunk_size + 2))] == 0:
					face_data[Constants.DIRECTION.RIGHT].append(Vector3(x, y, z))
					has_faces = true
				
				if voxels[x + ((y + 1) * (chunk_size + 2)) + (z * (chunk_size + 2) * (chunk_size + 2))] == 0:
					face_data[Constants.DIRECTION.UP].append(Vector3(x, y, z))
					has_faces = true
				
				if voxels[x + (y * (chunk_size + 2)) + ((z + 1) * (chunk_size + 2) * (chunk_size + 2))] == 0:
					face_data[Constants.DIRECTION.BACK].append(Vector3(x, y, z))
					has_faces = true
	
	var time_taken := (Time.get_ticks_usec() - start_time) / 1000.0
	print("Voxel_Chunk- Checked Faces in: %s msec"%time_taken)
	return face_data

func greedy_mesher() -> Dictionary:
	var positions:Dictionary[int,Dictionary] = {}
	
	for direction:int in faces:
		positions[direction] = {}
		
		var first_next_tile:Vector3 = Vector3.RIGHT
		var second_next_tile:Vector3 = Vector3.BACK
		
		if direction == Constants.DIRECTION.RIGHT or direction == Constants.DIRECTION.LEFT:
			first_next_tile = Vector3.UP
		
		for pos:Vector3 in faces[direction]:
			var ending_pos:Vector3 = pos
			
			var next_tile_array:Array = [pos]
			
			while faces[direction].has(ending_pos + first_next_tile):
				ending_pos += first_next_tile
				next_tile_array.append(ending_pos)
				faces[direction].erase(ending_pos)
			
			var can_shift:bool = true
			var next_shift:Vector3 = second_next_tile
			
			while faces[direction].has(ending_pos + second_next_tile):
				for tile:Vector3 in next_tile_array:
					
					if faces[direction].has(tile + next_shift):
						continue
					
					can_shift = false
					break
				
				if !can_shift:
					break
				
				for tile:Vector3 in next_tile_array:
					faces[direction].erase(tile + next_shift)
				
				ending_pos += second_next_tile
				next_shift += second_next_tile
			
			positions[direction].set(pos,ending_pos)
	
	return positions

func generate_mesh() -> void:
	var start_time := Time.get_ticks_usec()
	var mesh_faces:Array = []
	
	var positions:Dictionary = greedy_mesher()
	for direction:int in positions:
		for pos:Vector3 in positions[direction]:
			#print("helly eah x", pos, positions[direction][pos])
			mesh_faces.append(create_face(direction, pos, positions[direction][pos], placeholder_uvs))
	
	var vertices:Array = []
	var normals:Array = []
	var uvs:Array = []
	
	for face:Dictionary in mesh_faces:
		vertices += face ["vertices"]
		normals += face ["normals"]
		uvs += face ["uvs"]
	
	var vertex_array:PackedVector3Array = PackedVector3Array(vertices)
	var normal_array:PackedVector3Array = PackedVector3Array(normals)
	var uv_array:PackedVector3Array = PackedVector3Array(uvs)
	
	var arrays:Array = []
	arrays.resize(Mesh.ARRAY_MAX)
	arrays[Mesh.ARRAY_VERTEX] = vertex_array
	arrays[Mesh.ARRAY_NORMAL] = normal_array
	arrays[Mesh.ARRAY_TEX_UV] =  uv_array
	
	cube_mesh = ArrayMesh.new()
	cube_mesh.add_surface_from_arrays(Mesh.PRIMITIVE_TRIANGLES, arrays)
	self.mesh = cube_mesh

	var time_taken := (Time.get_ticks_usec() - start_time) / 1000.0
	print("Voxel_Chunk- Mesh Made in: %s msec"%time_taken)

func create_face(direction:int, starting_position:Vector3, ending_position:Vector3, uv_coords:Array) -> Dictionary:
	var face_directions:Array = [
		[ # Right
			[
				starting_position + Vector3(0.5, -0.5, -0.5) * cube_size, # Bottom Left
				Vector3(starting_position.x,starting_position.y,ending_position.z) + Vector3(0.5, -0.5,  0.5) * cube_size, # Bottom Right
				ending_position + Vector3(0.5,  0.5,  0.5) * cube_size, # Top Right
				Vector3(ending_position.x,ending_position.y,starting_position.z) + Vector3(0.5,  0.5, -0.5) * cube_size, # Top Left
			],
			Vector3.RIGHT,
		],
		[ # Left
			[
				starting_position + Vector3(-0.5, -0.5, -0.5) * cube_size, # Bottom Left
				Vector3(ending_position.x,ending_position.y,starting_position.z) + Vector3(-0.5,  0.5, -0.5) * cube_size, # Top Left
				ending_position + Vector3(-0.5,  0.5,  0.5) * cube_size, # Top Right
				Vector3(starting_position.x,starting_position.y,ending_position.z) + Vector3(-0.5, -0.5,  0.5) * cube_size # Bottom Right
			],
			Vector3.LEFT,
		],
		[ # Up
			[
				starting_position + Vector3(-0.5,  0.5, -0.5) * cube_size,
				Vector3(ending_position.x,ending_position.y,starting_position.z) + Vector3( 0.5,  0.5, -0.5) * cube_size,
				ending_position + Vector3( 0.5,  0.5,  0.5) * cube_size,
				Vector3(starting_position.x,starting_position.y,ending_position.z) + Vector3(-0.5,  0.5,  0.5) * cube_size
			],
			Vector3.UP,
		],
		[ # Down
			[
				starting_position + Vector3(-0.5, -0.5,  -0.5) * cube_size,
				Vector3(starting_position.x,starting_position.y,ending_position.z) + Vector3( -0.5, -0.5,  0.5) * cube_size,
				ending_position + Vector3( 0.5, -0.5, 0.5) * cube_size,
				Vector3(ending_position.x,ending_position.y,starting_position.z) + Vector3(0.5, -0.5, -0.5) * cube_size
			],
			Vector3.DOWN,
		],
		[ # Back
			[
				starting_position + Vector3(-0.5, -0.5, 0.5) * cube_size, # Bottom Left
				Vector3(starting_position.x,ending_position.y,starting_position.z) + Vector3(-0.5,  0.5, 0.5) * cube_size, # Top Left
				ending_position + Vector3(0.5,  0.5,  0.5) * cube_size, # Top Right
				Vector3(ending_position.x,starting_position.y,ending_position.z) + Vector3(0.5, -0.5,  0.5) * cube_size # Bottom Right
			],
			Vector3.BACK,
		],
		[ # Forward
			[
				starting_position + Vector3(-0.5, -0.5, -0.5) * cube_size, # Bottom Left
				Vector3(ending_position.x,starting_position.y,ending_position.z) + Vector3(0.5, -0.5,  -0.5) * cube_size, # Bottom Right
				ending_position + Vector3(0.5,  0.5,  -0.5) * cube_size, # Top Right
				Vector3(starting_position.x,ending_position.y,starting_position.z) + Vector3(-0.5,  0.5, -0.5) * cube_size, # Top Left
			],
			Vector3.FORWARD,
		],
	]
	var direction_data:Array = face_directions[direction]
	var vertices:Array = direction_data[0]
	var normals:Array = []
	normals.resize(4)
	normals.fill(direction_data[1])
	var uvs:Array = uv_coords
	
	return {
		"vertices" : [
			vertices[0], vertices[1], vertices[2],
			vertices[0], vertices[2], vertices[3],
		],
		"normals" : [
			normals[0], normals[1], normals[2],
			normals[0], normals[2], normals[3],
		],
		"uvs" : [
			uvs[0], uvs[1], uvs[2],
			uvs[0], uvs[2], uvs[3],
		]
	}

## private methods
