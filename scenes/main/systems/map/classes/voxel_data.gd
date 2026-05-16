class_name Voxel
## enums
## consts
## exports
## public vars
static var neighbor:Dictionary = {
	"left" : "voxels[x - 1][y][z]",
	"right" : "voxels[x + 1][y][z]",
	"down" : "voxels[x][y - 1][z]",
	"up" : "voxels[x][y + 1][z]]",
	"forward" : "voxels[x][y][z - 1]",
	"back" : "voxels[x][y][z + 1]",
}
## private vars
## onready vars
# obj_ for node refrences
## built-in override methods
## public methods
## private methods
