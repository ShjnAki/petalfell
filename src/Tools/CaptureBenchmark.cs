using System;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Godot;

namespace Petalfell.Tools;

/// <summary>Explicit GPU review probe; no image readback inside the measured loop.</summary>
public static class CaptureBenchmark
{
	public static async Task Measure(Node owner, SubViewport viewport, string directory, string name,
		SubViewport reflection = null, SubViewport puddles = null)
	{
		Rid rid = viewport.GetViewportRid();
		RenderingServer.ViewportSetMeasureRenderTime(rid, true);
		Rid mirror = reflection?.GetViewportRid() ?? default;
		Rid puddle = puddles?.GetViewportRid() ?? default;
		if (puddles != null) RenderingServer.ViewportSetMeasureRenderTime(puddle, true);
		if (reflection != null) RenderingServer.ViewportSetMeasureRenderTime(mirror, true);
		// Give timestamp queries and GPU clocks time to settle after image saving.
		for (int i = 0; i < 120; i++)
			await owner.ToSignal(RenderingServer.Singleton, RenderingServer.SignalName.FramePostDraw);
		const int count = 300;
		var gpu = new double[count];
		var cpu = new double[count];
		var wall = new double[count];
		var draws = new int[count];
		var primitives = new int[count];
		ulong previous = Time.GetTicksUsec();
		for (int i = 0; i < count; i++)
		{
			await owner.ToSignal(RenderingServer.Singleton, RenderingServer.SignalName.FramePostDraw);
			ulong now = Time.GetTicksUsec();
			wall[i] = (now - previous) / 1000.0;
			previous = now;
			gpu[i] = RenderingServer.ViewportGetMeasuredRenderTimeGpu(rid);
			cpu[i] = RenderingServer.ViewportGetMeasuredRenderTimeCpu(rid);
			if (reflection?.RenderTargetUpdateMode == SubViewport.UpdateMode.Always)
			{
				gpu[i] += RenderingServer.ViewportGetMeasuredRenderTimeGpu(mirror);
				cpu[i] += RenderingServer.ViewportGetMeasuredRenderTimeCpu(mirror);
			}
			if (puddles?.RenderTargetUpdateMode == SubViewport.UpdateMode.Always)
			{
				gpu[i] += RenderingServer.ViewportGetMeasuredRenderTimeGpu(puddle);
				cpu[i] += RenderingServer.ViewportGetMeasuredRenderTimeCpu(puddle);
			}
			draws[i] = RenderingServer.ViewportGetRenderInfo(rid,
				RenderingServer.ViewportRenderInfoType.Visible, RenderingServer.ViewportRenderInfo.DrawCallsInFrame);
			primitives[i] = RenderingServer.ViewportGetRenderInfo(rid,
				RenderingServer.ViewportRenderInfoType.Visible, RenderingServer.ViewportRenderInfo.PrimitivesInFrame);
		}
		RenderingServer.ViewportSetMeasureRenderTime(rid, false);
		if (puddles != null) RenderingServer.ViewportSetMeasureRenderTime(puddle, false);
		if (reflection != null) RenderingServer.ViewportSetMeasureRenderTime(mirror, false);
		if (gpu.Any(v => v <= 0 || !double.IsFinite(v)) || draws.Any(v => v <= 0))
			throw new InvalidOperationException("Render benchmark received missing GPU timestamps or an empty viewport.");
		using var file = new StreamWriter(ProjectSettings.GlobalizePath($"{directory}/{name}.csv"));
		file.WriteLine("frame,wall_ms,viewport_gpu_ms,viewport_cpu_ms,visible_draws,visible_primitives");
		for (int i = 0; i < count; i++)
			file.WriteLine(FormattableString.Invariant($"{i},{wall[i]:F4},{gpu[i]:F4},{cpu[i]:F4},{draws[i]},{primitives[i]}"));
		static string Stats(double[] values)
		{
			var sorted = values.OrderBy(v => v).ToArray();
			return string.Create(CultureInfo.InvariantCulture,
				$"median={sorted[sorted.Length / 2]:F3}ms p95={sorted[(int)(sorted.Length * .95)]:F3}ms");
		}
		string summary = $"{name}: {viewport.Size.X}x{viewport.Size.Y}, MSAA {viewport.Msaa3D}, {count} frames\n" +
			$"GPU {Stats(gpu)}; render CPU {Stats(cpu)}; wall {Stats(wall)}\n" +
			$"Visible draws {draws.Min()}..{draws.Max()}, primitives {primitives.Min()}..{primitives.Max()}\n" +
			"Offscreen production scene; GPU/CPU include active water and wet-paving mirrors. Draws describe the main view. " +
			"Window 3D disabled. No readback during samples. " +
			"Stationary camera, warmed chunks; not a traversal or window-handoff benchmark.\n";
		File.WriteAllText(ProjectSettings.GlobalizePath($"{directory}/{name}.txt"), summary);
		GD.Print($"[capture-perf] {summary}");
	}
}
