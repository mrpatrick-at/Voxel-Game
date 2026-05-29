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
var faces:Dictionary[Vector3,PackedVector3Array] = {}
var placeholder_uvs:Array = [0,0,0,0,0,0]

var is_empty:bool = true
var is_full:bool = true
var has_faces:bool = false
## private vars
## onready vars
# obj_ for node refrences
## built-in override methods
## public methods
func setup(chunk_coord:Vector3i) -> void:
	global_position = Vector3(chunk_coord.x << 4, chunk_coord.y << 4, chunk_coord.z << 4)

func generate(chunk_coord:Vector3i, chunk_size:int, height_map:PackedByteArray) -> void:
	#var start_time := Time.get_ticks_usec()
	#print("Voxel_Chunk- Chunk %s Called Setup"%chunk_coord)
	
	voxels = make_voxels(chunk_coord, chunk_size, height_map)
	
	if !is_empty and !is_full:
		
		faces = check_faces(chunk_size)
		
		for direction in faces:
			if !faces[direction].is_empty():
				has_faces = true
				break
		
		if has_faces:
			var mesh_array:Array = generate_mesh()
			
			cube_mesh = ArrayMesh.new()
			cube_mesh.add_surface_from_arrays(Mesh.PRIMITIVE_TRIANGLES, mesh_array)
			
			apply_mesh()
	
	#var time_taken := (Time.get_ticks_usec() - start_time) / 1000.0
	#print("Voxel_Chunk- Chunk %s Made in: %s msec"%[chunk_coord,time_taken])

func make_voxels(chunk_coord:Vector3i, chunk_size:int, height_map:PackedByteArray) -> PackedByteArray:
	#var start_time := Time.get_ticks_usec()
	
	var voxel_array:PackedByteArray = []
	voxel_array.resize((chunk_size + 2) * (chunk_size + 2) * (chunk_size + 2))
	
	for x:int in chunk_size + 2:
		for y:int in chunk_size + 2:
			for z:int in chunk_size + 2:
				if y < height_map[x + z * (chunk_size + 2)] - chunk_coord.y * (chunk_size):
					
					voxel_array[x + (y * (chunk_size + 2)) + (z * (chunk_size + 2) * (chunk_size + 2))] = Constants.SQUARE_TYPE.GRASS
					is_empty = false
					continue
				is_full = false
	#var time_taken := (Time.get_ticks_usec() - start_time) / 1000.0
	#print("Voxel_Chunk- Chunk Data Made in: %s msec"%time_taken)
	return voxel_array

func check_faces(chunk_size:int) -> Dictionary:
	#var start_time := Time.get_ticks_usec()
	var face_data:Dictionary[Vector3,PackedVector3Array] = {
		Vector3.RIGHT : [],
		Vector3.LEFT : [],
		Vector3.UP : [],
		Vector3.DOWN : [],
		Vector3.BACK : [],
		Vector3.FORWARD : [],
	}
	
	for x:int in chunk_size:
		for y:int in chunk_size:
			for z:int in chunk_size:
				if voxels[x + (y * (chunk_size + 2)) + (z * (chunk_size + 2) * (chunk_size + 2))] == 0:
					
					if voxels[(x + 1) + (y * (chunk_size + 2)) + (z * (chunk_size + 2) * (chunk_size + 2))] != Constants.SQUARE_TYPE.AIR:
						face_data[Vector3.LEFT].append(Vector3i(x + 1, y, z))
				
					if voxels[x + ((y + 1) * (chunk_size + 2)) + (z * (chunk_size + 2) * (chunk_size + 2))] != Constants.SQUARE_TYPE.AIR:
						face_data[Vector3.DOWN].append(Vector3i(x, y + 1, z))
					
					if voxels[x + (y * (chunk_size + 2)) + ((z + 1) * (chunk_size + 2) * (chunk_size + 2))] != Constants.SQUARE_TYPE.AIR:
						face_data[Vector3.FORWARD].append(Vector3i(x, y, z + 1))
					
					continue
				var coord:Vector3i = Vector3i(x, y, z)
				
				if voxels[(x + 1) + (y * (chunk_size + 2)) + (z * (chunk_size + 2) * (chunk_size + 2))] == Constants.SQUARE_TYPE.AIR:
					face_data[Vector3.RIGHT].append(coord)
				
				if voxels[x + ((y + 1) * (chunk_size + 2)) + (z * (chunk_size + 2) * (chunk_size + 2))] == Constants.SQUARE_TYPE.AIR:
					face_data[Vector3.UP].append(coord)
				
				if voxels[x + (y * (chunk_size + 2)) + ((z + 1) * (chunk_size + 2) * (chunk_size + 2))] == Constants.SQUARE_TYPE.AIR:
					face_data[Vector3.BACK].append(coord)
	
	#var time_taken := (Time.get_ticks_usec() - start_time) / 1000.0
	#print("Voxel_Chunk- Checked Faces in: %s msec"%time_taken)
	return face_data

func generate_mesh() -> Array: # ~ upto 6msec ATTENTION: PROBLEM
	var start_time := Time.get_ticks_usec()
	
	var positions:Dictionary = greedy_mesher()
	
	var dir_size:int = 0
	
	for direction:Vector3 in positions:
		dir_size += positions[direction].size()
	
	dir_size *= 6
	
	var vertex_array:PackedVector3Array = PackedVector3Array()
	var normal_array:PackedVector3Array = PackedVector3Array()
	var uv_array:PackedVector3Array = PackedVector3Array()
	var indices:PackedInt32Array = PackedInt32Array()
	
	vertex_array.resize(dir_size)
	normal_array.resize(dir_size)
	uv_array.resize(dir_size)
	indices.resize(dir_size)
	
	var index:int = 0
	var indices_index:int = 0
	
	var start_time_2 := Time.get_ticks_usec()
	# ATTENTION: The Following Code Block is the Performance Killer.
	for direction:Vector3 in positions:
		for pos:Vector3i in positions[direction]:
			var mesh_face:Dictionary[int,PackedVector3Array] = create_face(direction, pos, positions[direction][pos], placeholder_uvs)
			for i:int in 4:
				vertex_array[index + i] = mesh_face[Constants.FACE.VERTICES][i]
				normal_array[index + i] = mesh_face[Constants.FACE.NORMALS][i]
				uv_array[index + i] = mesh_face[Constants.FACE.UVS][i]
			
			indices[indices_index] = index
			indices[indices_index + 1] = index + 1
			indices[indices_index + 2] = index + 2
			indices[indices_index + 3] = index
			indices[indices_index + 4] = index + 2
			indices[indices_index + 5] = index + 3
			
			index += 4
			indices_index += 6
	# ATTENTION: That was the Performance Killer.
	
	var time_taken_2 := (Time.get_ticks_usec() - start_time_2) / 1000.0
	print("Voxel_Chunk- Indices Made in: %s msec"%time_taken_2)
	
	var mesh_array:Array = []
	mesh_array.resize(Mesh.ARRAY_MAX)
	mesh_array[Mesh.ARRAY_VERTEX] = vertex_array
	mesh_array[Mesh.ARRAY_NORMAL] = normal_array
	mesh_array[Mesh.ARRAY_TEX_UV] =  uv_array
	mesh_array[Mesh.ARRAY_INDEX] = indices
	
	var time_taken := (Time.get_ticks_usec() - start_time) / 1000.0
	print("Voxel_Chunk- Mesh Made in: %s msec"%time_taken)
	return mesh_array

func greedy_mesher() -> Dictionary: # ~ 0.3 msec
	#var start_time := Time.get_ticks_usec()
	var positions:Dictionary[Vector3,Dictionary] = {}
	
	for direction:Vector3 in faces:
		positions[direction] = {}
		
		var first_next_tile:Vector3i = Vector3i.RIGHT
		var second_next_tile:Vector3i = Vector3i.BACK
		
		if direction == Vector3.RIGHT or direction == Vector3.LEFT:
			first_next_tile = Vector3i.UP
		
		for pos:Vector3i in faces[direction]:
			var ending_pos:Vector3i = pos
			
			var next_tile_array:Array = [pos]
			
			while faces[direction].has(ending_pos + first_next_tile):
				ending_pos += first_next_tile
				next_tile_array.append(ending_pos)
				faces[direction].erase(ending_pos)
			
			var can_shift:bool = true
			var next_shift:Vector3i = second_next_tile
			
			while faces[direction].has(ending_pos + second_next_tile):
				for tile:Vector3i in next_tile_array:
					
					if faces[direction].has(tile + next_shift):
						continue
					
					can_shift = false
					break
				
				if !can_shift:
					break
				
				for tile:Vector3i in next_tile_array:
					faces[direction].erase(tile + next_shift)
				
				ending_pos += second_next_tile
				next_shift += second_next_tile
			
			positions[direction].set(pos,ending_pos)
	
	#var time_taken := (Time.get_ticks_usec() - start_time) / 1000.0
	#print("Voxel_Chunk- Greedy Meshing Done in: %s msec"%time_taken)
	return positions

func create_face(direction:Vector3, starting_position:Vector3, ending_position:Vector3, uv_coords:Array) -> Dictionary: # ~ 0.001 msec
	var vertices_array:Dictionary[Vector3,PackedVector3Array]= {
		Vector3.RIGHT :
		[
			starting_position + Vector3(0.5, -0.5, -0.5) * cube_size, # Bottom Left
			Vector3(starting_position.x,starting_position.y,ending_position.z) + Vector3(0.5, -0.5,  0.5) * cube_size, # Bottom Right
			ending_position + Vector3(0.5,  0.5,  0.5) * cube_size, # Top Right
			Vector3(ending_position.x,ending_position.y,starting_position.z) + Vector3(0.5,  0.5, -0.5) * cube_size, # Top Left
		],
		Vector3.LEFT :
		[
			starting_position + Vector3(-0.5, -0.5, -0.5) * cube_size, # Bottom Left
			Vector3(ending_position.x,ending_position.y,starting_position.z) + Vector3(-0.5,  0.5, -0.5) * cube_size, # Top Left
			ending_position + Vector3(-0.5,  0.5,  0.5) * cube_size, # Top Right
			Vector3(starting_position.x,starting_position.y,ending_position.z) + Vector3(-0.5, -0.5,  0.5) * cube_size # Bottom Right
		],
		Vector3.UP :
		[
			starting_position + Vector3(-0.5,  0.5, -0.5) * cube_size,
			Vector3(ending_position.x,ending_position.y,starting_position.z) + Vector3( 0.5,  0.5, -0.5) * cube_size,
			ending_position + Vector3( 0.5,  0.5,  0.5) * cube_size,
			Vector3(starting_position.x,starting_position.y,ending_position.z) + Vector3(-0.5,  0.5,  0.5) * cube_size
		],
		Vector3.DOWN :
		[
			starting_position + Vector3(-0.5, -0.5,  -0.5) * cube_size,
			Vector3(starting_position.x,starting_position.y,ending_position.z) + Vector3( -0.5, -0.5,  0.5) * cube_size,
			ending_position + Vector3( 0.5, -0.5, 0.5) * cube_size,
			Vector3(ending_position.x,ending_position.y,starting_position.z) + Vector3(0.5, -0.5, -0.5) * cube_size
		],
		Vector3.BACK :
		[
			starting_position + Vector3(-0.5, -0.5, 0.5) * cube_size, # Bottom Left
			Vector3(starting_position.x,ending_position.y,starting_position.z) + Vector3(-0.5,  0.5, 0.5) * cube_size, # Top Left
			ending_position + Vector3(0.5,  0.5,  0.5) * cube_size, # Top Right
			Vector3(ending_position.x,starting_position.y,ending_position.z) + Vector3(0.5, -0.5,  0.5) * cube_size # Bottom Right
		],
		Vector3.FORWARD :
		[
			starting_position + Vector3(-0.5, -0.5, -0.5) * cube_size, # Bottom Left
			Vector3(ending_position.x,starting_position.y,ending_position.z) + Vector3(0.5, -0.5,  -0.5) * cube_size, # Bottom Right
			ending_position + Vector3(0.5,  0.5,  -0.5) * cube_size, # Top Right
			Vector3(starting_position.x,ending_position.y,starting_position.z) + Vector3(-0.5,  0.5, -0.5) * cube_size, # Top Left
		],
	}
	
	var vertices:Array = vertices_array[direction]
	var normals:Array = []
	normals.resize(4)
	normals.fill(direction)
	var uvs:Array = uv_coords
	
	var mesh_face:Dictionary[int,PackedVector3Array] = {
		Constants.FACE.VERTICES : PackedVector3Array([
			vertices[0], vertices[1], vertices[2], vertices[3],
		]),
		Constants.FACE.NORMALS : PackedVector3Array([
			normals[0], normals[1], normals[2], normals[3],
		]),
		Constants.FACE.UVS : PackedVector3Array([
			uvs[0], uvs[1], uvs[2], uvs[3],
		])
	}
	
	return mesh_face

func apply_mesh() -> void:
	self.mesh = cube_mesh
	
	#var static_body = StaticBody3D.new()
	#add_child(static_body)
	#var collision_shape = CollisionShape3D.new()
	#var chunk_collision:ConvexPolygonShape3D = cube_mesh.create_convex_shape()
	#collision_shape.shape = chunk_collision
	#static_body.set_collision_layer(1)
	#static_body.set_collision_mask(0)
	#static_body.add_child(collision_shape)
	
## private methods
