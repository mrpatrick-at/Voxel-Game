@tool
extends Node3D
## enums
## consts
const world_width_in_chunks:int = 16
const world_heighth_in_chunks:int = 5
const world_length_in_chunks:int = 16

const chunk_size:int = 16
const mesh_library = preload("res://scenes/main/mesh_library.meshlib")
## exports
## public vars
static var seed:int
static var noise:FastNoiseLite
static var chunks:Dictionary = {}
## private vars
## onready vars
@onready var noise_viewer: TextureRect = $"../IngameUI/PanelContainer/VBoxContainer/NoiseViewer"
@onready var seed_label: Label = $"../IngameUI/PanelContainer/VBoxContainer/PanelContainer/HBoxContainer/SeedLabel"
# obj_ for node refrences
## built-in override methods

func _ready() -> void:
	randomize()
	if Engine.is_editor_hint():
		_make_map(true)

func _process(_delta: float) -> void:
	pass

func _on_button_new_pressed() -> void:
	_make_map(true)

func _on_button_load_pressed() -> void:
	_make_map(false)

## public methods

static func get_chunk_coords(global_pos: Vector3i) -> Vector3i:
	return Vector3i(global_pos.x * chunk_size, 0, global_pos.z * chunk_size)

static func get_local_coords(global_pos: Vector3i) -> Vector3i:
	return Vector3i(global_pos.x & chunk_size, 0, global_pos.z & chunk_size)

static func local_to_global_coords(chunk_pos:Vector2i,local_pos:Vector3i) -> Vector3i:
	var global_x := (chunk_pos.x * chunk_size) | local_pos.x
	var global_z := (chunk_pos.y * chunk_size) | local_pos.z
	
	return Vector3i(global_x, 0, global_z)

## private methods

func _make_map(is_generating:bool) -> void:
	var start_time:int = Time.get_ticks_usec()
	
	# Delete Old Nodes
	var children:Array = get_children()
	for child:Node3D in children:
		if child is VoxelChunk:
			remove_child(child)
			child.queue_free()
	print("_MAKE_MAP- Deleted %s children, %s children left"%[(children.size() - get_child_count()), get_child_count()])
	
	if is_generating:
		seed = randi()
		_make_noise()
		## Delete Old Saved Data
		#Scripts.MAP_DATA.delete_data()
		#DirAccess.remove_absolute("user://gamedata/chunkdata/")
		#
		## Make New Noise Texture
	
	#else:
		#Scripts.MAP_DATA.load_data()
	
	#if not DirAccess.dir_exists_absolute("user://gamedata/"):
		#DirAccess.make_dir_absolute("user://gamedata/")
	
	var noise_texture:NoiseTexture2D = NoiseTexture2D.new()
	noise_texture.width = world_width_in_chunks * chunk_size
	noise_texture.height = world_length_in_chunks * chunk_size
	noise_texture.generate_mipmaps = false
	noise_texture.invert = true
	noise_texture.noise = noise
	noise_viewer.texture = noise_texture
	
	var world_height:int = world_heighth_in_chunks * chunk_size
	
	var time_taken_2 := (Time.get_ticks_usec() - start_time) / 1000.0
	print("MAP_MANAGER- Pre Chunk Operations Done in: %s msec"%time_taken_2)
	
	for chunk_x in world_length_in_chunks:
		for chunk_z in world_width_in_chunks:
			var chunk_coord_2D:Vector2i = Vector2i(chunk_x,chunk_z)
			var chunk_heightmap:PackedByteArray = _get_height_map(chunk_coord_2D, world_height)
			for chunk_y in world_heighth_in_chunks:
				var chunk_coord := Vector3i(chunk_x, chunk_y, chunk_z)
				var chunk_res:= VoxelChunk.new()
				add_child(chunk_res)
				
				chunk_res.setup(chunk_coord)
				chunk_res.generate(noise)
				
				chunks[chunk_coord] = chunk_res
	
	var time_taken := (Time.get_ticks_usec() - start_time) / 1000.0
	print("MAP_MANAGER- Map Made in: %s msec"%time_taken)

static func _make_noise() -> void:
	noise = FastNoiseLite.new()
	noise.noise_type = FastNoiseLite.TYPE_SIMPLEX_SMOOTH
	noise.fractal_type = FastNoiseLite.FRACTAL_RIDGED
	noise.fractal_octaves = 1
	noise.seed = seed
	noise.frequency = 0.0025

static func _get_height_map(chunk_coord_2D:Vector2i, world_height:int) -> PackedByteArray:
	var height_map:PackedByteArray = []
	height_map.resize((chunk_size + 2) * (chunk_size + 2))
	for x:int in chunk_size + 2:
		for z:int in chunk_size + 2:
			var pixel_data:float = -noise.get_noise_2d(x + chunk_coord_2D.x * chunk_size, z + chunk_coord_2D.y * chunk_size)
			var tile_height:int = int((pixel_data + 1) * 0.5 * (world_height - 1) + 1)
			height_map[x + z * (chunk_size + 2)] = tile_height
	return height_map
