#if TOOLS
using Godot;
using System;
using System.Collections.Generic;
using System.Linq;

[Tool]
public partial class SpriteAnchors : EditorPlugin
{
	private static bool pressed;

	private static bool TryGetEditedSprite(out Node2D node, out int frame)
	{
		var edited = EditorInterface.Singleton.GetInspector().GetEditedObject();
		if (edited is Sprite2D sprite)
		{
			node = sprite;
			frame = sprite.Frame;
			return true;
		}
		else if (edited is AnimatedSprite2D animatedSprite)
		{
			node = animatedSprite;
			frame = animatedSprite.Frame;
			return true;
		}
		node = default;
		frame = default;
		return false;
	}
	private static IEnumerable<(StringName name, Vector2[] anchors)> GetSpriteAnchors(Node2D node)
	{
		return node.GetMetaList().Select(name => ((StringName name, Variant value))(name, node.GetMeta(name)))
			.Where(meta => meta.value.VariantType == Variant.Type.PackedVector2Array)
			.Select(meta => (meta.name, (Vector2[])meta.value));
	}
	private static IEnumerable<(StringName name, Vector2 anchor)> GetCurrentSpriteAnchors(Node2D node, int frame) =>
		GetSpriteAnchors(node).Select(meta => (meta.name, meta.anchors.ElementAtOrDefault(frame)));
	private static bool TryTrimPrefix(string s, string prefix, out string result)
	{
		result = s.TrimPrefix(prefix);
		return result != s;
	}
	private static StringName GetSelectedAnchor(Node2D node, IEnumerable<(StringName name, Vector2 anchor)> anchors) =>
		TryTrimPrefix(EditorInterface.Singleton.GetInspector().GetSelectedPath(), "metadata/", out var name) && node.GetMeta(name).VariantType == Variant.Type.PackedVector2Array ? name : (anchors.Any() ? anchors.MinBy(meta => meta.anchor.DistanceSquaredTo(node.GetLocalMousePosition())).name : default);
	private void OnFrameEdited(string property)
	{
		var inspector = EditorInterface.Singleton.GetInspector();
		var edited = inspector.GetEditedObject();
		if (_Handles(edited) && (property == "frame" || property == "frame_coords"))
			UpdateOverlays();
	}
	public override void _EnterTree()
	{
		var inspector = EditorInterface.Singleton.GetInspector();
		inspector.PropertyEdited += OnFrameEdited;
	}
	public override void _ExitTree()
	{
		var inspector = EditorInterface.Singleton.GetInspector();
		inspector.PropertyEdited -= OnFrameEdited;
	}
	public override void _ForwardCanvasDrawOverViewport(Control viewportControl)
	{
		if (TryGetEditedSprite(out var node, out var frame))
		{
			var anchors = GetCurrentSpriteAnchors(node, frame).ToArray();
			var selectedAnchor = GetSelectedAnchor(node, anchors);
			foreach (var (name, anchor) in anchors)
			{
				var position = node.GetViewportTransform() * node.ToGlobal(anchor);
				var selected = name == selectedAnchor;
				if (selected)
				{
					var font = EditorInterface.Singleton.GetEditorTheme().GetFont("bold", "EditorFonts");
					var text = name.ToString().Capitalize();
					var pos = position with { Y = position.Y - 4 } - font.GetStringSize(text) * 0.5f;
					viewportControl.DrawString(font, pos, text, fontSize: 20, modulate: Colors.LightGray);
					viewportControl.DrawStringOutline(font, pos, text, fontSize: 20, modulate: Colors.Black);
				}
				viewportControl.DrawCircle(position, 4, selected ? Colors.MediumAquamarine : Colors.White);
				viewportControl.DrawCircle(position, 4, Colors.Black, false);
			}
		}
	}
	public override bool _Handles(GodotObject @object)
	{
		return @object is Sprite2D || @object is AnimatedSprite2D;
	}
	public override void _MakeVisible(bool visible)
	{
		UpdateOverlays();
	}
	public override bool _ForwardCanvasGuiInput(InputEvent @event)
	{
		bool UpdateAnchors()
		{
			if (TryGetEditedSprite(out var node, out var frame))
			{
				var anchor = GetSelectedAnchor(node, GetCurrentSpriteAnchors(node, frame));
				if (anchor != null)
				{
					var anchors = (Vector2[])node.GetMeta(anchor);
					Array.Resize(ref anchors, Math.Max(frame + 1, anchors.Length));
					anchors[frame] = node.GetLocalMousePosition().Snapped(0.5f);
					node.SetMeta(anchor, anchors);
					UpdateOverlays();
					return true;
				}
			}
			return false;
		}
		if (@event is InputEventMouseButton mouseButton)
		{
			if (mouseButton.ButtonIndex == MouseButton.Left)
			{
				pressed = mouseButton.Pressed;
				if (pressed)
					return UpdateAnchors();
			}
		}
		else if (@event is InputEventMouseMotion)
		{
			if (pressed)
				return UpdateAnchors();
			UpdateOverlays();
		}
		return false;
	}
}
#endif
