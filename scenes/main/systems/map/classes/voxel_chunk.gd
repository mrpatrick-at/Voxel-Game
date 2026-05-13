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
func _ready() -> void:
	pass

## public methods

func setup(chunk_coord:Vector2i, chunk_size:int, world_height:int, noise:FastNoiseLite) -> void:
	print("VoxelChunk setup called")
	global_position = Vector3(chunk_coord.x << 6, 0, chunk_coord.y << 6)
	
	voxels = make_chunk(chunk_coord, chunk_size, world_height, noise)
	
	generate_mesh(voxels)

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
			
			print("tile_height: ", tile_height)
			
			for y in world_height:
				if y < tile_height:
					voxel_array[x][y][z] = 1
					continue
				voxel_array[x][y][z] = 0
				print("voxel: ", voxel_array[x][y][z])
	
	return voxel_array

#func generate_voxels() -> Array:
	#var voxel_array:Array = []
	#voxel_array.resize(chunk_width)
	#
	#for x in chunk_width:
		#voxel_array[x] = []
		#voxel_array[x].resize(chunk_height)
		#
		#for y in chunk_height:
			#voxel_array[x][y] = []
			#voxel_array[x][y].resize(chunk_depth)
	#
	#for x in chunk_width:
		#for y in chunk_height:
			#for z in chunk_depth:
				#voxel_array[x][y][z] = 1
	#
	#return voxel_array

func generate_mesh(voxels):
	var faces = []
	
	for x in range(voxels.size()):
		for y in range(voxels[x].size()):
			for z in range(voxels[x][y].size()):
				if voxels[x][y][z] != 0:
					var position:Vector3 = Vector3(x, y, z) * cube_size
					
					if x == 0 or voxels[x - 1][y][z] == 0:
						faces.append(create_face(Vector3.LEFT, position, uvs))
					
					if x == voxels.size() - 1 or voxels[x + 1][y][z] == 0:
						faces.append(create_face(Vector3.RIGHT, position, uvs))
					
					if y == 0 or voxels[x][y - 1][z] == 0:
						faces.append(create_face(Vector3.DOWN, position, uvs))
					
					if y == voxels[x].size() - 1 or voxels[x][y + 1][z] == 0:
						faces.append(create_face(Vector3.UP, position, uvs))
					
					if z == 0 or voxels[x][y][z - 1] == 0:
						faces.append(create_face(Vector3.FORWARD, position, uvs))
					
					if z == voxels.size() - 1 or voxels[x][y][z + 1] == 0:
						faces.append(create_face(Vector3.BACK, position, uvs))
	
	var vertices:Array = []
	var normals:Array = []
	var uvs:Array = []
	
	for face in faces:
		vertices += face ["vertices"]
		normals += face ["normals"]
		uvs += face ["uvs"]
	
	var vertex_array:PackedVector3Array = PackedVector3Array(vertices)
	var normal_array:PackedVector3Array = PackedVector3Array(normals)
	var uv_array:PackedVector3Array = PackedVector3Array(uvs)
	
	var arrays = []
	arrays.resize(Mesh.ARRAY_MAX)
	arrays[Mesh.ARRAY_VERTEX] = vertex_array
	arrays[Mesh.ARRAY_NORMAL] = normal_array
	arrays[Mesh.ARRAY_TEX_UV] =  uv_array
	
	cube_mesh = ArrayMesh.new()
	cube_mesh.add_surface_from_arrays(Mesh.PRIMITIVE_TRIANGLES, arrays)
	self.mesh = cube_mesh

func create_face(direction:Vector3, position:Vector3, uv_coords:Array) -> Dictionary:
	var vertices:Array = []
	var normals:Array = []
	var uvs:Array = []
	
	normals.resize(4)
	
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
