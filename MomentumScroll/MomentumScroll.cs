using FrooxEngine;
using HarmonyLib;
using ResoniteModLoader;
using FrooxEngine.UIX;

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
	private static List<(ScrollRect scrollRect, float velocity)> MovingScrollRects = new(); // All the scroll rects that are currently moving and their momentum
	
	private static void Setup(){
		Harmony harmony = new("com.example.MomentumScroll");
		harmony.PatchAll();
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
		Setup();
		Msg("Reloaded MomentumScroll");
	}
#endif
	public override void OnEngineInit() {
#if DEBUG
		HotReloader.RegisterForHotReload(this);
#endif
		Setup();
	}

	[HarmonyPatch(typeof(FrooxEngine.UIX.ScrollRect), "ProcessEvent")]
	class ScrollRect_ProcessEvent_Patch {
		static void Postfix(ScrollRect __instance, Canvas.InteractionData eventData) {
			switch (eventData.touch) {
				case EventState.End:
					float movementDirection = (eventData.position.y - eventData.lastPosition.y);
					MovingScrollRects.Add((__instance, movementDirection));
					break;
				case EventState.Begin:
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
				scrollRect.World.RunSynchronously(()=>scrollRect.AbsolutePosition += velocity * 0.05f);
				velocity *= 0.999f;

				if (MathF.Abs(velocity) < 0.000005f) {
					MovingScrollRects.RemoveAt(i);
				} else {
					MovingScrollRects[i] = (scrollRect, velocity);
				}
			}
		}
	}
}
