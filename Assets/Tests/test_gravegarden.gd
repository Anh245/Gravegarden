@tool
extends McpTestSuite
func suite_name() -> String:
	return "gravegarden"
func test_idle_towers_do_not_bank_attacks() -> void:
	var tower=track(load("res://scripts/building.gd").new())
	tower.definition=load("res://data/buildings/bone_lily.tres")
	tower.active=true
	var enemies: Array[Node]=[]
	tower.tick(100.0,enemies,false,true)
	assert_eq(tower.cooldown,0.0,"A long idle period must not create burst fire")
	tower.cooldown=0.8
	tower.tick(0.1,enemies,false,true)
	assert_true(is_equal_approx(tower.cooldown,0.7),"Normal shot cadence is preserved")
func test_strongest_aura_and_upgrade_levels() -> void:
	var g=track(load("res://scripts/game_director.gd").new())
	var tower=track(load("res://scripts/building.gd").new())
	var first=track(load("res://scripts/building.gd").new())
	var second=track(load("res://scripts/building.gd").new())
	tower.definition=load("res://data/buildings/bone_lily.tres")
	tower.active=true
	for support in [first,second]:
		support.definition=load("res://data/buildings/spirit_scarecrow.tres")
		support.active=true
	g.buildings.assign([tower,first,second])
	for tier in range(1,4):
		first.level=tier
		assert_true(is_equal_approx(g.support_bonus(tower),0.25+0.10*(tier-1)),"Support improves numerically at each level")
	second.level=3
	assert_true(is_equal_approx(g.support_bonus(tower),0.45),"Overlapping auras never add together")
	first.position=Vector2(1000,1000)
	second.position=Vector2(1000,1000)
	assert_eq(g.support_bonus(tower),0.0,"Support cannot reach outside its radius")
func test_offline_caps_and_clock_reversal() -> void:
	var storage=track(load("res://scripts/run_state.gd").new())
	var catalog={"bone_orchard":load("res://data/buildings/bone_orchard.tres"),"soul_distillery":load("res://data/buildings/soul_distillery.tres")}
	var snapshot={"timestamp":100.0,"buildings":[{"id":"bone_orchard","level":1,"stored":0.0},{"id":"soul_distillery","level":1,"stored":0.0}]}
	var hour=storage.offline_production(snapshot,3700.0,catalog)
	assert_eq(hour.elapsed,3600.0)
	assert_true(is_equal_approx(hour.total,7440.0),"Each producer respects its individual storage capacity")
	var repeated=storage.offline_production(snapshot,3700.0,catalog)
	assert_eq(repeated.total,0.0,"The same offline period cannot pay twice")
	var backwards=storage.offline_production(snapshot,1.0,catalog)
	assert_eq(backwards.elapsed,0.0)
	assert_eq(backwards.total,0.0)
	var capped=storage.offline_production(snapshot,100000.0,catalog)
	assert_eq(capped.elapsed,28800.0,"Offline time is capped at eight hours")
func test_authored_scene_and_font() -> void:
	var scene=load("res://scenes/gravegarden.tscn")
	assert_true(scene is PackedScene)
	assert_gt(scene.get_state().get_node_count(),400,"Permanent scene structure is authored")
	var theme=load("res://ui/gravegarden_theme.tres")
	assert_eq(theme.default_font.resource_path,"res://assets/fonts/LilitaOne-Regular.ttf")
