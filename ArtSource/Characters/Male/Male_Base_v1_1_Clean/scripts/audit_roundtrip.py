import bpy
import json
import os
import sys


def args_after_double_dash():
    return sys.argv[sys.argv.index("--") + 1:] if "--" in sys.argv else []


def rounded(values, digits=8):
    return [round(float(value), digits) for value in values]


def matrix_values(matrix):
    return [rounded(row) for row in matrix]


def audit_scene(label):
    armatures = sorted((obj for obj in bpy.context.scene.objects if obj.type == "ARMATURE"), key=lambda obj: obj.name)
    meshes = sorted((obj for obj in bpy.context.scene.objects if obj.type == "MESH"), key=lambda obj: obj.name)
    result = {
        "label": label,
        "unit_scale": bpy.context.scene.unit_settings.scale_length,
        "armatures": [],
        "meshes": [],
    }
    for obj in armatures:
        result["armatures"].append({
            "name": obj.name,
            "matrix_world": matrix_values(obj.matrix_world),
            "bones": [
                {
                    "name": bone.name,
                    "parent": bone.parent.name if bone.parent else None,
                    "head_local": rounded(bone.head_local),
                    "tail_local": rounded(bone.tail_local),
                    "matrix_local": matrix_values(bone.matrix_local),
                    "use_deform": bone.use_deform,
                }
                for bone in sorted(obj.data.bones, key=lambda bone: bone.name)
            ],
        })
    for obj in meshes:
        coords = [obj.matrix_world @ vertex.co for vertex in obj.data.vertices]
        mins = [min(co[index] for co in coords) for index in range(3)] if coords else [0.0] * 3
        maxs = [max(co[index] for co in coords) for index in range(3)] if coords else [0.0] * 3
        group_names = [group.name for group in obj.vertex_groups]
        group_counts = {name: 0 for name in group_names}
        max_influences = 0
        for vertex in obj.data.vertices:
            max_influences = max(max_influences, len(vertex.groups))
            for membership in vertex.groups:
                group_counts[group_names[membership.group]] += 1
        result["meshes"].append({
            "name": obj.name,
            "parent": obj.parent.name if obj.parent else None,
            "matrix_world": matrix_values(obj.matrix_world),
            "vertex_count": len(obj.data.vertices),
            "polygon_count": len(obj.data.polygons),
            "bounds_min": rounded(mins),
            "bounds_max": rounded(maxs),
            "dimensions": rounded([maxs[index] - mins[index] for index in range(3)]),
            "vertex_groups": group_names,
            "group_vertex_counts": group_counts,
            "max_influences": max_influences,
        })
    return result


def reset_scene():
    bpy.ops.object.select_all(action="SELECT")
    bpy.ops.object.delete(use_global=False)
    for datablocks in (bpy.data.meshes, bpy.data.armatures, bpy.data.materials, bpy.data.images, bpy.data.actions):
        for datablock in list(datablocks):
            if datablock.users == 0:
                datablocks.remove(datablock)


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


def export_fbx(path):
    bpy.ops.object.select_all(action="DESELECT")
    for obj in bpy.context.scene.objects:
        if obj.type in {"ARMATURE", "MESH"}:
            obj.select_set(True)
    bpy.ops.export_scene.fbx(
        filepath=path,
        use_selection=True,
        global_scale=1.0,
        apply_unit_scale=True,
        apply_scale_options="FBX_SCALE_NONE",
        use_space_transform=True,
        bake_space_transform=False,
        object_types={"ARMATURE", "MESH"},
        use_mesh_modifiers=True,
        add_leaf_bones=False,
        use_armature_deform_only=False,
        bake_anim=False,
        axis_forward="-Z",
        axis_up="Y",
        path_mode="AUTO",
    )


source_path, blend_path, roundtrip_path, report_path = map(os.path.abspath, args_after_double_dash())
reset_scene()
import_fbx(source_path)
source_audit = audit_scene("source_import")
bpy.ops.wm.save_as_mainfile(filepath=blend_path)
export_fbx(roundtrip_path)
reset_scene()
import_fbx(roundtrip_path)
roundtrip_audit = audit_scene("roundtrip_import")
with open(report_path, "w", encoding="utf-8") as handle:
    json.dump({"blender_version": bpy.app.version_string, "source": source_audit, "roundtrip": roundtrip_audit}, handle, indent=2)
print("AUDIT_REPORT", report_path)
