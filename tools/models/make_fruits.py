"""Procedural low-poly fruit prototypes of GDD 12.2 / A15, exported as .glb (metres, +Y up).

Run from the repo root with Blender 4.2 LTS:
    blender -b --factory-startup -P tools/models/make_fruits.py
Writes Assets/_Project/Art/Fruits/<Fruit>.glb (overwrites). No inputs, fixed seed: the same shapes every run,
though the exporter may reorder indices and change normals in the last float bits, so the bytes can differ.

Each file has two objects: "Body" (sphere of radius 1.0 m, like the placeholder sphere mesh that the fighter prefab
scales to the class radius) and "Details" (face, arms and the top cue). Details are visual only; the collider stays
the sphere on the prefab root. The face looks at Blender -Y, which is +Z (forward) in Unity.
"""
import math
import random
from pathlib import Path

import bpy
import bmesh
from mathutils import Matrix, Vector

OUT = Path.cwd() / "Assets/_Project/Art/Fruits"
R = 0.5  # modelling unit; export scales everything by EXPORT_SCALE so the body radius is 1.0 m
EXPORT_SCALE = 2.0


def hex_rgba(h):
    h = h.lstrip("#")
    srgb = [int(h[i:i + 2], 16) / 255 for i in (0, 2, 4)]
    lin = [c / 12.92 if c <= 0.04045 else ((c + 0.055) / 1.055) ** 2.4 for c in srgb]
    return (*lin, 1.0)


def material(name, color, roughness=0.6):
    m = bpy.data.materials.get(name) or bpy.data.materials.new(name)
    m.use_nodes = True
    bsdf = m.node_tree.nodes["Principled BSDF"]
    bsdf.inputs["Base Color"].default_value = hex_rgba(color)
    bsdf.inputs["Roughness"].default_value = roughness
    return m


def mesh_object(name, bm, mats):
    me = bpy.data.meshes.new(name)
    bm.to_mesh(me)
    bm.free()
    for m in mats:
        me.materials.append(m)
    for p in me.polygons:
        p.use_smooth = True
    ob = bpy.data.objects.new(name, me)
    bpy.context.collection.objects.link(ob)
    return ob


def sphere(bm, radius, center=(0, 0, 0), scale=(1, 1, 1), segments=12, rings=8, mat=0):
    res = bmesh.ops.create_uvsphere(bm, u_segments=segments, v_segments=rings, radius=radius,
                                    matrix=Matrix.Translation(center) @ Matrix.Diagonal((*scale, 1)))
    faces = {f for v in res["verts"] for f in v.link_faces}
    for f in faces:
        f.material_index = mat
    return res["verts"]


def cone(bm, r1, r2, depth, center, rotation=Matrix(), segments=8, mat=0):
    res = bmesh.ops.create_cone(bm, cap_ends=True, segments=segments, radius1=r1, radius2=r2, depth=depth,
                                matrix=Matrix.Translation(center) @ rotation.to_4x4())
    for f in {f for v in res["verts"] for f in v.link_faces}:
        f.material_index = mat


def leaf(bm, at, yaw, tilt, length, mat):
    """Flattened sphere pointing outwards from `at`."""
    rot = Matrix.Rotation(yaw, 4, "Z") @ Matrix.Rotation(tilt, 4, "Y")
    mid = Vector(at) + (rot @ Vector((length / 2, 0, 0, 0))).to_3d()
    res = bmesh.ops.create_uvsphere(bm, u_segments=8, v_segments=5, radius=1.0,
                                    matrix=Matrix.Translation(mid) @ rot @ Matrix.Diagonal((length / 2, length / 5, 0.012, 1)))
    for f in {f for v in res["verts"] for f in v.link_faces}:
        f.material_index = mat


def body(name, color, scale=(1, 1, 1), pattern=None, accent=None, seed=0):
    bm = bmesh.new()
    segments, rings = 24, 16
    sphere(bm, R, scale=scale, segments=segments, rings=rings)
    mats = [material(f"{name}_Body", color)]
    if pattern:
        mats.append(material(f"{name}_Pattern", accent))
        rng = random.Random(seed)
        for f in bm.faces:
            c = f.calc_center_median()
            lon = math.atan2(c.y, c.x)
            lat = math.asin(max(-1.0, min(1.0, c.z / (R * scale[2]))))
            if pattern(lon, lat, rng):
                f.material_index = 1
    return mesh_object("Body", bm, mats)


def details(name, color, top, scale=(1, 1, 1), face=True):
    """Dot eyes, blush and stubby arms on every fruit, plus the fruit's own top cue."""
    bm = bmesh.new()
    mats = [material("Eye", "#1E1E24", 0.3), material("Blush", "#FF7A8A"), material(f"{name}_Body", color)]
    sx, sy, sz = (R * s for s in scale)
    if face:
        for side in (-1, 1):
            sphere(bm, 0.055, center=(side * 0.15 * scale[0], -sy * 0.93, 0.08 * scale[2]), scale=(1, 0.6, 1.3), mat=0)
            sphere(bm, 0.06, center=(side * 0.27 * scale[0], -sy * 0.86, -0.04 * scale[2]), scale=(1, 0.35, 0.6), mat=1)
            sphere(bm, 0.08, center=(side * sx * 1.02, 0, -0.08 * scale[2]), scale=(1.1, 0.8, 0.8), mat=2)
    top(bm, mats, sz)
    return mesh_object("Details", bm, mats)


def stem(bm, mats, top_z, height=0.12, lean=0.2):
    mats.append(material("Stem", "#6B4226"))
    cone(bm, 0.025, 0.018, height, (0, 0, top_z + height / 2 - 0.02), Matrix.Rotation(lean, 3, "X"), mat=len(mats) - 1)


def green_leaf(bm, mats, at, yaw, tilt, length):
    if "Leaf" not in [m.name for m in mats]:
        mats.append(material("Leaf", "#3FA34D"))
    leaf(bm, at, yaw, tilt, length, [m.name for m in mats].index("Leaf"))


def diamond(lon, lat):
    """Diagonal lattice on the 24 x 16 face grid: the diamond pattern of the pineapple."""
    i, j = round(lon / (math.pi / 12) - 0.5), round(lat / (math.pi / 16) - 0.5)
    return (i + j) % 4 == 0 or (i - j) % 4 == 0


# GDD 12.2 table: body colour and silhouette cue per fruit.
def watermelon():
    scale = (1.12, 1.0, 0.9)
    b = body("Watermelon", "#5CC46E", scale, lambda lon, lat, _: math.sin(lon * 8 + math.sin(lat * 9) * 0.8) > 0.55,
             "#2E7D3A")

    def top(bm, mats, z):
        stem(bm, mats, z, 0.06, 0.1)
    return b, details("Watermelon", "#5CC46E", top, scale)


def pineapple():
    scale = (0.92, 0.92, 1.15)
    b = body("Pineapple", "#FFC93C", scale,
             lambda lon, lat, _: diamond(lon, lat),
             "#C98A1A")

    def top(bm, mats, z):
        for i in range(5):
            green_leaf(bm, mats, (0, 0, z - 0.03), i * 2 * math.pi / 5, -1.1, 0.32)
    return b, details("Pineapple", "#FFC93C", top, scale)


def apple():
    def top(bm, mats, z):
        stem(bm, mats, z - 0.04, 0.14, 0.25)
        green_leaf(bm, mats, (0, 0.02, z + 0.04), math.pi / 2, -0.4, 0.2)
    bm_scale = (1.0, 1.0, 0.95)
    b = body("Apple", "#EF4B4B", bm_scale)
    # Top dimple: pull the top ring of the body down a little.
    for v in b.data.vertices:
        if v.co.z > R * 0.9:
            v.co.z -= 0.05
    return b, details("Apple", "#EF4B4B", top, bm_scale)


def mangosteen():
    def top(bm, mats, z):
        mats.append(material("Calyx", "#4E8B3A"))
        for i in range(4):
            leaf(bm, (0, 0, z - 0.01), i * math.pi / 2 + math.pi / 4, -0.15, 0.22, len(mats) - 1)
        stem(bm, mats, z, 0.08, 0.0)
    return body("Mangosteen", "#7A4FA3"), details("Mangosteen", "#7A4FA3", top)


def peach():
    scale = (1.0, 0.97, 1.0)
    b = body("Peach", "#FFA3B5", scale, lambda lon, lat, _: abs(math.sin(lon)) < 0.13 and math.cos(lon) > 0 and lat > -0.6,
             "#F07F98")

    def top(bm, mats, z):
        green_leaf(bm, mats, (0, 0, z - 0.01), 0.3, -0.5, 0.24)
    return b, details("Peach", "#FFA3B5", top, scale)


def blueberry():
    b = body("Blueberry", "#4D7CDB", pattern=lambda lon, lat, rng: rng.random() < 0.06, accent="#A9C2F2", seed=18)

    def top(bm, mats, z):
        mats.append(material("Crown", "#2F4E9E"))
        for i in range(5):
            leaf(bm, (0, 0, z - 0.015), i * 2 * math.pi / 5, -0.6, 0.12, len(mats) - 1)
    return b, details("Blueberry", "#4D7CDB", top)


def export(name, objects):
    OUT.mkdir(parents=True, exist_ok=True)
    bpy.ops.object.select_all(action="DESELECT")
    for ob in objects:
        ob.data.transform(Matrix.Scale(EXPORT_SCALE, 4))
        ob.select_set(True)
    bpy.ops.export_scene.gltf(filepath=str(OUT / f"{name}.glb"), export_format="GLB", use_selection=True,
                              export_yup=True, export_apply=True, export_texcoords=False, export_cameras=False,
                              export_lights=False)


def main():
    for name, build in (("Watermelon", watermelon), ("Pineapple", pineapple), ("Apple", apple),
                        ("Mangosteen", mangosteen), ("Peach", peach), ("Blueberry", blueberry)):
        bpy.ops.wm.read_factory_settings(use_empty=True)
        export(name, build())
        print(f"[make_fruits] {name}.glb")


if __name__ == "__main__":
    main()
