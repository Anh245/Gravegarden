@tool
extends McpTestSuite
func suite_name() -> String:
	return "editor_sync"
func test_reload_file_buffers() -> void:
	var editor=EditorInterface.get_script_editor()
	var report={"open":[],"unsaved":editor.get_unsaved_files()}
	for script in editor.get_open_scripts():
		report.open.append(script.resource_path)
	var file=FileAccess.open("res://docs/Feedback/editor-sync.json",FileAccess.WRITE)
	file.store_string(JSON.stringify(report,"\t"))
	editor.reload_open_files()
	assert_true(true)
