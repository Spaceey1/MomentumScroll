using FrooxEngine;
using HarmonyLib;
using ResoniteModLoader;
using FrooxEngine.UIX;
using Elements.Core;

#if DEBUG
using ResoniteHotReloadLib;
#endif

namespace MomentumScroll;

public class MomentumScroll : ResoniteMod {
	internal const string VERSION_CONSTANT = "1.0.0"; //Changing the version here updates it in all locations needed
	public override string Name => "MomentumScroll";
	public override string Author => "Space";
	public override string Version => VERSION_CONSTANT;
	public override string Link => "https://github.com/Spaceey1/MomentumScroll/";
	private static List<(ScrollRect scrollRect, float2 velocity)> MovingScrollRects = new(); // All the scroll rects that are currently moving and their momentum
	[AutoRegisterConfigKey]
	public static ModConfigurationKey<float> speedMultiplier = new("speedMultiplier", "Speed multiplier", ()=>0.3f);
	[AutoRegisterConfigKey]
	public static ModConfigurationKey<float> stopThreshold = new("stopThreshold", "Stop threshold", ()=>0.0001f);
	[AutoRegisterConfigKey]
	public static ModConfigurationKey<float> drag = new("drag", "Drag", ()=>0.999f);
	private static ModConfiguration config;
	
	private static void Setup(ResoniteMod modInstance){
		Harmony harmony = new("com.example.MomentumScroll");
		harmony.PatchAll();
		config = modInstance.GetConfiguration();
	}

#if DEBUG
	static void BeforeHotReload()
	{
		Harmony harmony = new("com.example.MomentumScroll");
		harmony.UnpatchAll();
		Msg("Unloading MomentumScroll");
	}
	static void OnHotReload(ResoniteMod modInstance)
	{
		Setup(modInstance);
		Msg("Reloaded MomentumScroll");
	}

#endif
	public override void OnEngineInit() {
#if DEBUG
		HotReloader.RegisterForHotReload(this);
#endif
		Setup(this);
	}

	[HarmonyPatch(typeof(FrooxEngine.UIX.ScrollRect), "ProcessEvent")]
	class ScrollRect_ProcessEvent_Patch {
		static void Postfix(ScrollRect __instance, Canvas.InteractionData eventData) {
			switch (eventData.touch) {
				case EventState.End:
					float2 movementDirection = ((eventData.position - eventData.lastPosition) / __instance.Time.Delta);
					if(movementDirection.Magnitude < 0.5) return;
					MovingScrollRects.Add((__instance, movementDirection));
					break;
				case EventState.Stay:
					MovingScrollRects.RemoveAll(x => x.scrollRect == __instance);
					break;
				default:
					return;
			}
		}
	}

	[HarmonyPatch(typeof(Engine), "UpdateStep")]
	class Engine_UpdateStep_Patch {
		static void Prefix() {
			for (int i = MovingScrollRects.Count - 1; i >= 0; i--) {
				var (scrollRect, velocity) = MovingScrollRects[i];
				if (scrollRect.IsRemoved) {
					MovingScrollRects.RemoveAt(i);
					continue;
				}
				float2 endPos = scrollRect.AbsolutePosition + (velocity * scrollRect.Time.Delta * config.GetValue(speedMultiplier));
				scrollRect.World.RunSynchronously(()=>scrollRect.AbsolutePosition = endPos);
				if (velocity.Magnitude < config.GetValue(stopThreshold)) {
					MovingScrollRects.RemoveAt(i);
				} else {
					velocity *= config.GetValue(drag);
					MovingScrollRects[i] = (scrollRect, velocity);
				}
			}
		}
	}
}
