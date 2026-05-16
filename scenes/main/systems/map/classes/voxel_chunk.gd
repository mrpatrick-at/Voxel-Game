@tool
extends MeshInstance3D
class_name VoxelChunk
## enums
## consts
## exports
@export var cube_size: float = 1.0
## public vars
var cube_mesh: ArrayMesh
var voxels:Array = []
var uvs:Array = [0,0,0,0,0,0]
## private vars
## onready vars
# obj_ for node refrences
## built-in override methods
## public methods

func _ready() -> void:
	setup(Vector2i.ZERO,16,20,FastNoiseLite.new())

func setup(chunk_coord:Vector2i, chunk_size:int, world_height:int, noise:FastNoiseLite) -> void:
	var start_time := Time.get_ticks_usec()
	print("Voxel_Chunk- Setup Called")
	
	global_position = Vector3(chunk_coord.x << 4, 0, chunk_coord.y << 4)
	
	voxels = make_chunk(chunk_coord, chunk_size, world_height, noise)
	
	generate_mesh()
	
	var time_taken := (Time.get_ticks_usec() - start_time) / 1000.0
	print("Voxel_Chunk- Chunk Made in: %s msec"%time_taken)

func make_chunk(chunk_coord:Vector2i, chunk_size:int, world_height:int, noise:FastNoiseLite) -> Array:
	var start_time := Time.get_ticks_usec()
	
	var voxel_array:Array = []
	voxel_array.resize(chunk_size)
	
	for x in chunk_size:
		voxel_array[x] = []
		voxel_array[x].resize(world_height)
		
		for y in world_height:
			voxel_array[x][y] = []
			voxel_array[x][y].resize(chunk_size)
	
	for x:int in chunk_size:
		for z:int in chunk_size:
			var pixel_data:float = -noise.get_noise_2d(x + chunk_coord.x * chunk_size, z + chunk_coord.y * chunk_size)
			var tile_height:int = snappedi(pixel_data*10,1) + 10
			
			for y in world_height:
				if y - 1 < tile_height: # CAUTION Temp fix: y + 1. Fixes Tiles at height 0 not exisitng. Improve later (or maybe this is best solution idk)
					voxel_array[x][y][z] = 1
					continue
				voxel_array[x][y][z] = 0
	
	var time_taken := (Time.get_ticks_usec() - start_time) / 1000.0
	print("Voxel_Chunk- Chunk Data Made in: %s msec"%time_taken)
	return voxel_array

func generate_mesh() -> void:
	var start_time := Time.get_ticks_usec()
	var faces:Array = []
	
	var horizontal_bitmap:BitMap = BitMap.new()
	horizontal_bitmap.resize(Vector2i(64,64))
	
	#var bitmap:BitMap = BitMap.new()
	#bitmap.resize(Vector2i(15,15))
	
	for x in range(voxels.size()):
		for y in range(voxels[x].size()):
			
			#if voxels[x][y][0] == 1:
				#bitmap.set_bit(x,y,true)
			#else:
				#bitmap.set_bit(x,y,false)
			
			for z in range(voxels[x][y].size()):
				pass
	
	var x_visited:Dictionary = {}
	var visited_y:Dictionary = {}
	var visited_z:Dictionary = {}
	
	var x:int = 0
	var y:int = 0
	var z:int = 0
	
	var y_ending_position:Vector3 = Vector3(x, y, z) * cube_size
	var z_ending_position:Vector3 = Vector3(x, y, z) * cube_size
	
	var y_offset:int = y
	var z_offset:int = z
	
	var is_building:bool = true
	
	while is_building:
		
		if x >= 15:
			is_building = false
		
		if voxels[x][y][z] == 1 and !x_visited.has(Vector3(x, y, z)):
			x_visited[Vector3(x, y, z)] = true
			
			var x_ending:int = x
			
			while voxels.size() >= x_ending:
				x_visited[Vector3(x_ending, y, z)] = true
				x_ending += 1
				print("ending ran ",x_ending," times")
			
			var starting_position:Vector3 = Vector3(x, y, z) * cube_size
			var x_ending_position:Vector3 = Vector3(x_ending, y, z) * cube_size
			print("is appending")
			
			faces.append(create_face(Vector3.DOWN, starting_position, x_ending_position, uvs))
			faces.append(create_face(Vector3.UP, starting_position, x_ending_position, uvs))
		
		x += 1
		
		print("ran ",x," times")
	
	#for x in range(voxels.size()):
		#for y in range(voxels[x].size()):
			#for z in range(voxels[x][y].size()):
				#if voxels[x][y][z] == 0:
					#continue
				#
				#var starting_position:Vector3 = Vector3(x, y, z) * cube_size
				#
				#var ending_position:Vector3 = Vector3(x, y, z) * cube_size
				#
				#var z_ending_position:Vector3 = Vector3(x, y, z) * cube_size
				#var z_offset:int = z
				#
				#while z_offset < 15 and !visited_z.has(Vector3(x, y, z_offset)):
					#visited_z[Vector3(x, y, z_offset)] = true
					#
					#if voxels[x][y][z] == 1:
						#
						#if  voxels[x][y][z_offset] == 0:
							#z_ending_position = Vector3(x, y, z_offset) * cube_size
							#faces.append(create_face(Vector3.DOWN, starting_position, z_ending_position, uvs))
						#
					#z_offset += 1
					#
				#
				#if x == 0 or voxels[x - 1][y][z] == 0:
					#faces.append(create_face(Vector3.LEFT, starting_position, ending_position, uvs))
				#
				#if x == voxels.size() - 1 or voxels[x + 1][y][z] == 0:
					#faces.append(create_face(Vector3.RIGHT, starting_position, ending_position, uvs))
				#
				##if y == 0 or voxels[x][y - 1][z] == 0:
					##faces.append(create_face(Vector3.DOWN, starting_position, ending_position, uvs))
				#
				#if y == voxels[x].size() - 1 or voxels[x][y + 1][z] == 0:
					#faces.append(create_face(Vector3.UP, starting_position, ending_position, uvs))
				#
				#if z == 0 or voxels[x][y][z - 1] == 0:
					#faces.append(create_face(Vector3.FORWARD, starting_position, ending_position, uvs))
				#
				#if z == voxels.size() - 1 or voxels[x][y][z + 1] == 0:
					#faces.append(create_face(Vector3.BACK, starting_position, ending_position, uvs))
	
	var vertices:Array = []
	var normals:Array = []
	var uvs:Array = []
	
	for face:Dictionary in faces:
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

func create_face(direction:Vector3, starting_position:Vector3, ending_position:Vector3, uv_coords:Array) -> Dictionary:
	var starting_vertices:Array = []
	var ending_vertices:Array = []
	var normals:Array = []
	var uvs:Array = []
	
	normals.resize(4)
	
	var vertices:Array = []
	
	match direction:
		Vector3.UP:
			vertices = [
				starting_position + Vector3(-0.5,  0.5, -0.5) * cube_size,
				ending_position + Vector3( 0.5,  0.5, -0.5) * cube_size,
				ending_position + Vector3( 0.5,  0.5,  0.5) * cube_size,
				starting_position + Vector3(-0.5,  0.5,  0.5) * cube_size
			]
			
			starting_vertices = [
				starting_position + Vector3(-0.5,  0.5, -0.5) * cube_size,
				starting_position + Vector3( 0.5,  0.5, -0.5) * cube_size,
				starting_position + Vector3( 0.5,  0.5,  0.5) * cube_size,
			]
			ending_vertices = [
				ending_position + Vector3(-0.5,  0.5, -0.5) * cube_size,
				ending_position + Vector3( 0.5,  0.5,  0.5) * cube_size,
				ending_position + Vector3(-0.5,  0.5,  0.5) * cube_size
			]
			normals.fill(Vector3.UP)
			uvs = uv_coords
		
		Vector3.DOWN:
			vertices = [
				starting_position + Vector3(-0.5, -0.5,  0.5) * cube_size, # shared 1
				ending_position + Vector3( 0.5, -0.5,  0.5) * cube_size, # starting unique
				ending_position + Vector3( 0.5, -0.5, -0.5) * cube_size, # shared 2
				starting_position + Vector3(-0.5, -0.5, -0.5) * cube_size # ending unique
			]
			
			starting_vertices = [
				starting_position + Vector3(-0.5, -0.5,  0.5) * cube_size, # shared 1
				starting_position + Vector3( 0.5, -0.5,  0.5) * cube_size, # unique
				starting_position + Vector3( 0.5, -0.5, -0.5) * cube_size, # shared 2
			]
			ending_vertices = [
				ending_position + Vector3(-0.5, -0.5,  0.5) * cube_size, # shared 1
				ending_position + Vector3( 0.5, -0.5, -0.5) * cube_size, # shared 2
				ending_position + Vector3(-0.5, -0.5, -0.5) * cube_size # unique
			]
			normals.fill(Vector3.DOWN)
			uvs = uv_coords
		
		Vector3.LEFT:
			starting_vertices = [
				starting_position + Vector3(-0.5, -0.5, -0.5) * cube_size,
				starting_position + Vector3(-0.5,  0.5, -0.5) * cube_size,
				starting_position + Vector3(-0.5,  0.5,  0.5) * cube_size,
			]
			ending_vertices = [
				ending_position + Vector3(-0.5, -0.5, -0.5) * cube_size,
				ending_position + Vector3(-0.5,  0.5,  0.5) * cube_size,
				ending_position + Vector3(-0.5, -0.5,  0.5) * cube_size
			]
			normals.fill(Vector3.LEFT)
			uvs = uv_coords
		
		Vector3.RIGHT:
			starting_vertices = [
				starting_position + Vector3( 0.5, -0.5,  0.5) * cube_size,
				starting_position + Vector3( 0.5,  0.5,  0.5) * cube_size,
				starting_position + Vector3( 0.5,  0.5, -0.5) * cube_size,
			]
			ending_vertices = [
				ending_position + Vector3( 0.5, -0.5,  0.5) * cube_size,
				ending_position + Vector3( 0.5,  0.5,  0.5) * cube_size,
				ending_position + Vector3( 0.5,  0.5, -0.5) * cube_size,
			]
			normals.fill(Vector3.RIGHT)
			uvs = uv_coords
		
		Vector3.FORWARD:
			starting_vertices = [
				starting_position + Vector3(-0.5, -0.5, -0.5) * cube_size,
				starting_position + Vector3( 0.5, -0.5, -0.5) * cube_size,
				starting_position + Vector3( 0.5,  0.5, -0.5) * cube_size,
			]
			ending_vertices = [
				ending_position + Vector3(-0.5, -0.5, -0.5) * cube_size,
				ending_position + Vector3( 0.5,  0.5, -0.5) * cube_size,
				ending_position + Vector3(-0.5,  0.5, -0.5) * cube_size
			]
			normals.fill(Vector3.FORWARD)
			uvs = uv_coords
		
		Vector3.BACK:
			starting_vertices = [
				starting_position + Vector3(-0.5,  0.5,  0.5) * cube_size,
				starting_position + Vector3( 0.5,  0.5,  0.5) * cube_size,
				starting_position + Vector3( 0.5, -0.5,  0.5) * cube_size,
			]
			ending_vertices = [
				ending_position + Vector3(-0.5,  0.5,  0.5) * cube_size,
				ending_position + Vector3( 0.5, -0.5,  0.5) * cube_size,
				ending_position + Vector3(-0.5, -0.5,  0.5) * cube_size
			]
			normals.fill(Vector3.BACK)
			uvs = uv_coords
	
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
