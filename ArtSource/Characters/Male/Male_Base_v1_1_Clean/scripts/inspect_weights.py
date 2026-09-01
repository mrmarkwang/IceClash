import bpy
import json
import os
import sys
from collections import defaultdict, deque


source_path, report_path = map(os.path.abspath, sys.argv[sys.argv.index("--") + 1:])
bpy.ops.object.select_all(action="SELECT")
bpy.ops.object.delete(use_global=False)
bpy.ops.import_scene.fbx(
    filepath=source_path,
    global_scale=1.0,
    use_manual_orientation=False,
    bake_space_transform=False,
    ignore_leaf_bones=False,
    force_connect_children=False,
    automatic_bone_orientation=False,
    use_anim=False,
    use_prepost_rot=True,
)
mesh = next(obj for obj in bpy.context.scene.objects if obj.type == "MESH")
group_names = {group.index: group.name for group in mesh.vertex_groups}
weights = {
    vertex.index: {group_names[item.group]: item.weight for item in vertex.groups}
    for vertex in mesh.data.vertices
}
world = {vertex.index: mesh.matrix_world @ vertex.co for vertex in mesh.data.vertices}
adjacency = defaultdict(set)
for edge in mesh.data.edges:
    a, b = edge.vertices
    adjacency[a].add(b)
    adjacency[b].add(a)


def components():
    unseen = set(range(len(mesh.data.vertices)))
    found = []
    while unseen:
        seed = next(iter(unseen))
        queue = deque([seed])
        unseen.remove(seed)
        component = []
        while queue:
            current = queue.popleft()
            component.append(current)
            for neighbor in adjacency[current]:
                if neighbor in unseen:
                    unseen.remove(neighbor)
                    queue.append(neighbor)
        found.append(component)
    return sorted(found, key=len, reverse=True)


regions = {
    "groin": lambda co: abs(co.x) <= 0.35 and 0.62 <= co.z <= 1.02,
    "left_ankle": lambda co: co.x >= 0.02 and 0.02 <= co.z <= 0.34,
    "right_ankle": lambda co: co.x <= -0.02 and 0.02 <= co.z <= 0.34,
    "left_elbow": lambda co: co.x >= 0.30 and 1.18 <= co.z <= 1.58,
    "right_elbow": lambda co: co.x <= -0.30 and 1.18 <= co.z <= 1.58,
    "left_wrist": lambda co: co.x >= 0.58 and 1.20 <= co.z <= 1.58,
    "right_wrist": lambda co: co.x <= -0.58 and 1.20 <= co.z <= 1.58,
}


def summarize(indices):
    active = defaultdict(list)
    for index in indices:
        for name, value in weights[index].items():
            if value > 0.0001:
                active[name].append(value)
    return {
        "vertex_count": len(indices),
        "bounds_min": [round(min(world[index][axis] for index in indices), 6) for axis in range(3)] if indices else [],
        "bounds_max": [round(max(world[index][axis] for index in indices), 6) for axis in range(3)] if indices else [],
        "groups": {
            name: {
                "count": len(values),
                "min": round(min(values), 6),
                "max": round(max(values), 6),
                "mean": round(sum(values) / len(values), 6),
            }
            for name, values in sorted(active.items())
        },
    }


component_data = []
for component in components():
    info = summarize(component)
    info["sample_indices"] = component[:10]
    component_data.append(info)

region_data = {}
for name, predicate in regions.items():
    indices = [index for index, coordinate in world.items() if predicate(coordinate)]
    region_data[name] = summarize(indices)

with open(report_path, "w", encoding="utf-8") as handle:
    json.dump({"components": component_data, "regions": region_data}, handle, indent=2)
print("WEIGHT_REPORT", report_path)
