using Godot;

namespace Petalfell.World;

/// <summary>CPU atlas controls retain their original PNG encoding in the PCK.</summary>
internal static class AtlasSourceImages
{
	public static bool Exists(string path) => !string.IsNullOrWhiteSpace(path) &&
		(Godot.FileAccess.FileExists(path) || ResourceLoader.Exists(path));

	// FileAccess understands both the editor filesystem and exported PCK paths.
	// GlobalizePath + System.IO does not. Do not sample imported/compressed GPU
	// textures for the indexed province palette or the 16-bit elevation source.
	public static Image LoadRawPng(string path)
	{
		using var file = Godot.FileAccess.Open(path, Godot.FileAccess.ModeFlags.Read);
		if (file == null) return null;
		var image = new Image();
		if (image.LoadPngFromBuffer(file.GetBuffer((long)file.GetLength())) == Error.Ok &&
			!image.IsEmpty()) return image;
		image.Dispose();
		return null;
	}

	// Reference pictures are display resources and may use Godot's import remap.
	public static Image LoadReference(string path) => Godot.FileAccess.FileExists(path)
		? LoadRawPng(path) : ResourceLoader.Load<Texture2D>(path)?.GetImage();
}
