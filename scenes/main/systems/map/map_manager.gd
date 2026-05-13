extends Node3D
## enums
## consts
const world_height:int = 20
const world_chunk_width:int = 1
const world_chunk_length:int = 1
const chunk_size:int = 64
const mesh_library = preload("res://scenes/main/mesh_library.meshlib")
## exports
## public vars
## private vars
## onready vars
@onready var noise_viewer: TextureRect = $"../IngameUI/PanelContainer/VBoxContainer/NoiseViewer"
@onready var seed_label: Label = $"../IngameUI/PanelContainer/VBoxContainer/PanelContainer/HBoxContainer/SeedLabel"
# obj_ for node refrences
## built-in override methods

func _ready() -> void:
	pass

func _process(_delta: float) -> void:
	pass

func _on_button_new_pressed() -> void:
	_make_map(true)

func _on_button_load_pressed() -> void:
	_make_map(false)

## public methods

static func get_chunk_coords(global_pos: Vector3i) -> Vector2i:
	# Shift right by 7 is equivalent to dividing by 128 (128 cuz each tile is 2 so 2*64)..
	return Vector2i(global_pos.x >> 7, global_pos.y >> 7)

static func get_local_coords(global_pos: Vector3i) -> Vector2i:
	return Vector2i(global_pos.x & 127, global_pos.y & 127)

static func local_to_global_coords(chunk_pos:Vector2i,local_pos:Vector2i) -> Vector3i:
	# Shift left by 7 is equivalent to multiplying by 128 (128 cuz each tile is 2 so 2*64).
	var global_x := (chunk_pos.x << 7) | local_pos.x
	var global_y := (chunk_pos.y << 7) | local_pos.y
	
	return Vector3i(global_x, global_y, 0)

## private methods

func _make_map(is_generating:bool) -> void:
	var start_time := Time.get_ticks_usec()
	
	# Delete Old Nodes
	var children:Array = get_children()
	for child:Node3D in children:
		remove_child(child)
		child.queue_free()
	print("_MAKE_MAP- Deleted %s children"%children.size())
	
	if is_generating:
		## Delete Old Saved Data
		#Scripts.MAP_DATA.delete_data()
		#DirAccess.remove_absolute("user://gamedata/chunkdata/")
		#
		## Make New Noise Texture
		randomize()
		Scripts.MAP_DATA.seed = randi()
		Scripts.MAP_DATA.noise = _make_noise()
	
	#else:
		#Scripts.MAP_DATA.load_data()
	
	#if not DirAccess.dir_exists_absolute("user://gamedata/"):
		#DirAccess.make_dir_absolute("user://gamedata/")
	
	var noise_texture:NoiseTexture2D = NoiseTexture2D.new()
	noise_texture.width = world_chunk_width * chunk_size
	noise_texture.height = world_chunk_length * chunk_size
	noise_texture.generate_mipmaps = false
	noise_texture.invert = true
	noise_texture.noise = Scripts.MAP_DATA.noise
	noise_viewer.texture = noise_texture
	
	for chunk_x in world_chunk_length:
		for chunk_y in world_chunk_width:
			var chunk_coord := Vector2i(chunk_x, chunk_y)
			
			var new_chunk:= VoxelChunk.new()
			add_child(new_chunk)
			new_chunk.setup(chunk_coord, chunk_size, world_height, Scripts.MAP_DATA.noise)
			Scripts.MAP_DATA.chunks[chunk_coord] = new_chunk
			
			#_make_chunk(new_chunk, chunk_coord, is_generating)
	
	#Scripts.MAP_DATA.save_data()
	var time_taken := (Time.get_ticks_usec() - start_time) / 1000.0
	print("MAP_MANAGER- Map Made in: %s msec"%time_taken)

static func _make_noise(seed:int = Scripts.MAP_DATA.seed) -> FastNoiseLite:
	var noise:FastNoiseLite = FastNoiseLite.new()
	noise.noise_type = FastNoiseLite.TYPE_SIMPLEX_SMOOTH
	noise.fractal_type = FastNoiseLite.FRACTAL_RIDGED
	noise.fractal_octaves = 1
	noise.seed = seed
	noise.frequency = 0.0025
	
	return noise

func _make_chunk(chunk:VoxelChunk, chunk_coord:Vector2i, is_generating:bool) -> void: # Called for Every Chunk
	#if is_generating:
		#Scripts.CHUNK_MANAGER.delete_chunk(chunk_coord.x,chunk_coord.y)
	
	#var data_res:ChunkData = Scripts.CHUNK_MANAGER.load_chunk(chunk_coord.x, chunk_coord.y)
	
	for local_x:int in chunk_size:
		for local_y:int in chunk_size:
			var pixel_data:float = -Scripts.MAP_DATA.noise.get_noise_2d(local_x + chunk_coord.x * chunk_size, local_y + chunk_coord.y * chunk_size)
			var tile_height:int = snappedi(pixel_data*10,1) + 10
			
			
			if abs(tile_height) % 2 == 1:
				chunk.set_tile(local_x,tile_height,local_y,0)
			else:
				chunk.set_tile(local_x,tile_height,local_y,1)
			
			var water_height:int = tile_height + 1
			while water_height <= 4:
				chunk.set_tile(local_x,water_height,local_y,2)
				water_height += 1
			
			for height:int in tile_height + 1:
				var index:int = (local_x + local_y * chunk_size) * 20 + height
				
				if index >= 81920:
					print("_MAKE_CHUNK- Over Index. Index: %s, Chunk: %s"%[index,chunk_coord])
				#data_res.tile_data[index] = 1 # TODO: Fix this sometimes being to large for Array
	
	#if is_generating:
		#Scripts.CHUNK_MANAGER.save_chunk(data_res)
