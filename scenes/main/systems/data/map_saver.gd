extends RefCounted
## enums
## consts
## exports
## public vars
const path:String = "user://gamedata/mapdata.tres"
static var seed:int = 0
static var noise:FastNoiseLite
static var chunks: Dictionary
## private vars
## onready vars
# obj_ for node refrences
## built-in override methods
## public methods
static func save_data() -> void:
	#Scripts.DATA_MANAGER.create_directories()
	
	var data:MapData = MapData.new()
	data.seed = seed
	data.noise = noise
	data.chunks = chunks
	ResourceSaver.save(data, path)
	print("MAP_DATA- Saved MapData")

static func load_data() -> void:
	var data:MapData = load(path) as MapData
	seed = data.seed
	noise = data.noise
	chunks = data.chunks
	print("MAP_DATA- Loaded MapData")

static func delete_data() -> void:
	DirAccess.remove_absolute(path)
	print("MAP_DATA- Deleted MapData")

## private methods
