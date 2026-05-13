extends Resource
class_name MapData
## enums
## consts
## exports
@export var seed:int
@export var noise:FastNoiseLite
@export var chunks:Dictionary
## public vars
## private vars
## onready vars
# obj_ for node refrences
## built-in override methods
func _init() -> void:
	seed = 0
	noise = FastNoiseLite.new()
	chunks = {}
## public methods
## private methods
