# TriKat v10: taş 3D modellerini mobil için sadeleştirir (Blender 4.2, arka planda çalışır).
# Kullanım: blender.exe -b --factory-startup -P tools/blender/decimate_stones.py -- <girdi.fbx> <çıktı.fbx> <hedef_üçgen>
# Girdi: Tripo FBX (docs/taşlar/3d_ve_png/*.zip içinden). Çıktı: yalnız geometri + UV (dokular ayrıca küçültülür).
# Ölçek: en uzun kenar 1 birim, taban y=0, merkez x/z=0 (Unity'de önizleme kamerası buna göre).
import bpy, sys, bmesh
from mathutils import Vector

argv = sys.argv[sys.argv.index("--") + 1:]
src, dst, target = argv[0], argv[1], int(argv[2])

bpy.ops.wm.read_factory_settings(use_empty=True)
bpy.ops.import_scene.fbx(filepath=src)
meshes = [o for o in bpy.context.scene.objects if o.type == 'MESH']
for o in bpy.context.scene.objects:
    o.select_set(o.type == 'MESH')
bpy.context.view_layer.objects.active = meshes[0]
if len(meshes) > 1:
    bpy.ops.object.join()
obj = bpy.context.view_layer.objects.active
bpy.ops.object.transform_apply(location=True, rotation=True, scale=True)

def tris(o):
    return sum(len(p.vertices) - 2 for p in o.data.polygons)

before = tris(obj)
if before > target:
    m = obj.modifiers.new("Decimate", 'DECIMATE')
    m.decimate_type = 'COLLAPSE'
    m.ratio = max(0.002, target / before)
    m.use_collapse_triangulate = True
    bpy.ops.object.modifier_apply(modifier=m.name)
after = tris(obj)

# Normalize: en uzun kenar 1, taban 0'da, ortada.
bb = [obj.matrix_world @ Vector(c) for c in obj.bound_box]
mn = Vector((min(v.x for v in bb), min(v.y for v in bb), min(v.z for v in bb)))
mx = Vector((max(v.x for v in bb), max(v.y for v in bb), max(v.z for v in bb)))
size = max(mx.x - mn.x, mx.y - mn.y, mx.z - mn.z)
s = 1.0 / size if size > 0 else 1.0
center = Vector(((mn.x + mx.x) / 2, (mn.y + mx.y) / 2, mn.z))
for v in obj.data.vertices:
    v.co = (v.co - center) * s
bpy.ops.object.shade_smooth()

bpy.ops.export_scene.fbx(filepath=dst, use_selection=True, object_types={'MESH'}, path_mode='STRIP',
                         embed_textures=False, apply_scale_options='FBX_SCALE_UNITS', axis_forward='-Z', axis_up='Y',
                         mesh_smooth_type='FACE', use_mesh_modifiers=True)
print(f"KATKAT_DECIMATE {src} tris {before} -> {after}")
