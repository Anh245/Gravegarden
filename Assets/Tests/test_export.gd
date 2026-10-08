@tool
extends McpTestSuite
func suite_name() -> String:
	return "export"
func test_start_android_export() -> void:
	var destination="C:/Users/Usuario/Downloads/gravegarden.apk"
	var log_path=ProjectSettings.globalize_path("res://docs/Feedback/V2/android-export.log")
	var args=PackedStringArray(["--headless","--path",ProjectSettings.globalize_path("res://"),"--export-debug","Android",destination,"--log-file",log_path])
	var pid=OS.create_process(OS.get_executable_path(),args,false)
	var file=FileAccess.open("res://docs/Feedback/V2/android-export-job.json",FileAccess.WRITE)
	file.store_string(JSON.stringify({"pid":pid,"path":destination,"log":log_path,"started_utc":Time.get_datetime_string_from_system(true)},"\t"))
	assert_gt(pid,0,"Native Godot Android export process starts")
