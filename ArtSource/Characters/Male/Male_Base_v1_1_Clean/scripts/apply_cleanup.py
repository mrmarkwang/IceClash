import bpy
import json
import math
import os
import sys
from collections import defaultdict, deque


def smoothstep(edge0, edge1, value):
    value = max(0.0, min(1.0, (value - edge0) / (edge1 - edge0)))
    return value * value * (3.0 - 2.0 * value)


def import_source(path):
    bpy.ops.object.select_all(action="SELECT")
    bpy.ops.object.delete(use_global=False)
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


def component_sets(mesh):
    adjacency = defaultdict(set)
    for edge in mesh.data.edges:
        a, b = edge.vertices
        adjacency[a].add(b)
        adjacency[b].add(a)
    unseen = set(range(len(mesh.data.vertices)))
    found = []
    while unseen:
        seed = next(iter(unseen))
        queue = deque([seed])
        unseen.remove(seed)
        component = set()
        while queue:
            current = queue.popleft()
            component.add(current)
            for neighbor in adjacency[current]:
                if neighbor in unseen:
                    unseen.remove(neighbor)
                    queue.append(neighbor)
        found.append(component)
    return found


def duplicate_reference_collection(mesh, armature):
    source_collection = bpy.data.collections.new("Reference_Unchanged")
    bpy.context.scene.collection.children.link(source_collection)
    for obj in (armature, mesh):
        for collection in list(obj.users_collection):
            collection.objects.unlink(obj)
        source_collection.objects.link(obj)
    source_collection.hide_render = True

    clean_collection = bpy.data.collections.new("Cleanup_Working")
    bpy.context.scene.collection.children.link(clean_collection)
    clean_armature = armature.copy()
    clean_armature.data = armature.data.copy()
    clean_armature.name = "Armature_Clean_Working"
    clean_collection.objects.link(clean_armature)
    clean_mesh = mesh.copy()
    clean_mesh.data = mesh.data.copy()
    clean_mesh.name = "char1_Clean_Working"
    clean_mesh.parent = clean_armature
    clean_collection.objects.link(clean_mesh)
    for modifier in clean_mesh.modifiers:
        if modifier.type == "ARMATURE":
            modifier.object = clean_armature
    source_collection.hide_viewport = True
    return clean_mesh, clean_armature


def export_clean(path, mesh, armature):
    bpy.ops.object.select_all(action="DESELECT")
    mesh.select_set(True)
    armature.select_set(True)
    bpy.context.view_layer.objects.active = armature
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


source_path, blend_path, output_fbx, report_path = map(os.path.abspath, sys.argv[sys.argv.index("--") + 1:])
import_source(source_path)
source_mesh = next(obj for obj in bpy.context.scene.objects if obj.type == "MESH")
source_armature = next(obj for obj in bpy.context.scene.objects if obj.type == "ARMATURE")
mesh, armature = duplicate_reference_collection(source_mesh, source_armature)

groups = {group.name: group for group in mesh.vertex_groups}
group_names = {group.index: group.name for group in mesh.vertex_groups}
components = component_sets(mesh)
world = {vertex.index: mesh.matrix_world @ vertex.co for vertex in mesh.data.vertices}
component_by_vertex = {index: component for component in components for index in component}
component_size = {index: len(component_by_vertex[index]) for index in component_by_vertex}


def current_weights(vertex_index):
    return {group_names[item.group]: item.weight for item in mesh.data.vertices[vertex_index].groups}


changes = []
touched = set()


def replace_weights(vertex_index, replacements, region):
    before = current_weights(vertex_index)
    for name, value in replacements.items():
        if name not in groups:
            continue
        if value <= 0.000001:
            try:
                groups[name].remove([vertex_index])
            except RuntimeError:
                pass
        else:
            groups[name].add([vertex_index], value, "REPLACE")
    after = current_weights(vertex_index)
    if any(abs(before.get(name, 0.0) - after.get(name, 0.0)) > 0.00001 for name in set(before) | set(after)):
        changes.append({
            "vertex": vertex_index,
            "region": region,
            "world": [round(float(value), 6) for value in world[vertex_index]],
            "before": {name: round(value, 6) for name, value in sorted(before.items()) if value > 0.000001},
            "after_local": {name: round(value, 6) for name, value in sorted(after.items()) if value > 0.000001},
        })
        touched.add(vertex_index)


# Shorts/groin component: remove contralateral and lower-leg pulls, retaining a
# Hips-supported centerline and a gradual ipsilateral upper-leg transition.
for vertex in mesh.data.vertices:
    index = vertex.index
    co = world[index]
    if component_size[index] != 781 or not (0.60 <= co.z <= 0.95 and abs(co.x) <= 0.23):
        continue
    before = current_weights(index)
    relevant = sum(before.get(name, 0.0) for name in ("Hips", "LeftUpLeg", "RightUpLeg", "LeftLeg", "RightLeg"))
    if relevant < 0.05:
        continue
    lateral = smoothstep(0.012, 0.115, abs(co.x))
    lower = 1.0 - smoothstep(0.64, 0.98, co.z)
    leg_factor = min(0.95, lateral * (0.25 + 0.75 * lower))
    side_group = "LeftUpLeg" if co.x >= 0.0 else "RightUpLeg"
    opposite_group = "RightUpLeg" if co.x >= 0.0 else "LeftUpLeg"
    replace_weights(index, {
        "Hips": relevant * (1.0 - leg_factor),
        side_group: relevant * leg_factor,
        opposite_group: 0.0,
        "LeftLeg": 0.0,
        "RightLeg": 0.0,
    }, "groin_shorts")


# Ankle blends: use the actual Foot head height (~0.133 m) and spread the
# leg/foot transition across several loops. Existing ToeBase weight is untouched.
for vertex in mesh.data.vertices:
    index = vertex.index
    co = world[index]
    if component_size[index] not in {852, 484, 369} or not (0.095 <= co.z <= 0.245):
        continue
    left = co.x > 0.0
    leg = "LeftLeg" if left else "RightLeg"
    foot = "LeftFoot" if left else "RightFoot"
    before = current_weights(index)
    relevant = before.get(leg, 0.0) + before.get(foot, 0.0)
    if relevant < 0.05:
        continue
    foot_factor = 1.0 - smoothstep(0.11, 0.225, co.z)
    replace_weights(index, {leg: relevant * (1.0 - foot_factor), foot: relevant * foot_factor}, "ankle_foot")


# Elbow/sleeve blend: lateral position follows the unchanged bone chain and
# removes alternating circumference weights responsible for the jagged seam.
for vertex in mesh.data.vertices:
    index = vertex.index
    co = world[index]
    distance = abs(co.x)
    if component_size[index] != 1716 or not (0.36 <= distance <= 0.57 and 1.34 <= co.z <= 1.53):
        continue
    left = co.x > 0.0
    arm = "LeftArm" if left else "RightArm"
    forearm = "LeftForeArm" if left else "RightForeArm"
    before = current_weights(index)
    relevant = before.get(arm, 0.0) + before.get(forearm, 0.0)
    if relevant < 0.10:
        continue
    forearm_factor = smoothstep(0.405, 0.515, distance)
    replace_weights(index, {arm: relevant * (1.0 - forearm_factor), forearm: relevant * forearm_factor}, "sleeve_forearm")


# Wrist border: both disconnected components receive the same continuous
# ForeArm-to-Hand profile, so their coincident border follows the same motion.
for vertex in mesh.data.vertices:
    index = vertex.index
    co = world[index]
    distance = abs(co.x)
    if component_size[index] not in {1716, 745, 761} or not (0.675 <= distance <= 0.805 and 1.37 <= co.z <= 1.52):
        continue
    left = co.x > 0.0
    forearm = "LeftForeArm" if left else "RightForeArm"
    hand = "LeftHand" if left else "RightHand"
    before = current_weights(index)
    relevant = before.get(forearm, 0.0) + before.get(hand, 0.0)
    if relevant < 0.10:
        continue
    hand_factor = smoothstep(0.695, 0.785, distance)
    replace_weights(index, {forearm: relevant * (1.0 - hand_factor), hand: relevant * hand_factor}, "wrist_hand")


# Normalize and limit only the vertices edited above. This intentionally avoids
# changing the remaining model, where the source can contain up to 8 influences.
pruned_count = 0
for index in sorted(touched):
    weights = current_weights(index)
    positive = [(name, value) for name, value in weights.items() if value > 0.000001]
    positive.sort(key=lambda item: item[1], reverse=True)
    if len(positive) > 4:
        keep = {name for name, _ in positive[:4]}
        for name, _ in positive[4:]:
            groups[name].remove([index])
            pruned_count += 1
        weights = current_weights(index)
    total = sum(weights.values())
    if total > 0.0:
        for name, value in weights.items():
            groups[name].add([index], value / total, "REPLACE")

for entry in changes:
    entry["after"] = {
        name: round(value, 6)
        for name, value in sorted(current_weights(entry["vertex"]).items())
        if value > 0.000001
    }
    entry.pop("after_local", None)

armature.name = "Armature"
mesh.name = "char1"
bpy.context.scene["cleanup_notes"] = (
    "Selected-region weight cleanup only: groin/shorts, ankles, sleeve/forearm, wrists. "
    "No topology, armature, rest-pose, transform, material, UV, or animation edits."
)
bpy.ops.wm.save_as_mainfile(filepath=blend_path)
export_clean(output_fbx, mesh, armature)

counts = defaultdict(int)
for entry in changes:
    counts[entry["region"]] += 1
with open(report_path, "w", encoding="utf-8") as handle:
    json.dump({
        "blender_version": bpy.app.version_string,
        "source": source_path,
        "output": output_fbx,
        "topology_edits": 0,
        "armature_edits": 0,
        "touched_vertices": len(touched),
        "pruned_influences": pruned_count,
        "region_counts": dict(sorted(counts.items())),
        "changes": changes,
    }, handle, indent=2)
print("CLEANUP_REPORT", report_path)
print("TOUCHED_VERTICES", len(touched), dict(sorted(counts.items())))
