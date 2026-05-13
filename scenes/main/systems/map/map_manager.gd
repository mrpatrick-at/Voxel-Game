extends Node3D
## enums
## consts
const world_height:int = 20
const world_chunk_width:int = 8
const world_chunk_length:int = 8
const chunk_size:int = 16
const mesh_library = preload("res://scenes/main/mesh_library.meshlib")
## exports
## public vars
static var seed:int
static var noise:FastNoiseLite
## private vars
## onready vars
@onready var noise_viewer: TextureRect = $"../IngameUI/PanelContainer/VBoxContainer/NoiseViewer"
@onready var seed_label: Label = $"../IngameUI/PanelContainer/VBoxContainer/PanelContainer/HBoxContainer/SeedLabel"
# obj_ for node refrences
## built-in override methods

func _ready() -> void:
	randomize()
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
	
	#if is_generating:
		## Delete Old Saved Data
		#Scripts.MAP_DATA.delete_data()
		#DirAccess.remove_absolute("user://gamedata/chunkdata/")
		#
		## Make New Noise Texture
	
	#else:
		#Scripts.MAP_DATA.load_data()
	
	#if not DirAccess.dir_exists_absolute("user://gamedata/"):
		#DirAccess.make_dir_absolute("user://gamedata/")
	
	seed = randi()
	_make_noise()
	
	var noise_texture:NoiseTexture2D = NoiseTexture2D.new()
	noise_texture.width = world_chunk_width * chunk_size
	noise_texture.height = world_chunk_length * chunk_size
	noise_texture.generate_mipmaps = false
	noise_texture.invert = true
	noise_texture.noise = noise
	noise_viewer.texture = noise_texture
	
	for chunk_x in world_chunk_length:
		for chunk_y in world_chunk_width:
			var chunk_coord := Vector2i(chunk_x, chunk_y)
			
			var new_chunk:= VoxelChunk.new()
			add_child(new_chunk)
			new_chunk.setup(chunk_coord, chunk_size, world_height, noise)
			Scripts.MAP_DATA.chunks[chunk_coord] = new_chunk
	
	#Scripts.MAP_DATA.save_data()
	var time_taken := (Time.get_ticks_usec() - start_time) / 1000.0
	print("MAP_MANAGER- Map Made in: %s msec"%time_taken)

static func _make_noise() -> void:
	noise = FastNoiseLite.new()
	noise.noise_type = FastNoiseLite.TYPE_SIMPLEX_SMOOTH
	noise.fractal_type = FastNoiseLite.FRACTAL_RIDGED
	noise.fractal_octaves = 1
	noise.seed = seed
	noise.frequency = 0.0025
