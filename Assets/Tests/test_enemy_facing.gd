@tool
extends McpTestSuite
func suite_name() -> String:
	return "enemy_facing"
func fixture() -> Node:
	var path=track(Path2D.new())
	path.curve=Curve2D.new()
	var enemy=load("res://scripts/enemy.gd").new()
	path.add_child(enemy)
	var art=Sprite2D.new()
	enemy.add_child(art)
	enemy.art=art
	return enemy
func set_heading(enemy: Node,heading: Vector2) -> void:
	var curve=enemy.get_parent().curve
	curve.clear_points()
	curve.add_point(Vector2.ZERO)
	curve.add_point(heading.normalized()*4000)
	enemy.progress=2200
func test_initial_diagonal_facing() -> void:
	for heading in [Vector2(-1,1),Vector2(-1,-1),Vector2(1,1),Vector2(1,-1),Vector2(-1,1.05),Vector2(-1,-1.05),Vector2(1,1.05),Vector2(1,-1.05)]:
		var enemy=fixture()
		set_heading(enemy,heading)
		enemy._update_facing()
		assert_eq(enemy.walk_motion,"walk_right")
		assert_eq(enemy.art.flip_h,heading.x<0,"Diagonal spawns must face their direction of travel")
func test_deadband_does_not_preserve_wrong_sign() -> void:
	var enemy=fixture()
	set_heading(enemy,Vector2(1,0))
	enemy._update_facing()
	set_heading(enemy,Vector2(-1,0.98))
	enemy._update_facing()
	assert_eq(enemy.walk_motion,"walk_right","Keep the axis stable within the diagonal deadband")
	assert_true(enemy.art.flip_h,"Update horizontal direction even inside the deadband")
func test_vertical_and_horizontal_changes() -> void:
	var enemy=fixture()
	set_heading(enemy,Vector2(0,-1))
	enemy._update_facing()
	assert_eq(enemy.walk_motion,"walk_up")
	assert_false(enemy.art.flip_h)
	set_heading(enemy,Vector2(0,1))
	enemy._update_facing()
	assert_eq(enemy.walk_motion,"walk_down")
	set_heading(enemy,Vector2(-1,0))
	enemy._update_facing()
	assert_eq(enemy.walk_motion,"walk_right")
	assert_true(enemy.art.flip_h)
