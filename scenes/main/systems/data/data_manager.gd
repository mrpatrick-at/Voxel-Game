extends RefCounted
## enums
## consts
## exports
## public vars
static var seed:int = 0
static var noise_texture:NoiseTexture2D
static var chunks: Dictionary
## private vars
## onready vars
# obj_ for node refrences
## built-in override methods
## public methods
#static func save_data() -> void:
	#create_directories()
	#
	#var data:MapData = MapData.new()
	#data._init()
	#data.seed = seed
	#data.noise_texture = noise_texture
	#data.chunks = chunks
	#ResourceSaver.save(data, path)
	#print("DATA_MANAGER- Saved Data")
#
#static func load_data() -> void:
	#var data:MapData = load(path)
	#seed = data.seed
	#noise_texture = data.noise_texture
	#chunks = data.chunks
	#await noise_texture.changed
	#print("DATA_MANAGER- Loaded Data")

static func delete_data(path:String) -> void:
	DirAccess.remove_absolute(path)
	print("DATA_MANAGER- Deleted Data")

static func create_directories() -> void: # Creates Directories if they dont already exist
	if not DirAccess.dir_exists_absolute("user://gamedata"):
		DirAccess.make_dir_absolute("user://gamedata")
	
	if not DirAccess.dir_exists_absolute("user://gamedata/chunkdata/"):
		DirAccess.make_dir_absolute("user://gamedata/chunkdata/")

## private methods
