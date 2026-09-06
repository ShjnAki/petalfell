# Authored Fallen Colossus

The September 6 worldbuilding request replaces the earlier supplied meshes with
from-scratch sculpture consistent with Petalfell's stepped stone language.

`tools/build_fallen_colossus.py` is the editable source: explicit foot, ankle,
calf, knee, broken thigh, face, recess and crown profiles. No earlier GLB is
imported. It carves those solids on a bounded 0.8-unit leg / 0.7-unit head lattice,
merges connected surfaces, dissolves invisible coplanar edges and bevels simple
convex corners. The whole operation is offline and site-specific.

Rebuild from the project root:

```bash
blender --background --factory-startup --python-exit-code 1 --python tools/build_fallen_colossus.py
dotnet build
godot-mono --headless --path . --editor --import
./tools/world-authoring.sh verify-sculpture
```

Outputs are two `assets/sites/fallen-colossus-authored-*.glb` files, their JSON
audit and this directory's editable `fallen-colossus.blend`. The blend displays
the two assets side by side; exported objects have independent bottom-centred
pivots. Godot applies only the site anchors and the head's -45-degree yaw.

UV1 is metre-coordinate stone projection. UV2 packs an octahedral shared hull
normal in Godot's basis; a negative X sentinel pins a deeply re-entrant corner.
Lighting normals remain flat. Do not regenerate UV2 as lightmap coordinates.
There are no image materials or baked light. The runtime's sculpture material
supplies the palette, weathering, course seams and moss.

The original GLBs and normalization tool are retained as historical inputs to
earlier evidence, but no original geometry is loaded by this authoring script or
the current production attachment. Whole-site reference parity and author
acceptance remain open.
