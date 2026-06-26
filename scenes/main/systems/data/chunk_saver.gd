extends RefCounted
## enums
## consts
const chunk_path:String = "user://gamedata/chunkdata/chunk_%s_%s.tres"
## exports
## public vars
## private vars
## onready vars
# obj_ for node refrences
## built-in override methods
## public methods

static func save_chunk(chunk_res: ChunkData) -> void:
	var path:String = chunk_path % [chunk_res.chunk_coord.x, chunk_res.chunk_coord.y]
	
	# Ensure the directory exists
	#Scripts.DATA_MANAGER.create_directories()
	
	ResourceSaver.save(chunk_res, path)
	print("DATABASE- SAVED Chunk %s"%Vector2i(chunk_res.chunk_coord.x, chunk_res.chunk_coord.y))

static func load_chunk(chunk_x: int, chunk_y: int) -> ChunkData:
	var path:String= chunk_path % [chunk_x, chunk_y]
	print("DATABASE- LOADED Chunk %s"%Vector2i(chunk_x, chunk_y))
	
	if ResourceLoader.exists(path):
		return load(path) as ChunkData
	
	# If it doesn't exist, create a new empty one
	return ChunkData.new(Vector2i(chunk_x, chunk_y))

static func delete_chunk(chunk_x:int, chunk_y:int) -> void:
	var path:String= chunk_path % [chunk_x, chunk_y]
	DirAccess.remove_absolute(path)

## private methods
