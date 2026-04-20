using System;
using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Entities.Multiplayer;
using MegaCrit.Sts2.Core.Logging;
using MegaCrit.Sts2.Core.Multiplayer;

namespace Test.Scripts;

/// <summary>
/// 网络测试场景引导逻辑。负责在联机建立后附加测试节点，并在断线后移除。
/// </summary>
public static class NetworkTestBootstrap
{
	private const string NetworkTestScenePath = "res://scene/network_test.tscn";
	private const string NetworkTestNodeName = "NetworkTest";

	private static Node? _attachedNetworkTestNode;

	private static bool _loggedMissingScene;

	/// <summary>
	/// 联机建立时调用，尝试打开测试场景。
	/// </summary>
	private static void OnConnectionEstablished()
	{
		TryOpenNetworkTestScene();
	}

	/// <summary>
	/// 联机断开时调用，重置引导状态。
	/// </summary>
	private static void OnConnectionClosed()
	{
		if (_attachedNetworkTestNode is { } attachedNode && GodotObject.IsInstanceValid(attachedNode))
		{
			attachedNode.QueueFree();
		}

		_attachedNetworkTestNode = null;
	}

	/// <summary>
	/// 在多人联机建立后自动附加网络测试场景，不改变当前主场景。
	/// </summary>
	private static void TryOpenNetworkTestScene()
	{
		if (_attachedNetworkTestNode is { } attachedNode && GodotObject.IsInstanceValid(attachedNode))
		{
			return;
		}

		if (!ResourceLoader.Exists(NetworkTestScenePath))
		{
			if (!_loggedMissingScene)
			{
				Log.Error($"Network test scene not found: {NetworkTestScenePath}");
				_loggedMissingScene = true;
			}
			return;
		}

		SceneTree? sceneTree = Engine.GetMainLoop() as SceneTree;
		if (sceneTree == null)
		{
			return;
		}

		Node? parent = sceneTree.CurrentScene ?? sceneTree.Root;
		if (parent == null)
		{
			return;
		}

		Node? existingNode = parent.GetNodeOrNull<Node>(NetworkTestNodeName);
		if (existingNode != null)
		{
			_attachedNetworkTestNode = existingNode;
			return;
		}

		PackedScene? networkTestScene = GD.Load<PackedScene>(NetworkTestScenePath);
		if (networkTestScene == null)
		{
			if (!_loggedMissingScene)
			{
				Log.Error($"Failed to load network test scene: {NetworkTestScenePath}");
				_loggedMissingScene = true;
			}
			return;
		}

		Node networkTestNode = networkTestScene.Instantiate();
		networkTestNode.Name = NetworkTestNodeName;
		parent.CallDeferred(Node.MethodName.AddChild, networkTestNode);
		_attachedNetworkTestNode = networkTestNode;
	}

	[HarmonyPatch(typeof(NetHostGameService))]
	private static class NetHostGameServicePatches
	{
		[HarmonyPatch(nameof(NetHostGameService.StartENetHost))]
		[HarmonyPostfix]
		private static void StartENetHostPostfix(NetHostGameService __instance, NetErrorInfo? __result)
		{
			if (!__result.HasValue && __instance.IsConnected)
			{
				OnConnectionEstablished();
			}
		}

		[HarmonyPatch(nameof(NetHostGameService.Update))]
		[HarmonyPostfix]
		private static void UpdatePostfix(NetHostGameService __instance)
		{
			if (__instance.IsConnected)
			{
				OnConnectionEstablished();
			}
		}

		[HarmonyPatch(nameof(NetHostGameService.OnDisconnected))]
		[HarmonyPrefix]
		private static void OnDisconnectedPrefix()
		{
			OnConnectionClosed();
		}
	}

	[HarmonyPatch(typeof(NetClientGameService))]
	private static class NetClientGameServicePatches
	{
		[HarmonyPatch(nameof(NetClientGameService.OnConnectedToHost))]
		[HarmonyPostfix]
		private static void OnConnectedToHostPostfix(NetClientGameService __instance)
		{
			if (__instance.IsConnected)
			{
				OnConnectionEstablished();
			}
		}

		[HarmonyPatch(nameof(NetClientGameService.Update))]
		[HarmonyPostfix]
		private static void UpdatePostfix(NetClientGameService __instance)
		{
			if (__instance.IsConnected)
			{
				OnConnectionEstablished();
			}
		}

		[HarmonyPatch(nameof(NetClientGameService.OnDisconnectedFromHost))]
		[HarmonyPrefix]
		private static void OnDisconnectedFromHostPrefix()
		{
			OnConnectionClosed();
		}
	}
}