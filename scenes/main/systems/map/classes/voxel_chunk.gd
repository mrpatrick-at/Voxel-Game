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
var placeholder_uvs:Array = [0,0,0,0,0,0]
## private vars
## onready vars
# obj_ for node refrences
## built-in override methods
## public methods

#func _ready() -> void:
	#if Engine.is_editor_hint():
		#setup(Vector2i.ZERO,16,20,FastNoiseLite.new())

func setup(chunk_coord:Vector2i, chunk_size:int, world_height:int, noise:FastNoiseLite) -> void:
	var start_time := Time.get_ticks_usec()
	print("Voxel_Chunk- Chunk %s Called Setup"%chunk_coord)
	
	global_position = Vector3(chunk_coord.x << 4, 0, chunk_coord.y << 4)
	
	voxels = make_chunk(chunk_coord, chunk_size, world_height, noise)
	
	generate_mesh()
	
	var time_taken := (Time.get_ticks_usec() - start_time) / 1000.0
	print("Voxel_Chunk- Chunk %s Made in: %s msec"%[chunk_coord,time_taken])

func make_chunk(chunk_coord:Vector2i, chunk_size:int, world_height:int, noise:FastNoiseLite) -> Array:
	#var start_time := Time.get_ticks_usec()
	
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
			var tile_height:int = int((pixel_data + 1) * 0.5 * world_height + 1)
			
			for y in world_height:
				if y < tile_height:
					voxel_array[x][y][z] = 1
					continue
				voxel_array[x][y][z] = 0
	
	#var time_taken := (Time.get_ticks_usec() - start_time) / 1000.0
	#print("Voxel_Chunk- Chunk Data Made in: %s msec"%time_taken)
	return voxel_array

func generate_mesh() -> void:
	#var start_time := Time.get_ticks_usec()
	var faces:Array = []
	
	var horizontal_bitmap:BitMap = BitMap.new()
	horizontal_bitmap.resize(Vector2i(64,64))
	
	var x_positions:Dictionary = _x_build()
	for direction:Vector3 in x_positions:
		for pos:Vector3 in x_positions[direction].keys():
			#print("helly eah x",pos,x_positions[direction][pos])
			faces.append(create_face(direction, pos, x_positions[direction][pos], placeholder_uvs))
	
	var y_positions:Dictionary = _y_build()
	for direction:Vector3 in y_positions:
		for pos:Vector3 in y_positions[direction].keys():
			#print("helly eah y",pos,y_positions[direction][pos])
			faces.append(create_face(direction, pos, y_positions[direction][pos], placeholder_uvs))
	
	var z_positions:Dictionary = _z_build()
	for direction:Vector3 in z_positions:
		for pos:Vector3 in z_positions[direction].keys():
			#print("helly eah y",pos,y_positions[direction][pos])
			faces.append(create_face(direction, pos, z_positions[direction][pos], placeholder_uvs))
	
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

	#var time_taken := (Time.get_ticks_usec() - start_time) / 1000.0
	#print("Voxel_Chunk- Mesh Made in: %s msec"%time_taken)

func create_face(direction:Vector3, starting_position:Vector3, ending_position:Vector3, uv_coords:Array) -> Dictionary:
	var normals:Array = []
	var uvs:Array = []
	
	normals.resize(4)
	
	var vertices:Array = []
	
	match direction:
		Vector3.UP:
			vertices = [
				starting_position + Vector3(-0.5,  0.5, -0.5) * cube_size,
				Vector3(ending_position.x,ending_position.y,starting_position.z) + Vector3( 0.5,  0.5, -0.5) * cube_size,
				ending_position + Vector3( 0.5,  0.5,  0.5) * cube_size,
				Vector3(starting_position.x,starting_position.y,ending_position.z) + Vector3(-0.5,  0.5,  0.5) * cube_size
			]
			normals.fill(Vector3.UP)
			uvs = uv_coords
		
		Vector3.DOWN:
			vertices = [
				starting_position + Vector3(-0.5, -0.5,  -0.5) * cube_size,
				Vector3(starting_position.x,starting_position.y,ending_position.z) + Vector3( -0.5, -0.5,  0.5) * cube_size,
				ending_position + Vector3( 0.5, -0.5, 0.5) * cube_size,
				Vector3(ending_position.x,ending_position.y,starting_position.z) + Vector3(0.5, -0.5, -0.5) * cube_size
			]
			normals.fill(Vector3.DOWN)
			uvs = uv_coords
		
		Vector3.RIGHT:
			vertices = [
				starting_position + Vector3(0.5, -0.5, -0.5) * cube_size, # Bottom Left
				Vector3(starting_position.x,starting_position.y,ending_position.z) + Vector3(0.5, -0.5,  0.5) * cube_size, # Bottom Right
				ending_position + Vector3(0.5,  0.5,  0.5) * cube_size, # Top Right
				Vector3(ending_position.x,ending_position.y,starting_position.z) + Vector3(0.5,  0.5, -0.5) * cube_size, # Top Left
			]
			normals.fill(Vector3.RIGHT)
			uvs = uv_coords
			
		Vector3.LEFT:
			vertices = [
				starting_position + Vector3(-0.5, -0.5, -0.5) * cube_size, # Bottom Left
				Vector3(ending_position.x,ending_position.y,starting_position.z) + Vector3(-0.5,  0.5, -0.5) * cube_size, # Top Left
				ending_position + Vector3(-0.5,  0.5,  0.5) * cube_size, # Top Right
				Vector3(starting_position.x,starting_position.y,ending_position.z) + Vector3(-0.5, -0.5,  0.5) * cube_size # Bottom Right
			]
			normals.fill(Vector3.LEFT)
			uvs = uv_coords
		
		Vector3.BACK:
			vertices = [
				starting_position + Vector3(-0.5, -0.5, 0.5) * cube_size, # Bottom Left
				Vector3(starting_position.x,ending_position.y,starting_position.z) + Vector3(-0.5,  0.5, 0.5) * cube_size, # Top Left
				ending_position + Vector3(0.5,  0.5,  0.5) * cube_size, # Top Right
				Vector3(ending_position.x,starting_position.y,ending_position.z) + Vector3(0.5, -0.5,  0.5) * cube_size # Bottom Right
			]
			normals.fill(Vector3.BACK)
			uvs = uv_coords
		
		Vector3.FORWARD:
			vertices = [
				starting_position + Vector3(-0.5, -0.5, -0.5) * cube_size, # Bottom Left
				Vector3(ending_position.x,starting_position.y,ending_position.z) + Vector3(0.5, -0.5,  -0.5) * cube_size, # Bottom Right
				ending_position + Vector3(0.5,  0.5,  -0.5) * cube_size, # Top Right
				Vector3(starting_position.x,ending_position.y,starting_position.z) + Vector3(-0.5,  0.5, -0.5) * cube_size, # Top Left
			]
			normals.fill(Vector3.FORWARD)
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

func _x_build() -> Dictionary:
	var positions_dict:Dictionary = {
		Vector3.RIGHT : {},
		Vector3.LEFT : {},
	}
	for direction:Vector3 in positions_dict:
		var direction_int:int = 1
		if direction == Vector3.LEFT:
			direction_int = -1
		
		var voxel_array:Array = voxels.duplicate(true)
		
		for x:int in range(voxels.size()):
			var y:int = 0
			var z:int = 0
			var is_building:bool = true
			
			var direction_x:int = clampi(x + direction_int,0,voxels.size() - 1)
			var is_chunk_border:bool = false
			if x == 0 and direction_int == -1 or x == voxels.size() - 1 and direction_int == 1:
				is_chunk_border = true
				#print("True slayy man x + direction is: ",direction_x)
			
			while is_building:
				
				if y >= voxels[x].size():
					y = 0
					z += 1
				if z >= voxels[x][y].size():
					is_building = false
					break
				
				if voxel_array[x][y][z] == 0 or !voxels[direction_x][y][z] == 0 and !is_chunk_border:
					#print("EMPTY: ",x," , ",y," , ",z)
					y += 1
					continue
				
				var y_ending:int = y
				var y_step_amount:int = 1
				
				while voxels[x].size() > y_ending:
					voxel_array[x][y_ending][z] = 0
					var next_y_ending:int = min(y_ending + 1, voxels[x].size() - 1)
						
					if voxel_array[x][next_y_ending][z] == 0 or !voxels[direction_x][next_y_ending][z] == 0 and !is_chunk_border: # TODO: Inter Chunk Meshing
						#print("OH GOD WHY")
						break
					
					y_ending += 1
					y_step_amount += 1
				
				var z_ending:int = z
				var can_shift:bool = true
				
				while voxels[x][y].size() > z_ending:
					for y_step:int in y_step_amount:
						if voxel_array[x][min(y_step + y,voxels[x].size() - 1)][min(z_ending + 1, voxels.size() - 1)] == 0 or voxels[direction_x][y_step - 1 + y][min(z_ending + 1, voxels.size() - 1)] == 1 and !is_chunk_border:
							can_shift = false
							#print("CANNOT SHIFT BRUV")
							break
					
					if !can_shift:
						break
					
					for y_step in y_step_amount:
						voxel_array[x][min(y_step + y,voxels[x].size() - 1)][min(z_ending + 1, voxels.size() - 1)] = 0
					
					z_ending += 1
				
				var starting_position:Vector3 = Vector3(x, y, z) * cube_size
				var ending_position:Vector3 = Vector3(x, y_ending, z_ending) * cube_size
				
				positions_dict[direction].set(starting_position,ending_position)
				
			#print("x = ",x)
	return positions_dict

func _y_build() -> Dictionary:
	var positions_dict:Dictionary = {
		Vector3.UP : {},
		Vector3.DOWN : {},
	}
	for direction:Vector3 in positions_dict:
		var direction_int:int = 1
		if direction == Vector3.DOWN:
			direction_int = -1
		
		var voxel_array:Array = voxels.duplicate(true)
		
		for y:int in voxels[0].size():
			var x:int = 0
			var z:int = 0
			var is_building:bool = true
			
			var direction_y:int = clampi(y + direction_int,0,voxels[x].size() - 1)
			var is_chunk_border:bool = false
			if y == 0 and direction_int == -1 or y >= voxels[x].size() - 1 and direction_int == 1:
				is_chunk_border = true
				#print("True slayy man x + direction is: ",direction_y)
			
			while is_building:
				
				if x == voxels.size():
					x = 0
					z += 1
				if z == voxels[x][y].size():
					is_building = false
					break
				
				if voxel_array[x][y][z] == 0 or !voxels[x][direction_y][z] == 0 and !is_chunk_border:
					x += 1
					continue
				
				var x_ending:int = x
				var x_step_amount:int = 1
				
				while voxels.size() > x_ending:
					voxel_array[x_ending][y][z] = 0
					var next_x_ending:int = min(x_ending + 1,voxels.size() -1)
					
					if voxel_array[next_x_ending][y][z] == 0 or !voxels[next_x_ending][direction_y][z] == 0 and !is_chunk_border: # TODO: Inter Chunk Meshing
						#print("OH GOD WHY")
						break
					x_ending += 1
					x_step_amount += 1
				
				var z_ending:int = z
				var can_shift:bool = true
				
				while voxels[x][y].size() - 1 >= z_ending:
					for x_step:int in x_step_amount:
						if voxel_array[min(x_step + x,voxels.size() - 1)][y][min(z_ending + 1, voxels.size() - 1)] == 0 or !voxels[x_step - 1 + x][direction_y][min(z_ending + 1, voxels.size() - 1)] == 0 and !is_chunk_border:
							can_shift = false
							#print("CANNOT SHIFT BRUV")
							break
					
					if !can_shift:
						break
					
					for x_step in x_step_amount:
						voxel_array[min(x_step + x,voxels.size() - 1)][y][min(z_ending + 1, voxels.size() - 1)] = 0
						
					z_ending += 1
				
				var starting_position:Vector3 = Vector3(x, y, z) * cube_size
				var ending_position:Vector3 = Vector3(x_ending, y, z_ending) * cube_size
				
				positions_dict[direction].set(starting_position,ending_position)
				
			#print("y = ",y)
	return positions_dict

func _z_build() -> Dictionary:
	var positions_dict:Dictionary = {
		Vector3.BACK : {},
		Vector3.FORWARD : {},
	}
	for direction:Vector3 in positions_dict:
		var direction_int:int = 1
		if direction == Vector3.FORWARD:
			direction_int = -1
		
		var voxel_array:Array = voxels.duplicate(true)
		
		for z:int in range(voxels.size()):
			var x:int = 0
			var y:int = 0
			var is_building:bool = true
			
			var direction_z:int = clampi(z + direction_int,0,voxels[x][y].size() - 1)
			var is_chunk_border:bool = false
			if z == 0 and direction_int == -1 or z == voxels[x][y].size() - 1 and direction_int == 1:
				is_chunk_border = true
				#print("True slayy man x + direction is: ",direction_z)
			
			while is_building:
				
				if x >= voxels.size():
					x = 0
					y += 1
				if y >= voxels[x].size():
					is_building = false
					break
				
				if voxel_array[x][y][z] == 0 or !voxels[x][y][direction_z] == 0 and !is_chunk_border:
					#print("EMPTY: ",x," , ",y," , ",z)
					x += 1
					continue
				
				var y_ending:int = y
				var y_step_amount:int = 1
				
				while voxels[x].size() > y_ending:
					voxel_array[x][y_ending][z] = 0
					var next_y_ending:int = min(y_ending + 1, voxels[x].size() - 1)
						
					if voxel_array[x][next_y_ending][z] == 0 or !voxels[x][next_y_ending][direction_z] == 0 and !is_chunk_border: # TODO: Inter Chunk Meshing
						#print("OH GOD WHY")
						break
					
					y_ending += 1
					y_step_amount += 1
				
				var x_ending:int = x
				var can_shift:bool = true
				
				while voxels.size() > x_ending:
					for y_step:int in y_step_amount:
						if voxel_array[min(x_ending + 1, voxels.size() - 1)][min(y_step + y,voxels[x].size() - 1)][z] == 0 or voxels[min(x_ending + 1, voxels.size() - 1)][y_step - 1 + y][direction_z] == 1 and !is_chunk_border:
							can_shift = false
							#print("CANNOT SHIFT BRUV")
							break
					
					if !can_shift:
						break
					
					for y_step in y_step_amount:
						voxel_array[min(x_ending + 1, voxels.size() - 1)][min(y_step + y,voxels[x].size() - 1)][z] = 0
					
					x_ending += 1
				
				var starting_position:Vector3 = Vector3(x, y, z) * cube_size
				var ending_position:Vector3 = Vector3(x_ending, y_ending, z) * cube_size
				
				positions_dict[direction].set(starting_position,ending_position)
				
			#print("x = ",x)
	return positions_dict
