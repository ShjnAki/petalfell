extends SceneTree

func _initialize() -> void:
	call_deferred("_run")

func _run() -> void:
	root.add_child(load("res://tools/TerrainSurvey.cs").new())
