@tool
extends McpTestSuite
func suite_name() -> String:
	return "feedback_contract"
func test_unlocks_and_wave_roster() -> void:
	var g=track(load("res://scripts/game_director.gd").new())
	for id in g.BUILDING_IDS:
		g.catalog[id]=load("res://data/buildings/"+id+".tres")
	for index in range(1,6):
		g.waves.append(load("res://data/waves/wave_"+str(index)+".tres"))
	for id in g.BUILDING_IDS:
		var unlock=g.catalog[id].unlock_wave
		g.completed_wave=unlock-1
		assert_true(g.is_unlocked(id),"Unlock on the named wave: "+id)
		if unlock>1:
			g.completed_wave=unlock-2
			assert_false(g.is_unlocked(id),"Remain locked before the named wave: "+id)
	assert_eq(g.waves[0].spawn_list().count("rotling"),8)
	assert_eq(g.waves[0].spawn_list().count("bone_hound"),4)
	for id in ["rotling","bone_hound","bog_brute","wraith"]:
		assert_true(g.waves[1].spawn_list().has(id))
	g.completed_wave=5
	assert_eq(g.current_spawn_list().size(),104)
	g.completed_wave=9
	assert_eq(g.current_spawn_list().count("ossuary_giant"),6)
func test_all_buildings_have_both_animations() -> void:
	var g=track(load("res://scripts/game_director.gd").new())
	assert_eq(g.BUILDING_IDS.size(),11)
	for id in g.BUILDING_IDS:
		var data=load("res://data/buildings/"+id+".tres")
		for motion in ["idle","attack"]:
			assert_true(data.animations.has(motion),id+" "+motion)
			assert_true(ResourceLoader.exists(data.animations[motion].path))
			assert_eq(data.animations[motion].count,8)
func test_badge_is_authored_in_screen_space_with_padding() -> void:
	var building=track(load("res://scenes/building.tscn").instantiate())
	var layer=building.get_node("CollectionLayer")
	var badge=layer.get_node("CollectionBadge")
	assert_true(layer is CanvasLayer)
	assert_false(layer.follow_viewport_enabled)
	assert_eq(badge.size,Vector2(168,168))
	assert_eq(badge.scale,Vector2.ONE)
	assert_true(badge.get_node("Coin").position.y>=24)
	assert_true(badge.size.y-badge.get_node("Amount").get_rect().end.y>=32)
func test_six_authored_routes_and_single_hall_displays() -> void:
	var state=load("res://scenes/gravegarden.tscn").get_state()
	var routes=0
	var health=0
	var duplicate_visible=true
	for index in range(state.get_node_count()):
		if state.get_node_type(index)=="Path2D":
			routes+=1
		if str(state.get_node_name(index)).begins_with("WorldHealth"):
			health+=1
		if str(state.get_node_path(index)).ends_with("Interface/Screen/HUD/Heart"):
			for p in range(state.get_node_property_count(index)):
				if state.get_node_property_name(index,p)=="visible":
					duplicate_visible=state.get_node_property_value(index,p)
	assert_eq(routes,6)
	assert_eq(health,3)
	assert_false(duplicate_visible)
