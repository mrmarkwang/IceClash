import bpy
import json
import math
import os
import sys


source_path, clean_path, cleanup_report_path, output_path = map(
    os.path.abspath, sys.argv[sys.argv.index("--") + 1:]
)


def reset_scene():
    bpy.ops.object.select_all(action="SELECT")
    bpy.ops.object.delete(use_global=False)


def import_fbx(path):
    bpy.ops.import_scene.fbx(
        filepath=path,
        global_scale=1.0,
        use_manual_orientation=False,
        bake_space_transform=False,
        ignore_leaf_bones=False,
        force_connect_children=False,
        automatic_bone_orientation=False,
        use_anim=False,
        use_prepost_rot=True,
    )


def scene_snapshot():
    armature = next(obj for obj in bpy.context.scene.objects if obj.type == "ARMATURE")
    mesh = next(obj for obj in bpy.context.scene.objects if obj.type == "MESH")
    return {
        "bones": [(bone.name, bone.parent.name if bone.parent else None) for bone in sorted(armature.data.bones, key=lambda item: item.name)],
        "armature_matrix": [[float(value) for value in row] for row in armature.matrix_world],
        "mesh_matrix": [[float(value) for value in row] for row in mesh.matrix_world],
        "vertices": len(mesh.data.vertices),
        "polygons": len(mesh.data.polygons),
    }, mesh


reset_scene()
import_fbx(source_path)
source, _ = scene_snapshot()
reset_scene()
import_fbx(clean_path)
clean, mesh = scene_snapshot()
with open(cleanup_report_path, "r", encoding="utf-8") as handle:
    cleanup = json.load(handle)
touched = sorted({entry["vertex"] for entry in cleanup["changes"]})

failures = []
if source["bones"] != clean["bones"]:
    failures.append("bone name/parent map changed")
if source["vertices"] != clean["vertices"] or source["polygons"] != clean["polygons"]:
    failures.append("mesh topology counts changed")


def max_matrix_delta(a, b):
    return max(abs(a[row][column] - b[row][column]) for row in range(4) for column in range(4))


armature_matrix_delta = max_matrix_delta(source["armature_matrix"], clean["armature_matrix"])
mesh_matrix_delta = max_matrix_delta(source["mesh_matrix"], clean["mesh_matrix"])
if armature_matrix_delta > 0.000001 or mesh_matrix_delta > 0.000001:
    failures.append("object matrix changed beyond tolerance")

bad_influence_vertices = []
bad_normalization_vertices = []
for index in touched:
    weights = [membership.weight for membership in mesh.data.vertices[index].groups if membership.weight > 0.000001]
    if len(weights) > 4:
        bad_influence_vertices.append(index)
    if not math.isclose(sum(weights), 1.0, abs_tol=0.0001):
        bad_normalization_vertices.append(index)
if bad_influence_vertices:
    failures.append(f"{len(bad_influence_vertices)} touched vertices exceed four influences")
if bad_normalization_vertices:
    failures.append(f"{len(bad_normalization_vertices)} touched vertices are not normalized")

result = {
    "pass": not failures,
    "failures": failures,
    "bone_count": len(clean["bones"]),
    "bone_names_parents_exact": source["bones"] == clean["bones"],
    "vertex_count": clean["vertices"],
    "polygon_count": clean["polygons"],
    "topology_counts_exact": source["vertices"] == clean["vertices"] and source["polygons"] == clean["polygons"],
    "armature_matrix_max_delta": armature_matrix_delta,
    "mesh_matrix_max_delta": mesh_matrix_delta,
    "touched_vertices": len(touched),
    "touched_max_influences": max(
        len([membership for membership in mesh.data.vertices[index].groups if membership.weight > 0.000001])
        for index in touched
    ),
    "touched_weights_normalized": not bad_normalization_vertices,
    "topology_edits": cleanup["topology_edits"],
    "armature_edits": cleanup["armature_edits"],
}
with open(output_path, "w", encoding="utf-8") as handle:
    json.dump(result, handle, indent=2)
print(json.dumps(result, indent=2))
if failures:
    raise RuntimeError("; ".join(failures))
