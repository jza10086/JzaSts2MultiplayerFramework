extends Node

func _ready() -> void:
	NetworkRouter.packet_received.connect(_on_action_received)

func _on_action_received(action: String, packet: Dictionary, sender_ids: Array) -> void:
	if action == "test_action":
		print("收到来自" + str(sender_ids) + "的测试消息: " + packet.payload.test_message)


func _on_button_pressed() -> void:
	NetworkRouter.send_to_host("test_action", {"test_message": "客户端收到消息!"})


func _on_button_2_pressed() -> void:
	NetworkRouter.broadcast_to_all_clients("test_action", {"test_message": "主机发送消息!"}, true)
