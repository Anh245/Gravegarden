@tool
extends EditorExportPlugin
func _get_name() -> String:
	return "GravegardenDistribution"
func _export_file(path: String,_type: String,_features: PackedStringArray) -> void:
	if path.begins_with("res://docs/") or path.begins_with("res://tests/") or path.begins_with("res://tools/") or path.begins_with("res://addons/") or path.begins_with("res://assets/audio/source/") or path.begins_with("res://assets/fonts/Alegreya") or path=="res://assets/fonts/OFL.txt":
		skip()
