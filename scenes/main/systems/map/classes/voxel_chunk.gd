@tool
extends MeshInstance3D
class_name VoxelChunk
## enums
enum DIRECTION {
	RIGHT,
	LEFT,
	UP,
	DOWN,
	BACK,
	FORWARD,
}
enum VOXEL_TYPE {
	AIR,
	DIRT,
}
enum MESH {
	VERTICES,
	NORMALS,
	UVS,
	INDICES,
}
## consts
## exports
## public vars
var voxel_size: float = 0
var extended_chunk_size:int
var sq_extended_chunk_size:int

var cube_mesh:ArrayMesh
var local_heightmap:PackedByteArray = []
var voxels:PackedByteArray = []
var faces:Array[PackedVector3Array] = []
var greedy_faces:Array[PackedVector3Array] = []
var placeholder_uvs:Array = [0,0,0,0,0,0]

var is_empty:bool = true
var has_faces:bool = false
## private vars
## onready vars
# obj_ for node refrences
## built-in override methods
## public methods
func setup(chunk_coord:Vector3i) -> void:
	global_position = Vector3(chunk_coord.x << 4, chunk_coord.y << 4, chunk_coord.z << 4)

func generate(chunk_coord:Vector3i, chunk_size:int, height_map:PackedByteArray, cube_size:float) -> void:
	var start_time := Time.get_ticks_usec()
	print("Voxel_Chunk- Chunk %s Called Setup"%chunk_coord)
	
	voxel_size = cube_size
	extended_chunk_size = chunk_size + 2
	sq_extended_chunk_size = extended_chunk_size * extended_chunk_size
	
	local_heightmap = convert_heightmap(chunk_coord, chunk_size, height_map)
	
	voxels = make_voxels()
	
	if !is_empty:
		
		faces = check_faces(chunk_size)
		
		for direction:int in faces.size():
			if !faces[direction].is_empty():
				has_faces = true
				break
		
		if has_faces:
			greedy_faces = greedy_mesher()
			
			var mesh_array:Array = generate_mesh()
			
			cube_mesh = ArrayMesh.new()
			cube_mesh.add_surface_from_arrays(Mesh.PRIMITIVE_TRIANGLES, mesh_array)
			
			apply_mesh()
	
	var time_taken := (Time.get_ticks_usec() - start_time) / 1000.0
	print("Voxel_Chunk- Chunk %s Made in: %s msec"%[chunk_coord,time_taken])

func convert_heightmap(chunk_coord:Vector3i, chunk_size:int, height_map:PackedByteArray) -> PackedByteArray:
	var start_time := Time.get_ticks_usec()
	var converted_heigtmap:PackedByteArray = []
	converted_heigtmap.resize(extended_chunk_size * sq_extended_chunk_size)
	
	var conversion_int:int = chunk_coord.y * chunk_size
	
	for x:int in extended_chunk_size:
		for z:int in extended_chunk_size:
			var index:int = x + z * extended_chunk_size
			converted_heigtmap[index] = clampi(height_map[index] - conversion_int, 0, extended_chunk_size)
	
	var time_taken := (Time.get_ticks_usec() - start_time) / 1000.0
	print("Voxel_Chunk- Heightmap Conversion Done in: %s msec"%time_taken)
	return converted_heigtmap

func make_voxels() -> PackedByteArray:
	var start_time := Time.get_ticks_usec()
	
	var voxel_array:PackedByteArray = []
	
	voxel_array.resize(extended_chunk_size * sq_extended_chunk_size)
	for x:int in extended_chunk_size:
		for z:int in extended_chunk_size:
			for y:int in local_heightmap[x + z * extended_chunk_size]:
				
				voxel_array[x * extended_chunk_size + y + z * sq_extended_chunk_size] = VOXEL_TYPE.DIRT
				is_empty = false
	
	var time_taken := (Time.get_ticks_usec() - start_time) / 1000.0
	print("Voxel_Chunk- Chunk Data Made in: %s msec"%time_taken)
	return voxel_array

func check_faces(chunk_size:int) -> Array:
	var start_time := Time.get_ticks_usec()
	var face_data:Array[PackedVector3Array] = []
	face_data.resize(DIRECTION.size())
	
	for x:int in chunk_size:
		var x_next:int = x + 1
		var x_offset:int = x * extended_chunk_size
		var x_next_offset:int = x_next * extended_chunk_size
		for z:int in chunk_size:
			var z_next:int = z + 1
			var z_offset:int = z * sq_extended_chunk_size
			var z_next_offset:int = z_next * sq_extended_chunk_size
			var height:int = local_heightmap[x + z * extended_chunk_size]
			for y:int in height:
				
				var coord:Vector3i = Vector3i(x, y, z)
				
				if voxels[x_next_offset + y + z_offset] == VOXEL_TYPE.AIR:
					face_data[DIRECTION.RIGHT].append(coord)
				
				if voxels[x_offset + y + 1 + z_offset] == VOXEL_TYPE.AIR:
					face_data[DIRECTION.UP].append(coord)
				
				if voxels[x_offset + y + z_next_offset] == VOXEL_TYPE.AIR:
					face_data[DIRECTION.BACK].append(coord)
			
			var y:int = height + 1
			if voxels[x * extended_chunk_size + y + z * sq_extended_chunk_size] == VOXEL_TYPE.AIR:
				if voxels[x_next_offset + y + z_offset] != VOXEL_TYPE.AIR:
					face_data[DIRECTION.LEFT].append(Vector3i(x_next, y, z))
				
				if voxels[x_offset + y + 1 + z_offset] != VOXEL_TYPE.AIR:
					face_data[DIRECTION.DOWN].append(Vector3i(x, y + 1, z))
				
				if voxels[x_offset + y + z_next_offset] != VOXEL_TYPE.AIR:
					face_data[DIRECTION.FORWARD].append(Vector3i(x, y, z_next))
	
	var time_taken := (Time.get_ticks_usec() - start_time) / 1000.0
	print("Voxel_Chunk- Checked Faces in: %s msec"%time_taken)
	return face_data

func greedy_mesher() -> Array:
	var start_time := Time.get_ticks_usec()
	var greedy_output: Array[PackedVector3Array] = []
	greedy_output.resize(DIRECTION.size())
	
	for direction:int in faces.size():
		greedy_output[direction] = PackedVector3Array([])
		
		var first_next_tile:Vector3i = Vector3i.RIGHT
		var second_next_tile:Vector3i = Vector3i.BACK
		
		if direction == DIRECTION.RIGHT or direction == DIRECTION.LEFT:
			first_next_tile = Vector3i.UP
		
		for pos:Vector3i in faces[direction]:
			var ending_pos:Vector3i = pos
			
			var next_tile_array:PackedVector3Array = [pos]
			
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
			
			greedy_output[direction].append(pos)
			greedy_output[direction].append(ending_pos)
	
	var time_taken := (Time.get_ticks_usec() - start_time) / 1000.0
	print("Voxel_Chunk- Greedy Meshing Done in: %s msec"%time_taken)
	return greedy_output

func generate_mesh() -> Array:
	var start_time := Time.get_ticks_usec()
	
	var dir_size:int = 0
	
	for direction:int in greedy_faces.size():
		dir_size += greedy_faces[direction].size()
	
	dir_size *= 6
	
	var vertex_array:PackedVector3Array = PackedVector3Array()
	var normal_array:PackedVector3Array = PackedVector3Array()
	var uv_array:PackedVector3Array = PackedVector3Array()
	var indices_array:PackedInt32Array = PackedInt32Array()
	
	vertex_array.resize(dir_size)
	normal_array.resize(dir_size)
	uv_array.resize(dir_size)
	indices_array.resize(dir_size)
	
	var start_time_2 := Time.get_ticks_usec()
	
	var index:int = 0
	# ATTENTION: Slow af sometimes
	for direction:int in greedy_faces.size():
		
		var directional_faces:PackedVector3Array = greedy_faces[direction]
		
		var greedy_face_index:int = 0
		while greedy_face_index < directional_faces.size():
			var pos:Vector3i = directional_faces[greedy_face_index]
			var ending_pos:Vector3i = directional_faces[greedy_face_index + 1]
			
			var mesh_face:Array[PackedVector3Array] = create_face(direction, pos, ending_pos, placeholder_uvs)
			
			var index_offset:int = index << 2
			var indices_index:int = index_offset + (index << 1)
			
			var i:int = index_offset
			
			for vertice in mesh_face[MESH.VERTICES]:
				vertex_array[i] = vertice
				normal_array[i] = vertice
				uv_array[i] = vertice
				i += 1
			
			var point_1:int = index_offset + 1
			var point_2:int = index_offset + 2
			var point_3:int = index_offset + 3
			
			indices_array[indices_index] = index_offset
			indices_array[indices_index + 1] = point_1
			indices_array[indices_index + 2] = point_2
			indices_array[indices_index + 3] = index_offset
			indices_array[indices_index + 4] = point_2
			indices_array[indices_index + 5] = point_3
			
			index += 1
			greedy_face_index += 2
	# ATTENTION: That was the Performance Killer.
	
	var time_taken_2 := (Time.get_ticks_usec() - start_time_2) / 1000.0
	print("Voxel_Chunk- Indices Made in: %s msec"%time_taken_2)
	
	var mesh_array:Array = []
	mesh_array.resize(Mesh.ARRAY_MAX)
	mesh_array[Mesh.ARRAY_VERTEX] = vertex_array
	mesh_array[Mesh.ARRAY_NORMAL] = normal_array
	mesh_array[Mesh.ARRAY_TEX_UV] =  uv_array
	mesh_array[Mesh.ARRAY_INDEX] = indices_array
	
	var time_taken := (Time.get_ticks_usec() - start_time) / 1000.0 - time_taken_2
	print("Voxel_Chunk- Mesh Made in: %s msec"%time_taken)
	return mesh_array

func create_face(direction:int, starting_position:Vector3, ending_position:Vector3, uv_coords:Array) -> Array:
	var direction_array:Array = [
		Vector3.RIGHT,
		Vector3.LEFT,
		Vector3.UP,
		Vector3.DOWN,
		Vector3.BACK,
		Vector3.FORWARD
	]
	var vertices_array: Array[Array] = [
		[
			starting_position + Vector3(0.5, -0.5, -0.5) * voxel_size, # Bottom Left
			Vector3(starting_position.x,starting_position.y,ending_position.z) + Vector3(0.5, -0.5,  0.5) * voxel_size, # Bottom Right
			ending_position + Vector3(0.5,  0.5,  0.5) * voxel_size, # Top Right
			Vector3(ending_position.x,ending_position.y,starting_position.z) + Vector3(0.5,  0.5, -0.5) * voxel_size, # Top Left
		],
		[
			starting_position + Vector3(-0.5, -0.5, -0.5) * voxel_size, # Bottom Left
			Vector3(ending_position.x,ending_position.y,starting_position.z) + Vector3(-0.5,  0.5, -0.5) * voxel_size, # Top Left
			ending_position + Vector3(-0.5,  0.5,  0.5) * voxel_size, # Top Right
			Vector3(starting_position.x,starting_position.y,ending_position.z) + Vector3(-0.5, -0.5,  0.5) * voxel_size # Bottom Right
		],
		[
			starting_position + Vector3(-0.5,  0.5, -0.5) * voxel_size,
			Vector3(ending_position.x,ending_position.y,starting_position.z) + Vector3( 0.5,  0.5, -0.5) * voxel_size,
			ending_position + Vector3( 0.5,  0.5,  0.5) * voxel_size,
			Vector3(starting_position.x,starting_position.y,ending_position.z) + Vector3(-0.5,  0.5,  0.5) * voxel_size
		],
		[
			starting_position + Vector3(-0.5, -0.5,  -0.5) * voxel_size,
			Vector3(starting_position.x,starting_position.y,ending_position.z) + Vector3( -0.5, -0.5,  0.5) * voxel_size,
			ending_position + Vector3( 0.5, -0.5, 0.5) * voxel_size,
			Vector3(ending_position.x,ending_position.y,starting_position.z) + Vector3(0.5, -0.5, -0.5) * voxel_size
		],
		[
			starting_position + Vector3(-0.5, -0.5, 0.5) * voxel_size, # Bottom Left
			Vector3(starting_position.x,ending_position.y,starting_position.z) + Vector3(-0.5,  0.5, 0.5) * voxel_size, # Top Left
			ending_position + Vector3(0.5,  0.5,  0.5) * voxel_size, # Top Right
			Vector3(ending_position.x,starting_position.y,ending_position.z) + Vector3(0.5, -0.5,  0.5) * voxel_size # Bottom Right
		],
		[
			starting_position + Vector3(-0.5, -0.5, -0.5) * voxel_size, # Bottom Left
			Vector3(ending_position.x,starting_position.y,ending_position.z) + Vector3(0.5, -0.5,  -0.5) * voxel_size, # Bottom Right
			ending_position + Vector3(0.5,  0.5,  -0.5) * voxel_size, # Top Right
			Vector3(starting_position.x,ending_position.y,starting_position.z) + Vector3(-0.5,  0.5, -0.5) * voxel_size, # Top Left
		],
	]
	
	var vertices:Array = vertices_array[direction]
	var vec3_direction:Vector3 = direction_array[direction]
	var normals:Array = []
	normals.resize(4)
	normals.fill(vec3_direction)
	var uvs:Array = uv_coords
	
	var mesh_face:Array[PackedVector3Array] = [
		PackedVector3Array([
			vertices[0], vertices[1], vertices[2], vertices[3],
		]),
		PackedVector3Array([
			normals[0], normals[1], normals[2], normals[3],
		]),
		PackedVector3Array([
			uvs[0], uvs[1], uvs[2], uvs[3],
		])
	]
	
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
