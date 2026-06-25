extends Node
## enums
## consts
const MAIN:= preload("res://scenes/main/main.tscn")

const MAP_MANAGER:= preload("res://scenes/main/systems/map/map_manager.gd")

const DATA_MANAGER:= preload("res://scenes/main/systems/data/data_manager.gd")
const MAP_DATA:= preload("res://scenes/main/systems/data/map_saver.gd")
const CHUNK_MANAGER:= preload("res://scenes/main/systems/data/chunk_saver.gd")

const PLAYER:= preload("res://scenes/main/systems/player/main.gd")
## exports
## public vars
## private vars
## onready vars
# obj_ for node refrences
## built-in override methods

func _ready() -> void:
	pass 

func _process(_delta: float) -> void:
	pass

## public methods
## private methods
