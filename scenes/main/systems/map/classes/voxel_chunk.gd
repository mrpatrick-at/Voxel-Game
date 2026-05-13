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

func setup(chunk_coord:Vector2i, chunk_size:int, world_height:int, noise:FastNoiseLite) -> void:
	print("VoxelChunk setup called")
	global_position = Vector3(chunk_coord.x << 6, 0, chunk_coord.y << 6)
	
	voxels = make_chunk(chunk_coord, chunk_size, world_height, noise)
	
	generate_mesh()

func make_chunk(chunk_coord:Vector2i, chunk_size:int, world_height:int, noise:FastNoiseLite) -> Array:
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
			
			#for y in world_height:
				#if y != tile_height:
					#voxel_array[x][y][z] = 0
				#else:
					#voxel_array[x][tile_height][z] = 1
			
			for y in world_height:
				if y < tile_height:
					voxel_array[x][y][z] = 1
					continue
				voxel_array[x][y][z] = 0
	
	return voxel_array

func generate_mesh() -> void:
	var faces:Array = []
	
	var horizontal_bitmap:BitMap = BitMap.new()
	horizontal_bitmap.resize(Vector2i(64,64))
	
	for x in range(voxels.size()):
		for y in range(voxels[x].size()):
			for z in range(voxels[x][y].size()):
				pass
	
	var visited:Dictionary = {}
	
	for x in range(voxels.size()):
		for y in range(voxels[x].size()):
			for z in range(voxels[x][y].size()):
				if visited.has(Vector3(x,y,z)) or voxels[x][y][z] == 0:
					continue
				
				visited[Vector3(x,y,z)] = true
				
				var position_starting:Vector3 = Vector3(x, y, z) * cube_size
				
				var position_ending:Vector3 = Vector3(x, y, z) * cube_size
				
				if x == 0 or voxels[x - 1][y][z] == 0:
					faces.append(create_face(Vector3.LEFT, position_starting, position_ending, uvs))
				
				if x == voxels.size() - 1 or voxels[x + 1][y][z] == 0:
					faces.append(create_face(Vector3.RIGHT, position_starting, position_ending, uvs))
				
				if y == 0 or voxels[x][y - 1][z] == 0:
					faces.append(create_face(Vector3.DOWN, position_starting, position_ending, uvs))
				
				if y == voxels[x].size() - 1 or voxels[x][y + 1][z] == 0:
					faces.append(create_face(Vector3.UP, position_starting, position_ending, uvs))
				
				if z == 0 or voxels[x][y][z - 1] == 0:
					faces.append(create_face(Vector3.FORWARD, position_starting, position_ending, uvs))
				
				if z == voxels.size() - 1 or voxels[x][y][z + 1] == 0:
					faces.append(create_face(Vector3.BACK, position_starting, position_ending, uvs))
	
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

func create_face(direction:Vector3, position_starting:Vector3, position_ending:Vector3, uv_coords:Array) -> Dictionary:
	var vertices:Array = []
	var normals:Array = []
	var uvs:Array = []
	
	normals.resize(4)
	
	for num in 1:
		var position:Vector3 = Vector3.ZERO
		
		if 0:
			position = position_starting
		
		else:
			position = position_ending
		
		match direction:
			Vector3.UP:
				vertices = [
					position + Vector3(-0.5,  0.5, -0.5) * cube_size,
					position + Vector3( 0.5,  0.5, -0.5) * cube_size,
					position + Vector3( 0.5,  0.5,  0.5) * cube_size,
					position + Vector3(-0.5,  0.5,  0.5) * cube_size
				]
				normals.fill(Vector3.UP)
				uvs = uv_coords
			
			Vector3.DOWN:
				vertices = [
					position + Vector3(-0.5, -0.5,  0.5) * cube_size,
					position + Vector3( 0.5, -0.5,  0.5) * cube_size,
					position + Vector3( 0.5, -0.5, -0.5) * cube_size,
					position + Vector3(-0.5, -0.5, -0.5) * cube_size
				]
				normals.fill(Vector3.DOWN)
				uvs = uv_coords
			
			Vector3.LEFT:
				vertices = [
					position + Vector3(-0.5, -0.5, -0.5) * cube_size,
					position + Vector3(-0.5,  0.5, -0.5) * cube_size,
					position + Vector3(-0.5,  0.5,  0.5) * cube_size,
					position + Vector3(-0.5, -0.5,  0.5) * cube_size
				]
				normals.fill(Vector3.LEFT)
				uvs = uv_coords
			
			Vector3.RIGHT:
				vertices = [
					position + Vector3( 0.5, -0.5,  0.5) * cube_size,
					position + Vector3( 0.5,  0.5,  0.5) * cube_size,
					position + Vector3( 0.5,  0.5, -0.5) * cube_size,
					position + Vector3( 0.5, -0.5, -0.5) * cube_size
				]
				normals.fill(Vector3.RIGHT)
				uvs = uv_coords
			
			Vector3.FORWARD:
				vertices = [
					position + Vector3(-0.5, -0.5, -0.5) * cube_size,
					position + Vector3( 0.5, -0.5, -0.5) * cube_size,
					position + Vector3( 0.5,  0.5, -0.5) * cube_size,
					position + Vector3(-0.5,  0.5, -0.5) * cube_size
				]
				normals.fill(Vector3.FORWARD)
				uvs = uv_coords
			
			Vector3.BACK:
				vertices = [
					position + Vector3(-0.5,  0.5,  0.5) * cube_size,
					position + Vector3( 0.5,  0.5,  0.5) * cube_size,
					position + Vector3( 0.5, -0.5,  0.5) * cube_size,
					position + Vector3(-0.5, -0.5,  0.5) * cube_size
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
