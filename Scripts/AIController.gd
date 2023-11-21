extends AIController3D

var move_x : float = 0
var move_y : float = 0
var place_bomb : int = 0

var game_manager

var obs_around_player_name = "obs_around_player"
var obs_around_player_bomb_name = "obs_around_player_bomb"
var player_info_name = "player_info"
var enemy_info_name = "enemy_info"
var enemy_obs_name = "enemy_obs"
var enemy_obs_bomb_name = "enemy_obs_bomb"

func init(player: Node3D):
	_player = player
	game_manager = player.gameManager

func _physics_process(_delta):
	if _player.IsHuman and _player.playerIndex == 0:
		get_obs()

func get_obs() -> Dictionary:
	var id = _player.playerIndex
	
	var players = []
	var players_obs = []
	var players_obs_bomb = []
	for i in range(game_manager.maxPlayerCount):
		if i == id:
			continue
			
		var player_i = game_manager.players[i]
		players.append_array(_player.GetEnemyObs(player_i))

		var obs = game_manager.GetObservationsAroundPlayer(id, i)
		players_obs.append_array(obs[obs_around_player_name])
		players_obs_bomb.append_array(obs[obs_around_player_bomb_name])		

	_player.CheckDistanceFromEnemies()

	var player = _player.GetObs()
	var obs = game_manager.GetObservationsAroundPlayer(id, id)
	var obs_around_player = obs[obs_around_player_name]
	var obs_around_player_bomb = obs[obs_around_player_bomb_name]

	return {
		obs_around_player_name: obs_around_player,
		obs_around_player_bomb_name: obs_around_player_bomb, 
		player_info_name: player,
		enemy_info_name: players,
		enemy_obs_name: players_obs,
		enemy_obs_bomb_name: players_obs_bomb
	}

func get_obs_space():
	var obs = get_obs()
	return {
		obs_around_player_name: {
			"size": [len(obs[obs_around_player_name])],
			"space": "box",
			"dtype": "int",
			"low": "0",
			"high": "4"
		},
		obs_around_player_bomb_name:{
			"size": [len(obs[obs_around_player_bomb_name])],
			"space": "box",
		},
		player_info_name: {
			"size": [len(obs[player_info_name])],
			"space": "box"
		},
		enemy_info_name: {
			"size": [len(obs[enemy_info_name])],
			"space": "box"
		},
		enemy_obs_name: {
			"size": [len(obs[enemy_obs_name])],
			"space": "box",
			"dtype": "int",
			"low": "0",
			"high": "4"
		},
		enemy_obs_bomb_name: {
			"size": [len(obs[enemy_obs_bomb_name])],
			"space": "box",
		},
	}
	
func get_reward() -> float:
	if _player.Lives == 0:
		return 0
	
	return reward
	
func get_action_space() -> Dictionary:
	return {
		"move" : {
			"size": 2,
			"action_type": "continuous"
		},
		"place_bomb" : {
			"size": 2,
			"action_type": "discrete"
		}
	}
	
func set_action(action) -> void:
	move_x = clamp(action["move"][0], -1.0, 1.0)
	move_y = clamp(action["move"][1], -1.0, 1.0)
	#place_bomb = clamp(action["place_bomb"][0],0, 1) > 0
	
	#move_x = action["move_x"] - 1
	#move_y = action["move_y"] - 1
	place_bomb = action["place_bomb"]
