extends Node

const NETWORK_ROUTER_PATH: String = "/root/NetworkRouter"

var _network_router: Node


func _ready() -> void:
	_bind_network_router()


func _bind_network_router() -> void:
	if _network_router != null:
		return

	var router := get_node_or_null(NETWORK_ROUTER_PATH)
	if router == null:
		push_warning("networktest 未找到 NetworkRouter: %s" % NETWORK_ROUTER_PATH)
		return

	_network_router = router
	var handler := Callable(self, "_on_action_received")
	if not _network_router.is_connected("packet_received", handler):
		_network_router.connect("packet_received", handler)

func _on_action_received(action: String, packet: Dictionary, sender_ids: Array) -> void:
	if action == "test_action":
		print_rich("[color=green]","收到来自" + str(sender_ids) + "的测试消息: " + packet.payload.test_message,"[/color]")


func _on_button_pressed() -> void:
	_network_router.call("send_to_host", "test_action", {"test_message": "客机发送消息!"})


func _on_button_2_pressed() -> void:
	_network_router.call("broadcast_to_all_clients", "test_action", {"test_message": "主机发送消息!"}, true)
