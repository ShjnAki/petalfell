"""Author Reference 12's replacement sculptures from explicit carved profiles.

Run: blender --background --factory-startup --python tools/build_fallen_colossus.py
No imported mesh, raster displacement, random damage or generated ground is used.
Coordinates are authored in metres (one Petalfell block); Blender Z becomes Godot Y.
Only the two derived GLBs, their audit, and the editable blend are written.
"""

import json
import math
from pathlib import Path

import bmesh
import bpy
from mathutils import Matrix, Vector
from mathutils.bvhtree import BVHTree

ROOT = Path(__file__).resolve().parents[1]
OUTPUT = ROOT / "assets/sites"
AUTHORING = ROOT / "art/fallen-colossus"
AUDIT = {"source": "tools/build_fallen_colossus.py", "revision": 3,
         "units": "metres; bottom-centred; Blender Z up; Godot Y up", "assets": {}}


def activate(obj):
    bpy.ops.object.select_all(action="DESELECT")
    obj.select_set(True)
    bpy.context.view_layer.objects.active = obj


def solid(name, vertices, faces):
    mesh = bpy.data.meshes.new(name)
    mesh.from_pydata(vertices, [], faces)
    mesh.update()
    obj = bpy.data.objects.new(name, mesh)
    bpy.context.collection.objects.link(obj)
    bm = bmesh.new()
    bm.from_mesh(mesh)
    bmesh.ops.recalc_face_normals(bm, faces=bm.faces)
    bm.to_mesh(mesh)
    bm.free()
    return obj


def box(name, centre, size, bevel=0):
    x, y, z = centre
    a, b, c = (v * .5 for v in size)
    obj = solid(name, [(x+dx*a, y+dy*b, z+dz*c)
                     for dz in (-1, 1) for dy in (-1, 1) for dx in (-1, 1)],
                [(0, 1, 3, 2), (4, 6, 7, 5), (0, 4, 5, 1),
                 (2, 3, 7, 6), (0, 2, 6, 4), (1, 5, 7, 3)])
    if bevel:
        chamfer(obj, bevel)
    return obj


def loft(name, rings, centre_x=0):
    """Eight-sided rectangular sections; every change in section is authored."""
    vertices = []
    for z, width, front, back, corner in rings:
        vertices.extend([(centre_x+x, y, z) for x, y in
                         [(-width+corner, front), (width-corner, front),
                          (width, front+corner), (width, back-corner),
                          (width-corner, back), (-width+corner, back),
                          (-width, back-corner), (-width, front+corner)]])
    faces = [tuple(range(7, -1, -1))]
    for ring in range(len(rings)-1):
        a, b = ring*8, (ring+1)*8
        faces.extend([(a+i, a+(i+1)%8, b+(i+1)%8, b+i) for i in range(8)])
    faces.append(tuple(range(len(vertices)-8, len(vertices))))
    return solid(name, vertices, faces)


def chamfer(obj, width):
    activate(obj)
    mod = obj.modifiers.new("Single stone arris", "BEVEL")
    mod.width, mod.segments = width, 1
    mod.affect = "EDGES"
    mod.limit_method = "ANGLE"
    mod.angle_limit = math.radians(24)
    bpy.ops.object.modifier_apply(modifier=mod.name)


def carving_arris(obj):
    """Bevel simple exposed corners only; retain the complex step junctions."""
    bm = bmesh.new()
    bm.from_mesh(obj.data)
    weights = bm.edges.layers.float.new("bevel_weight_edge")
    for edge in bm.edges:
        if edge.is_convex and all(len(v.link_edges) == 3 for v in edge.verts):
            edge[weights] = 1.0
    bm.to_mesh(obj.data)
    bm.free()
    activate(obj)
    mod = obj.modifiers.new("Exposed stone arris", "BEVEL")
    mod.width, mod.segments = .045, 1
    mod.limit_method = "WEIGHT"
    bpy.ops.object.modifier_apply(modifier=mod.name)


def cut(obj, cutter):
    activate(obj)
    mod = obj.modifiers.new("Authored carving", "BOOLEAN")
    mod.operation, mod.solver, mod.object = "DIFFERENCE", "EXACT", cutter
    bpy.ops.object.modifier_apply(modifier=mod.name)
    bpy.data.objects.remove(cutter, do_unlink=True)


def rotate_geometry(obj, axis, degrees):
    obj.data.transform(Matrix.Rotation(math.radians(degrees), 4, axis))


def legs():
    parts = []
    # Each foot blends into its ankle through two long instep planes. The calf
    # is the widest shaft band, and the knee changes the plane only slightly.
    for name, cx, front, top, drift in [
        ("West", -5.5, -9.8, 30.8, -.25),
        ("East", 5.5, -8.4, 29.6, .25),
    ]:
        foot = loft(name+"Foot", [
            (0, 4.35, front, 5.8, .6), (.7, 4.5, front-.1, 5.8, .5),
            (2.25, 4.35, front+.2, 5.6, .6),
            (3.8, 3.9, front+2.3, 5.3, .6),
            (5.8, 3.25, -3.05, 4.4, .55),
            (7.4, 3.15, -2.8, 4.25, .55),
        ], cx)
        # Three unevenly spaced shallow toe cuts; no detached cube toes.
        for i, dx in enumerate([-2.25, -.35, 1.45]):
            cut(foot, box(name+"ToeIncision", (cx+dx, front+.55, 1.8),
                          (.10, 2.4-(i*.22), 2.4)))
        chamfer(foot, .095)
        parts.append(foot)
        shaft = loft(name+"Leg", [
            (5.5, 3.15, -2.8, 4.15, .5),
            (8.1, 3.10, -2.7, 4.2, .5),
            (11.7, 3.55, -2.9, 4.95, .7),
            (16.2, 3.8, -3.05, 5.4, .8),
            (19.0, 3.5, -3.4, 4.75, .65),
            (21.0, 3.65, -3.85, 4.25, .6),
            (23.2, 3.6, -3.35, 4.55, .6),
            (27.0, 3.95, -3.25, 5.0, .65),
            (top, 4.0, -3.25, 5.15, .6),
        ], cx+drift)
        # Incised masonry joints sit inside the carved surface, with staggered
        # short returns, rather than dividing the leg into enlarged boxes.
        for i, z in enumerate([8.8, 13.2, 17.4, 22.5, 26.6]):
            cut(shaft, box("Front bed joint", (cx, -3.35, z), (9, .48, .075)))
            cut(shaft, box("Side return", (cx+3.8, 2.7, z+.65), (.32, 5.6, .07)))
            cut(shaft, box("Short vertical joint", (cx+(-1.4 if i%2 else 1.35), -3.3, z-1.1),
                           (.065, .42, 2.1)))
        # The break is an asymmetric negative cut across the thigh, with a
        # second small missing corner; it never becomes a torso connecting legs.
        cut(shaft, box("Broken upper corner", (cx+2.5, -2.2, top-.28), (3.7, 4.3, 1.1)))
        cut(shaft, box("Broken back corner", (cx-2.8, 4.5, top-.05), (2.4, 2.6, .65)))
        chamfer(shaft, .105)
        parts.append(shaft)
    return parts


def head():
    parts = []
    core = loft("CarvedHead", [
        (0, 4.9, -3.4, 2.5, 1.3), (1.8, 6.3, -4.6, 3.8, 1.15),
        (4.1, 7.7, -5.1, 4.8, 1.35), (7.0, 8.8, -5.2, 5.4, 1.65),
        (10.5, 9.1, -4.8, 5.8, 1.65), (13.8, 9.0, -4.9, 5.8, 1.5),
        (17.2, 8.65, -4.6, 5.5, 1.35), (19.0, 7.9, -4.05, 4.8, 1.4),
    ])
    for sign in (-1, 1):
        # Deep socket behind a low lid; the dark feature is a real cavity.
        cut(core, box("Eye socket", (sign*4.05, -5.2, 11.25), (4.8, 3.6, 2.0)))
    cut(core, box("Mouth incision", (0, -5.4, 3.85), (6.35, 1.3, .45)))
    cut(core, box("Lost temple corner", (-7.9, -3.55, 14.45), (2.7, 4.3, 2.1)))
    for z, x, w in [(2.3, 1.5, 8), (6.3, -4.1, 5.8), (9.0, 5.8, 4.2),
                    (14.9, -.8, 11.5), (17.0, 4.8, 4.8)]:
        cut(core, box("Face stone course", (x, -5.0, z), (w, .43, .065)))
    chamfer(core, .13)
    parts.append(core)
    # Nose bridge, tip and nostril planes are one tapered carving. Cheek and
    # brow volumes overlap the core; their seams follow facial planes.
    nose = loft("NoseBridgeAndTip", [
        (6.5, 1.8, -7.9, -4.65, .4), (7.1, 2.0, -8.45, -4.65, .42),
        (8.0, 1.65, -8.35, -4.5, .34), (12.45, .95, -5.75, -4.4, .23),
        (13.05, .7, -5.1, -4.4, .16),
    ])
    for sign in (-1, 1):
        cut(nose, box("Nostril", (sign*1.2, -7.65, 6.52), (.75, 1.1, .42)))
    chamfer(nose, .10)
    parts.append(nose)
    for sign in (-1, 1):
        cheek = loft("CheekPlane", [
            (4.8, 1.4, -5.45, -4.75, .25), (7.6, 2.1, -6.0, -4.7, .3),
            (9.65, 2.0, -5.95, -4.6, .3), (10.0, 1.65, -5.15, -4.55, .2),
        ], sign*4.8)
        chamfer(cheek, .095)
        parts.append(cheek)
        brow = loft("Brow", [
            (12.2, 2.55, -6.05, -4.5, .32),
            (13.1, 2.75, -5.95, -4.4, .4),
            (13.7, 2.3, -5.1, -4.35, .3),
        ], sign*4.1)
        chamfer(brow, .1)
        parts.append(brow)
        lid = box("RecessedEyeLid", (sign*4.05, -3.75, 10.95), (3.7, .5, .38), .075)
        parts.append(lid)
        ear = loft("Ear", [(7, .68, -1.1, 1.2, .25),
                            (8.0, .9, -1.5, 1.4, .25),
                            (11.7, .9, -1.4, 1.4, .25),
                            (12.4, .6, -.9, .9, .2)], sign*9.0)
        cut(ear, box("Ear hollow", (sign*9.45, -.25, 10.1), (1, 1.2, 2.1)))
        chamfer(ear, .1)
        parts.append(ear)
    parts.append(loft("UpperLip", [(4.15, 3.05, -5.75, -4.8, .3),
                                  (4.65, 2.55, -6.2, -4.8, .3),
                                  (5.2, 1.95, -5.4, -4.8, .25)]))
    parts.append(loft("LowerLip", [(2.85, 2.8, -5.0, -4.4, .35),
                                  (3.4, 3.05, -5.8, -4.5, .35),
                                  (3.75, 2.95, -5.7, -4.6, .3)]))
    # An open octagonal crown follows the skull. The asymmetric broken teeth
    # and the lost west corner make a damaged crown rather than a crenellated box.
    rim = loft("CrownRim", [(16.3, 9.25, -5.7, 6.2, 1.6),
                            (17.25, 9.5, -5.8, 6.4, 1.6),
                            (18.4, 9.3, -5.55, 6.2, 1.55)])
    cut(rim, box("Crown interior", (0, .25, 18), (15.7, 8.2, 6)))
    cut(rim, box("Lost crown corner", (-8.3, -5.1, 18.35), (3.9, 2.7, 1.2)))
    chamfer(rim, .11)
    parts.append(rim)
    for i, (x, y, width, depth, h) in enumerate([
        (-6.1, -4.9, 1.6, 1.55, 2.0), (-2.65, -5.0, 1.9, 1.6, 3.0),
        (1.0, -5.0, 1.85, 1.6, 2.8), (4.65, -4.95, 1.9, 1.6, 2.2),
        (8.2, -2.15, 1.5, 1.6, 2.6), (8.2, 2.8, 1.5, 1.6, 1.4),
        (5.9, 5.25, 1.8, 1.55, 2.5), (1.8, 5.3, 1.8, 1.55, 2.9),
        (-2.4, 5.3, 1.8, 1.55, 1.5), (-6.2, 5.0, 1.8, 1.55, 2.5),
        (-8.2, 2.3, 1.5, 1.6, 2.7), (-8.2, -1.8, 1.5, 1.6, .9),
    ]):
        tooth = box("CrownTooth%02d" % i, (x, y, 18+h/2), (width, depth, h), .11)
        parts.append(tooth)
    return parts


def stepped_carving(label, parts, unit):
    """Cut the authored solid into a small stone lattice, then mesh its skin.

    This is a bounded, offline modelling operation on the profiles above. It
    cannot allocate world terrain or import/reinterpret another artist's model.
    Adjacent parts become one continuous carved surface, including eye recesses.
    """
    occupied = set()
    for part in parts:
        bm = bmesh.new()
        bm.from_mesh(part.data)
        tree = BVHTree.FromBMesh(bm)
        lo = [math.floor(min(v.co[i] for v in part.data.vertices)/unit) for i in range(3)]
        hi = [math.ceil(max(v.co[i] for v in part.data.vertices)/unit) for i in range(3)]
        for z in range(lo[2], hi[2]):
            for y in range(lo[1], hi[1]):
                for x in range(lo[0], hi[0]):
                    cell = (x, y, z)
                    if cell in occupied:
                        continue
                    p = Vector(((x+.5)*unit, (y+.5)*unit, (z+.5)*unit))
                    nearest, normal, _, _ = tree.find_nearest(p)
                    if nearest is not None and (p-nearest).dot(normal) < 0:
                        occupied.add(cell)
        bm.free()
        bpy.data.objects.remove(part, do_unlink=True)
    # A cut narrower than the lattice can leave diagonal-only contact. Fill
    # the first empty corner of that 2x2 cross-section before making a skin;
    # otherwise two surfaces share an edge with four incident faces.
    for _ in range(8):
        additions = set()
        for a, b in [(0, 1), (0, 2), (1, 2)]:
            origins = set()
            for cell in occupied:
                for da, db in [(0, 0), (-1, 0), (0, -1), (-1, -1)]:
                    p = list(cell)
                    p[a] += da
                    p[b] += db
                    origins.add(tuple(p))
            for origin in sorted(origins):
                corners = []
                for da, db in [(0, 0), (1, 0), (1, 1), (0, 1)]:
                    p = list(origin)
                    p[a] += da
                    p[b] += db
                    corners.append(tuple(p))
                present = [p in occupied for p in corners]
                if present in ([True, False, True, False], [False, True, False, True]):
                    additions.add(min(p for p in corners if p not in occupied))
        if not additions:
            break
        occupied.update(additions)
    vertices, faces, index = [], [], {}

    def vertex(corner):
        if corner not in index:
            index[corner] = len(vertices)
            vertices.append(tuple(p*unit for p in corner))
        return index[corner]

    for x, y, z in sorted(occupied):
        for direction, corners in [
            ((1, 0, 0), [(1,0,0),(1,1,0),(1,1,1),(1,0,1)]),
            ((-1,0,0), [(0,1,0),(0,0,0),(0,0,1),(0,1,1)]),
            ((0,1,0), [(1,1,0),(0,1,0),(0,1,1),(1,1,1)]),
            ((0,-1,0), [(0,0,0),(1,0,0),(1,0,1),(0,0,1)]),
            ((0,0,1), [(0,0,1),(1,0,1),(1,1,1),(0,1,1)]),
            ((0,0,-1), [(0,1,0),(1,1,0),(1,0,0),(0,0,0)]),
        ]:
            dx, dy, dz = direction
            if (x+dx, y+dy, z+dz) not in occupied:
                faces.append(tuple(vertex((x+a, y+b, z+c)) for a,b,c in corners))
    obj = solid("Stepped"+label.title(), vertices, faces)
    # Dissolving coplanar cell boundaries preserves the steps without sending
    # thousands of invisible voxel-grid edges to Godot.
    bm = bmesh.new()
    bm.from_mesh(obj.data)
    if any(not e.is_manifold for e in bm.edges):
        raise RuntimeError(f"{label}: carving lattice has a non-manifold contact")
    bmesh.ops.dissolve_limit(bm, angle_limit=.001, verts=bm.verts, edges=bm.edges,
                            use_dissolve_boundaries=False)
    bm.to_mesh(obj.data)
    bm.free()
    carving_arris(obj)
    # Metre-coordinate stone courses are attached before the fallen rotation,
    # so joints turn with the head rather than remaining horizontal in the world.
    uv = obj.data.uv_layers.new(name="StoneCourses")
    for face in obj.data.polygons:
        axis = max(range(3), key=lambda i: abs(face.normal[i]))
        for loop in face.loop_indices:
            p = obj.data.vertices[obj.data.loops[loop].vertex_index].co
            uv.data[loop].uv = ((p.y, p.z) if axis == 0 else
                                (p.x, p.z) if axis == 1 else (p.x, p.y))
    if label == "head":
        rotate_geometry(obj, "X", -67)
        rotate_geometry(obj, "Y", -7)
    bm = bmesh.new()
    bm.from_mesh(obj.data)
    bm.normal_update()
    bm.verts.ensure_lookup_table()
    outline_uv = obj.data.uv_layers.new(name="OutlineNormalOct")
    for loop in obj.data.loops:
        vertex = bm.verts[loop.vertex_index]
        normal = vertex.normal
        # A deeply re-entrant corner has no useful averaged outward direction.
        # Pin that shared corner instead of extruding through an adjacent face.
        if any(normal.dot(face.normal) < -.01 for face in vertex.link_faces):
            outline_uv.data[loop.index].uv = (-1, 2)
            continue
        # glTF converts Blender Z up to Godot Y up; UVs themselves are not
        # vectors, so encode that normal explicitly in the destination basis.
        n = Vector((normal.x, normal.z, -normal.y)).normalized()
        n /= abs(n.x)+abs(n.y)+abs(n.z)
        x, y = n.x, n.y
        if n.z < 0:
            x, y = (1-abs(y))*(1 if x >= 0 else -1), (1-abs(x))*(1 if y >= 0 else -1)
        # Blender's glTF exporter flips UV V. Pre-flip this packed data channel.
        outline_uv.data[loop.index].uv = (x*.5+.5, 1-(y*.5+.5))
    bm.free()
    return [obj]


def finish(label, parts):
    parts = stepped_carving(label, parts, .70 if label == "head" else .80)
    obj = parts[0]
    activate(obj)
    obj.name = "AuthoredColossus" + label.title()
    vertices = obj.data.vertices
    lower = Vector([min(v.co[i] for v in vertices) for i in range(3)])
    upper = Vector([max(v.co[i] for v in vertices) for i in range(3)])
    offset = Vector(((lower.x+upper.x)/2, (lower.y+upper.y)/2, lower.z))
    for v in vertices:
        v.co -= offset
    # Flat faces and single-segment physical bevels carry shape. The metre UVs
    # need no image atlas, imported geometry, baked light or hidden floor.
    for face in obj.data.polygons:
        face.use_smooth = False
    obj.data.calc_loop_triangles()
    triangles = len(obj.data.loop_triangles)
    if triangles > 16000:
        raise RuntimeError(f"{label}: {triangles} triangles exceeds budget")
    bm = bmesh.new()
    bm.from_mesh(obj.data)
    non_manifold = sum(not edge.is_manifold for edge in bm.edges)
    volume = bm.calc_volume(signed=True)
    bm.free()
    if non_manifold or volume <= 0:
        raise RuntimeError(f"{label}: invalid solid ({non_manifold=}, {volume=})")
    dimensions = upper-lower
    AUDIT["assets"][label] = {
        "mesh": obj.name, "vertices": len(vertices), "triangles": triangles,
        "godot_size": [round(dimensions.x, 4), round(dimensions.z, 4), round(dimensions.y, 4)],
        "non_manifold_edges": non_manifold, "signed_volume": round(volume, 4),
        "materials": 0, "textures": 0, "external_mesh_sources": [],
    }
    obj.data.materials.clear()
    activate(obj)
    bpy.ops.export_scene.gltf(filepath=str(OUTPUT / f"fallen-colossus-authored-{label}.glb"),
                              export_format="GLB", use_selection=True,
                              export_apply=True, export_yup=True, export_materials="NONE")
    obj.hide_set(True)
    return obj


bpy.ops.wm.read_factory_settings(use_empty=True)
bpy.context.preferences.filepaths.save_version = 0
OUTPUT.mkdir(parents=True, exist_ok=True)
AUTHORING.mkdir(parents=True, exist_ok=True)
finish("legs", legs())
finish("head", head())
for obj in bpy.context.scene.objects:
    obj.hide_set(False)
    if obj.name.endswith("Head"):
        obj.location.x = 30
bpy.ops.wm.save_as_mainfile(filepath=str(AUTHORING / "fallen-colossus.blend"))
(OUTPUT / "fallen-colossus-authored-audit.json").write_text(json.dumps(AUDIT, indent=2)+"\n")
print("[authored-colossus] " + json.dumps(AUDIT))
