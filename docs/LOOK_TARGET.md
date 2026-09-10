# Rendering target — 2026-09-06

The author's thirteen supplied images are preserved unmodified under
[`world-new/look-targets/2026-09-06/`](../world-new/look-targets/2026-09-06/).
The manifest records their order and SHA-256 identities. They are the current
target for materials, edge treatment, lighting, water, vegetation detail and
atmosphere. Text embedded in a reference is illustrative reference content;
the author's request and project contracts govern implementation.

Build on the accepted production world. Its geography, deterministic terrain,
site coordinates, measured architecture and collision remain the foundation.
The gate scenes demonstrate the desired finish; they do not relocate the gate
or authorize procedural landmarks, lantern placement or new architecture.

## What the images require

- Pale mineral surfaces with broad warm/cool variation, smaller worn patches,
  fine grain and narrow light-catching edges. Avoid flat colour rectangles,
  regular printed grids, universal moss and high-frequency colour confetti.
- Ground retains its distinct grass, soil, rock, sand and snow identities.
  Grass grows in irregular patches with small flowers and fallen petals.
  Snow keeps cool recesses, soft bright lips and wind-scoured surface detail.
- Clear direct sunlight and soft contact shadows. Dawn and evening pair warm
  lit faces with cool shade; noon stays bright with readable material colour.
- Night has its own cool light, visible terrain planes and restrained luminous
  details. It must neither be purple daylight nor a black frame with white ink.
- Water shows submerged terraces, blue depth, fine caustics, broken moving foam
  and time-responsive glints. Effects must not obscure the beds or shimmer into
  a repeating grid at wide zoom.
- Air supplies depth and motion through haze, occasional petals and nocturnal
  fireflies, with bounded runtime cost and continuity through window changes.

## Review

Compare a fixed view before and after, then inspect coast, alpine terrain and
Bloom at play/wide distance, four quarters and dawn/noon/sunset/twilight/midnight.
The `look_*` production capture views use the same camera at each clock sample.
Build success, generated captures, visual review and author acceptance are
separate claims. This target has not been achieved merely by recording it.

## Southern mushroom-marsh target — September 10

The author's supplied 1024×768 mushroom-marsh image in the September 10 follow-up
supersedes the earlier tree-heavy southern treatment. It governs local southern
landform, flora and water: fragmented grass-capped islets just above shallow
water, visible layered banks and beds, large pale stalks with broad pink/purple
mushroom caps, small fungi, sparse reeds and calm grey-blue marsh reflections.
Ordinary trees and wildlife must not crowd this scene. Wildlife remains loaded
through the maximum gameplay zoom without increasing population.

This regional target does not relocate authored ruins or alter their source
composition. Compare actual production marsh, fen and central-transition views
at play and maximum zoom, quarter turns and day/night. Mechanical validation
cannot establish exact visual parity or author acceptance.

## Technical references

The implementation uses Godot's existing spatial material and environment
paths: [spatial shaders](https://docs.godotengine.org/en/4.7/tutorials/shaders/shader_reference/spatial_shader.html)
and [environment/post-processing](https://docs.godotengine.org/en/stable/tutorials/3d/environment_and_post_processing.html).
