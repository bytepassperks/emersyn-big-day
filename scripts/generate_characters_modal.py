"""
Modal GPU Blender Pipeline: Generate 9 rigged 3D chibi characters for Emersyn's Big Day.
Uses Blender's Python API on Modal with GPU acceleration for rendering.

Characters:
1. Emersyn (main character, cute girl with pigtails)
2. Ava (friend, short bob hair)
3. Mia (friend, long braids)
4. Leo (friend, spiky hair boy)
5. Shopkeeper (older NPC, glasses and apron)
6. Teacher (NPC, bun hair and book)
7. Cat (pet, round kawaii cat)
8. Dog (pet, floppy ears puppy)
9. Bunny (pet, long ears rabbit)

Each character is exported as .glb with:
- Humanoid skeleton (or quadruped for pets)
- Multiple animations baked in
- PBR materials with kawaii colors
- Optimized poly count for mobile (2K-5K tris)
"""

import modal
import os

app = modal.App("emersyn-blender-characters")

# Blender image with dependencies
blender_image = (
    modal.Image.debian_slim(python_version="3.11")
    .apt_install(
        "blender", "xvfb", "libxi6", "libxxf86vm1", "libxfixes3",
        "libxrender1", "libgl1-mesa-glx", "libglu1-mesa"
    )
    .pip_install("numpy", "boto3")
)

# iDrive e2 credentials
S3_ENDPOINT = "https://s3.us-west-1.idrivee2.com"
S3_BUCKET = "crop-spray-uploads"
S3_ACCESS_KEY = os.environ.get("S3_ACCESS_KEY", "EQQ53Vm4Cr9Rov1FsOPt")
S3_SECRET_KEY = os.environ.get("S3_SECRET_KEY", "far8XneFX3NH9HFUjAAt9YZ3CB8RmJiCvKpe6")


def upload_to_s3(local_path: str, s3_key: str):
    """Upload file to iDrive e2 S3 storage."""
    import boto3
    s3 = boto3.client(
        "s3",
        endpoint_url=S3_ENDPOINT,
        aws_access_key_id=S3_ACCESS_KEY,
        aws_secret_access_key=S3_SECRET_KEY,
    )
    s3.upload_file(local_path, S3_BUCKET, s3_key)
    print(f"Uploaded {local_path} -> s3://{S3_BUCKET}/{s3_key}")


# Character definitions
CHARACTERS = {
    "emersyn": {
        "type": "humanoid",
        "body_color": (1.0, 0.85, 0.75, 1.0),  # Warm skin
        "hair_color": (0.4, 0.2, 0.1, 1.0),  # Brown
        "outfit_color": (1.0, 0.4, 0.6, 1.0),  # Pink
        "accent_color": (1.0, 0.85, 0.3, 1.0),  # Yellow
        "eye_color": (0.2, 0.5, 0.9, 1.0),  # Blue
        "hair_style": "pigtails",
        "height": 1.0,
        "head_scale": 1.6,  # Chibi proportions
        "description": "Main character - cute girl with pigtails and pink dress"
    },
    "ava": {
        "type": "humanoid",
        "body_color": (0.95, 0.82, 0.7, 1.0),
        "hair_color": (0.9, 0.75, 0.3, 1.0),  # Blonde
        "outfit_color": (0.4, 0.7, 1.0, 1.0),  # Light blue
        "accent_color": (1.0, 1.0, 1.0, 1.0),  # White
        "eye_color": (0.3, 0.7, 0.3, 1.0),  # Green
        "hair_style": "bob",
        "height": 0.95,
        "head_scale": 1.6,
        "description": "Friend - short blonde bob, blue outfit"
    },
    "mia": {
        "type": "humanoid",
        "body_color": (0.75, 0.55, 0.4, 1.0),
        "hair_color": (0.15, 0.1, 0.05, 1.0),  # Dark
        "outfit_color": (0.8, 0.4, 1.0, 1.0),  # Purple
        "accent_color": (1.0, 0.6, 0.8, 1.0),  # Pink
        "eye_color": (0.45, 0.25, 0.1, 1.0),  # Brown
        "hair_style": "braids",
        "height": 0.98,
        "head_scale": 1.6,
        "description": "Friend - dark braids, purple dress"
    },
    "leo": {
        "type": "humanoid",
        "body_color": (0.92, 0.8, 0.68, 1.0),
        "hair_color": (0.85, 0.45, 0.1, 1.0),  # Orange
        "outfit_color": (0.2, 0.7, 0.3, 1.0),  # Green
        "accent_color": (0.9, 0.85, 0.2, 1.0),  # Yellow
        "eye_color": (0.35, 0.55, 0.2, 1.0),  # Hazel
        "hair_style": "spiky",
        "height": 1.02,
        "head_scale": 1.5,
        "description": "Friend - spiky orange hair, green shirt, boy"
    },
    "shopkeeper": {
        "type": "humanoid",
        "body_color": (0.9, 0.78, 0.65, 1.0),
        "hair_color": (0.6, 0.6, 0.6, 1.0),  # Gray
        "outfit_color": (0.9, 0.85, 0.7, 1.0),  # Beige
        "accent_color": (0.6, 0.3, 0.1, 1.0),  # Brown
        "eye_color": (0.3, 0.3, 0.3, 1.0),
        "hair_style": "short",
        "height": 1.1,
        "head_scale": 1.4,
        "description": "NPC - friendly shopkeeper with apron and glasses"
    },
    "teacher": {
        "type": "humanoid",
        "body_color": (0.88, 0.76, 0.63, 1.0),
        "hair_color": (0.3, 0.15, 0.05, 1.0),  # Dark brown
        "outfit_color": (0.3, 0.3, 0.6, 1.0),  # Navy
        "accent_color": (1.0, 0.9, 0.7, 1.0),  # Cream
        "eye_color": (0.25, 0.35, 0.6, 1.0),
        "hair_style": "bun",
        "height": 1.08,
        "head_scale": 1.4,
        "description": "NPC - teacher with hair bun and navy outfit"
    },
    "cat": {
        "type": "quadruped",
        "body_color": (1.0, 0.6, 0.3, 1.0),  # Orange tabby
        "accent_color": (1.0, 1.0, 1.0, 1.0),  # White belly
        "eye_color": (0.4, 0.8, 0.3, 1.0),  # Green
        "height": 0.4,
        "head_scale": 2.0,  # Extra big head for kawaii
        "description": "Pet - round kawaii orange tabby cat"
    },
    "dog": {
        "type": "quadruped",
        "body_color": (0.85, 0.7, 0.45, 1.0),  # Golden
        "accent_color": (0.95, 0.85, 0.65, 1.0),  # Light gold belly
        "eye_color": (0.3, 0.2, 0.1, 1.0),  # Dark brown
        "height": 0.5,
        "head_scale": 1.8,
        "description": "Pet - cute golden puppy with floppy ears"
    },
    "bunny": {
        "type": "quadruped",
        "body_color": (1.0, 1.0, 1.0, 1.0),  # White
        "accent_color": (1.0, 0.7, 0.8, 1.0),  # Pink inner ears
        "eye_color": (0.8, 0.2, 0.3, 1.0),  # Red/pink
        "height": 0.35,
        "head_scale": 2.2,  # Very big head for max kawaii
        "description": "Pet - fluffy white bunny with pink ears"
    },
}

# Animation definitions
HUMANOID_ANIMATIONS = [
    "Idle", "IdleLookAround", "IdleScratchHead", "IdleYawn", "IdleBlink",
    "Walk", "Run", "Jump", "Dance", "Wave", "Clap",
    "Eat", "Sleep", "SitDown", "StandUp",
    "Happy", "Sad", "Angry", "Surprised", "Shy", "Excited",
    "PokeReaction", "PetReaction", "TickleReaction",
    "CookStir", "CookChop", "Paint", "Read", "Sing",
    "PickUp", "PutDown", "Point", "ThumbsUp",
    "Spin", "Cartwheel", "BowPolite",
]

PET_ANIMATIONS = [
    "Idle", "IdleLookAround", "Walk", "Run", "Jump",
    "Eat", "Sleep", "CurlUp",
    "Happy", "Sad", "Surprised", "Annoyed",
    "PetReaction", "PokeReaction",
    "Sniff", "Shake",
]

CAT_EXTRA = ["LickPaw", "StretchIdle", "TailFlick", "Pounce", "Purr", "NuzzleUp"]
DOG_EXTRA = ["WagTail", "Pant", "ScratchEar", "Spin", "Fetch", "SitIdle", "Beg"]
BUNNY_EXTRA = ["TwitchNose", "HopInPlace", "CleanFace", "EarWiggle", "Binky", "Dig"]


BLENDER_SCRIPT = '''
import bpy
import bmesh
import math
import json
import sys
import os

# Parse arguments
argv = sys.argv
args_idx = argv.index("--") + 1 if "--" in argv else len(argv)
args = argv[args_idx:]
char_name = args[0] if len(args) > 0 else "emersyn"
char_data_json = args[1] if len(args) > 1 else "{}"
animations_json = args[2] if len(args) > 2 else "[]"
output_path = args[3] if len(args) > 3 else "/tmp/output.glb"

char_data = json.loads(char_data_json)
animation_names = json.loads(animations_json)

# Clear scene
bpy.ops.object.select_all(action='SELECT')
bpy.ops.object.delete()

# --- MATERIAL HELPERS ---
def create_material(name, color):
    mat = bpy.data.materials.new(name=name)
    mat.use_nodes = True
    bsdf = mat.node_tree.nodes.get("Principled BSDF")
    if bsdf:
        bsdf.inputs["Base Color"].default_value = color
        bsdf.inputs["Roughness"].default_value = 0.8
        # Blender 3.x uses "Specular", 4.0+ uses "Specular IOR Level"
        spec_key = "Specular IOR Level" if "Specular IOR Level" in bsdf.inputs else "Specular"
        bsdf.inputs[spec_key].default_value = 0.3
    return mat

# --- CHIBI HUMANOID BUILDER ---
def build_humanoid(data):
    height = data.get("height", 1.0)
    head_scale = data.get("head_scale", 1.6)

    body_mat = create_material(f"{char_name}_body", data["body_color"])
    hair_mat = create_material(f"{char_name}_hair", data.get("hair_color", (0.3, 0.2, 0.1, 1.0)))
    outfit_mat = create_material(f"{char_name}_outfit", data.get("outfit_color", (1.0, 0.4, 0.6, 1.0)))
    eye_mat = create_material(f"{char_name}_eyes", data.get("eye_color", (0.2, 0.5, 0.9, 1.0)))
    white_mat = create_material(f"{char_name}_white", (1.0, 1.0, 1.0, 1.0))
    mouth_mat = create_material(f"{char_name}_mouth", (0.9, 0.4, 0.4, 1.0))

    objects = []

    # HEAD (big chibi head)
    head_size = 0.22 * head_scale
    bpy.ops.mesh.primitive_uv_sphere_add(radius=head_size, location=(0, 0, height * 0.78), segments=24, ring_count=16)
    head = bpy.context.active_object
    head.name = f"{char_name}_head"
    head.data.materials.append(body_mat)
    objects.append(head)

    # EYES
    eye_size = head_size * 0.18
    for side in [-1, 1]:
        bpy.ops.mesh.primitive_uv_sphere_add(
            radius=eye_size,
            location=(side * head_size * 0.35, -head_size * 0.85, height * 0.8),
            segments=12, ring_count=8
        )
        eye_white = bpy.context.active_object
        eye_white.name = f"{char_name}_eye_white_{side}"
        eye_white.data.materials.append(white_mat)
        objects.append(eye_white)

        # Pupil
        bpy.ops.mesh.primitive_uv_sphere_add(
            radius=eye_size * 0.55,
            location=(side * head_size * 0.35, -head_size * 0.93, height * 0.8),
            segments=10, ring_count=6
        )
        pupil = bpy.context.active_object
        pupil.name = f"{char_name}_pupil_{side}"
        pupil.data.materials.append(eye_mat)
        objects.append(pupil)

        # Eye shine
        bpy.ops.mesh.primitive_uv_sphere_add(
            radius=eye_size * 0.2,
            location=(side * head_size * 0.3, -head_size * 0.97, height * 0.82),
            segments=6, ring_count=4
        )
        shine = bpy.context.active_object
        shine.name = f"{char_name}_eyeshine_{side}"
        shine.data.materials.append(white_mat)
        objects.append(shine)

    # MOUTH
    bpy.ops.mesh.primitive_uv_sphere_add(
        radius=head_size * 0.08,
        location=(0, -head_size * 0.88, height * 0.72),
        segments=8, ring_count=6
    )
    mouth = bpy.context.active_object
    mouth.name = f"{char_name}_mouth"
    mouth.scale = (2.0, 0.5, 0.8)
    mouth.data.materials.append(mouth_mat)
    objects.append(mouth)

    # NOSE
    bpy.ops.mesh.primitive_uv_sphere_add(
        radius=head_size * 0.04,
        location=(0, -head_size * 0.9, height * 0.76),
        segments=6, ring_count=4
    )
    nose = bpy.context.active_object
    nose.name = f"{char_name}_nose"
    nose.data.materials.append(body_mat)
    objects.append(nose)

    # BLUSH SPOTS
    for side in [-1, 1]:
        bpy.ops.mesh.primitive_uv_sphere_add(
            radius=head_size * 0.06,
            location=(side * head_size * 0.5, -head_size * 0.75, height * 0.74),
            segments=8, ring_count=4
        )
        blush = bpy.context.active_object
        blush.name = f"{char_name}_blush_{side}"
        blush.scale = (1.5, 0.3, 1.0)
        blush_mat = create_material(f"{char_name}_blush", (1.0, 0.6, 0.6, 0.5))
        blush.data.materials.append(blush_mat)
        objects.append(blush)

    # BODY (torso)
    bpy.ops.mesh.primitive_uv_sphere_add(
        radius=0.15,
        location=(0, 0, height * 0.5),
        segments=16, ring_count=12
    )
    torso = bpy.context.active_object
    torso.name = f"{char_name}_torso"
    torso.scale = (1.0, 0.7, 1.3)
    torso.data.materials.append(outfit_mat)
    objects.append(torso)

    # ARMS
    for side in [-1, 1]:
        bpy.ops.mesh.primitive_cylinder_add(
            radius=0.04,
            depth=0.25,
            location=(side * 0.18, 0, height * 0.52),
            rotation=(0, 0, side * 0.3)
        )
        arm = bpy.context.active_object
        arm.name = f"{char_name}_arm_{side}"
        arm.data.materials.append(outfit_mat)
        objects.append(arm)

        # Hand
        bpy.ops.mesh.primitive_uv_sphere_add(
            radius=0.05,
            location=(side * 0.28, 0, height * 0.42),
            segments=10, ring_count=6
        )
        hand = bpy.context.active_object
        hand.name = f"{char_name}_hand_{side}"
        hand.data.materials.append(body_mat)
        objects.append(hand)

    # LEGS
    for side in [-1, 1]:
        bpy.ops.mesh.primitive_cylinder_add(
            radius=0.05,
            depth=0.25,
            location=(side * 0.08, 0, height * 0.2)
        )
        leg = bpy.context.active_object
        leg.name = f"{char_name}_leg_{side}"
        leg.data.materials.append(outfit_mat)
        objects.append(leg)

        # Foot/shoe
        bpy.ops.mesh.primitive_uv_sphere_add(
            radius=0.06,
            location=(side * 0.08, -0.02, height * 0.06),
            segments=10, ring_count=6
        )
        foot = bpy.context.active_object
        foot.name = f"{char_name}_foot_{side}"
        foot.scale = (1.0, 1.4, 0.7)
        shoe_mat = create_material(f"{char_name}_shoe", data.get("accent_color", (1.0, 0.85, 0.3, 1.0)))
        foot.data.materials.append(shoe_mat)
        objects.append(foot)

    # HAIR (style-dependent)
    hair_style = data.get("hair_style", "short")
    if hair_style == "pigtails":
        # Two pigtail spheres
        for side in [-1, 1]:
            bpy.ops.mesh.primitive_uv_sphere_add(
                radius=head_size * 0.4,
                location=(side * head_size * 0.65, head_size * 0.2, height * 0.82),
                segments=12, ring_count=8
            )
            pigtail = bpy.context.active_object
            pigtail.name = f"{char_name}_pigtail_{side}"
            pigtail.scale = (0.7, 0.7, 1.2)
            pigtail.data.materials.append(hair_mat)
            objects.append(pigtail)
        # Hair cap
        bpy.ops.mesh.primitive_uv_sphere_add(
            radius=head_size * 1.05,
            location=(0, head_size * 0.1, height * 0.82),
            segments=16, ring_count=8
        )
        cap = bpy.context.active_object
        cap.name = f"{char_name}_haircap"
        cap.scale = (1.0, 1.0, 0.6)
        cap.data.materials.append(hair_mat)
        objects.append(cap)

    elif hair_style == "bob":
        bpy.ops.mesh.primitive_uv_sphere_add(
            radius=head_size * 1.1,
            location=(0, head_size * 0.05, height * 0.78),
            segments=16, ring_count=10
        )
        bob = bpy.context.active_object
        bob.name = f"{char_name}_hair_bob"
        bob.scale = (1.0, 1.0, 0.85)
        bob.data.materials.append(hair_mat)
        objects.append(bob)

    elif hair_style == "braids":
        # Two braid cylinders
        for side in [-1, 1]:
            bpy.ops.mesh.primitive_cylinder_add(
                radius=head_size * 0.12,
                depth=head_size * 1.2,
                location=(side * head_size * 0.45, head_size * 0.25, height * 0.65)
            )
            braid = bpy.context.active_object
            braid.name = f"{char_name}_braid_{side}"
            braid.data.materials.append(hair_mat)
            objects.append(braid)
        # Hair cap
        bpy.ops.mesh.primitive_uv_sphere_add(
            radius=head_size * 1.05,
            location=(0, head_size * 0.1, height * 0.82),
            segments=16, ring_count=8
        )
        cap = bpy.context.active_object
        cap.name = f"{char_name}_haircap"
        cap.scale = (1.0, 1.0, 0.6)
        cap.data.materials.append(hair_mat)
        objects.append(cap)

    elif hair_style == "spiky":
        for i in range(5):
            angle = (i / 5) * math.pi * 2
            x = math.sin(angle) * head_size * 0.3
            y = math.cos(angle) * head_size * 0.3
            bpy.ops.mesh.primitive_cone_add(
                radius1=head_size * 0.15,
                depth=head_size * 0.5,
                location=(x, y, height * 0.95),
                rotation=(math.sin(angle) * 0.3, math.cos(angle) * 0.3, 0)
            )
            spike = bpy.context.active_object
            spike.name = f"{char_name}_spike_{i}"
            spike.data.materials.append(hair_mat)
            objects.append(spike)
        # Base cap
        bpy.ops.mesh.primitive_uv_sphere_add(
            radius=head_size * 1.02,
            location=(0, 0, height * 0.82),
            segments=12, ring_count=6
        )
        cap = bpy.context.active_object
        cap.name = f"{char_name}_haircap"
        cap.scale = (1.0, 1.0, 0.55)
        cap.data.materials.append(hair_mat)
        objects.append(cap)

    else:  # "short" or "bun"
        bpy.ops.mesh.primitive_uv_sphere_add(
            radius=head_size * 1.08,
            location=(0, head_size * 0.05, height * 0.8),
            segments=16, ring_count=10
        )
        hair = bpy.context.active_object
        hair.name = f"{char_name}_hair"
        hair.scale = (1.0, 1.0, 0.7)
        hair.data.materials.append(hair_mat)
        objects.append(hair)

        if hair_style == "bun":
            bpy.ops.mesh.primitive_uv_sphere_add(
                radius=head_size * 0.3,
                location=(0, head_size * 0.6, height * 0.92),
                segments=10, ring_count=6
            )
            bun = bpy.context.active_object
            bun.name = f"{char_name}_bun"
            bun.data.materials.append(hair_mat)
            objects.append(bun)

    return objects

# --- QUADRUPED BUILDER ---
def build_quadruped(data):
    height = data.get("height", 0.4)
    head_scale = data.get("head_scale", 2.0)

    body_mat = create_material(f"{char_name}_body", data["body_color"])
    accent_mat = create_material(f"{char_name}_accent", data.get("accent_color", (1.0, 1.0, 1.0, 1.0)))
    eye_mat = create_material(f"{char_name}_eyes", data.get("eye_color", (0.3, 0.8, 0.3, 1.0)))
    white_mat = create_material(f"{char_name}_white", (1.0, 1.0, 1.0, 1.0))
    nose_mat = create_material(f"{char_name}_nose", (0.2, 0.15, 0.1, 1.0))

    objects = []

    # BODY
    bpy.ops.mesh.primitive_uv_sphere_add(
        radius=height * 0.6,
        location=(0, 0, height * 0.65),
        segments=16, ring_count=12
    )
    body = bpy.context.active_object
    body.name = f"{char_name}_body"
    body.scale = (0.8, 1.2, 0.9)
    body.data.materials.append(body_mat)
    objects.append(body)

    # HEAD
    head_size = height * 0.5 * head_scale
    bpy.ops.mesh.primitive_uv_sphere_add(
        radius=head_size,
        location=(0, -height * 0.7, height * 0.9),
        segments=20, ring_count=14
    )
    head = bpy.context.active_object
    head.name = f"{char_name}_head"
    head.data.materials.append(body_mat)
    objects.append(head)

    # EYES
    eye_size = head_size * 0.2
    for side in [-1, 1]:
        bpy.ops.mesh.primitive_uv_sphere_add(
            radius=eye_size,
            location=(side * head_size * 0.4, -height * 0.7 - head_size * 0.8, height * 0.95),
            segments=10, ring_count=6
        )
        eye = bpy.context.active_object
        eye.name = f"{char_name}_eye_{side}"
        eye.data.materials.append(white_mat)
        objects.append(eye)

        bpy.ops.mesh.primitive_uv_sphere_add(
            radius=eye_size * 0.55,
            location=(side * head_size * 0.4, -height * 0.7 - head_size * 0.88, height * 0.95),
            segments=8, ring_count=5
        )
        pupil = bpy.context.active_object
        pupil.name = f"{char_name}_pupil_{side}"
        pupil.data.materials.append(eye_mat)
        objects.append(pupil)

    # NOSE
    bpy.ops.mesh.primitive_uv_sphere_add(
        radius=head_size * 0.1,
        location=(0, -height * 0.7 - head_size * 0.9, height * 0.87),
        segments=8, ring_count=5
    )
    nose = bpy.context.active_object
    nose.name = f"{char_name}_nose"
    nose.data.materials.append(nose_mat)
    objects.append(nose)

    # EARS (varies by animal)
    if char_name == "cat":
        for side in [-1, 1]:
            bpy.ops.mesh.primitive_cone_add(
                radius1=head_size * 0.25,
                depth=head_size * 0.5,
                location=(side * head_size * 0.5, -height * 0.55, height * 1.2)
            )
            ear = bpy.context.active_object
            ear.name = f"{char_name}_ear_{side}"
            ear.rotation_euler = (0, side * 0.2, 0)
            ear.data.materials.append(body_mat)
            objects.append(ear)

    elif char_name == "dog":
        for side in [-1, 1]:
            bpy.ops.mesh.primitive_uv_sphere_add(
                radius=head_size * 0.3,
                location=(side * head_size * 0.55, -height * 0.5, height * 0.85),
                segments=10, ring_count=6
            )
            ear = bpy.context.active_object
            ear.name = f"{char_name}_ear_{side}"
            ear.scale = (0.6, 0.4, 1.2)  # Floppy
            ear.data.materials.append(body_mat)
            objects.append(ear)

    elif char_name == "bunny":
        for side in [-1, 1]:
            bpy.ops.mesh.primitive_cylinder_add(
                radius=head_size * 0.12,
                depth=head_size * 0.8,
                location=(side * head_size * 0.25, -height * 0.5, height * 1.3)
            )
            ear = bpy.context.active_object
            ear.name = f"{char_name}_ear_{side}"
            ear.rotation_euler = (0, side * 0.15, 0)
            ear.data.materials.append(body_mat)
            objects.append(ear)

            # Inner ear
            bpy.ops.mesh.primitive_cylinder_add(
                radius=head_size * 0.07,
                depth=head_size * 0.75,
                location=(side * head_size * 0.25, -height * 0.52, height * 1.3)
            )
            inner = bpy.context.active_object
            inner.name = f"{char_name}_inner_ear_{side}"
            inner.rotation_euler = (0, side * 0.15, 0)
            inner.data.materials.append(accent_mat)
            objects.append(inner)

    # LEGS (4 legs)
    for i, (lx, ly) in enumerate([(-0.12, -0.2), (0.12, -0.2), (-0.12, 0.2), (0.12, 0.2)]):
        bpy.ops.mesh.primitive_cylinder_add(
            radius=height * 0.1,
            depth=height * 0.5,
            location=(lx, ly, height * 0.25)
        )
        leg = bpy.context.active_object
        leg.name = f"{char_name}_leg_{i}"
        leg.data.materials.append(body_mat)
        objects.append(leg)

        # Paw
        bpy.ops.mesh.primitive_uv_sphere_add(
            radius=height * 0.1,
            location=(lx, ly, height * 0.02),
            segments=8, ring_count=5
        )
        paw = bpy.context.active_object
        paw.name = f"{char_name}_paw_{i}"
        paw.scale = (1.0, 1.2, 0.5)
        paw.data.materials.append(accent_mat)
        objects.append(paw)

    # TAIL
    if char_name == "cat":
        bpy.ops.mesh.primitive_cylinder_add(
            radius=height * 0.05,
            depth=height * 0.6,
            location=(0, height * 0.5, height * 0.7),
            rotation=(0.8, 0, 0)
        )
    elif char_name == "dog":
        bpy.ops.mesh.primitive_cylinder_add(
            radius=height * 0.06,
            depth=height * 0.4,
            location=(0, height * 0.45, height * 0.8),
            rotation=(0.5, 0, 0)
        )
    else:  # bunny
        bpy.ops.mesh.primitive_uv_sphere_add(
            radius=height * 0.12,
            location=(0, height * 0.35, height * 0.65),
            segments=8, ring_count=5
        )
    tail = bpy.context.active_object
    tail.name = f"{char_name}_tail"
    tail.data.materials.append(body_mat)
    objects.append(tail)

    # BELLY
    bpy.ops.mesh.primitive_uv_sphere_add(
        radius=height * 0.4,
        location=(0, -0.05, height * 0.6),
        segments=12, ring_count=8
    )
    belly = bpy.context.active_object
    belly.name = f"{char_name}_belly"
    belly.scale = (0.7, 1.0, 0.7)
    belly.data.materials.append(accent_mat)
    objects.append(belly)

    return objects

# --- BUILD CHARACTER ---
char_type = char_data.get("type", "humanoid")
if char_type == "humanoid":
    all_objects = build_humanoid(char_data)
else:
    all_objects = build_quadruped(char_data)

# --- JOIN ALL MESHES ---
bpy.ops.object.select_all(action='DESELECT')
for obj in all_objects:
    if obj and obj.type == 'MESH':
        obj.select_set(True)

if all_objects:
    bpy.context.view_layer.objects.active = all_objects[0]
    bpy.ops.object.join()
    merged = bpy.context.active_object
    merged.name = char_name

    # Smooth shading
    bpy.ops.object.shade_smooth()

    # --- CREATE ARMATURE ---
    bpy.ops.object.armature_add(enter_editmode=True)
    armature_obj = bpy.context.active_object
    armature_obj.name = f"{char_name}_armature"
    armature = armature_obj.data
    armature.name = f"{char_name}_skeleton"

    # Remove default bone
    for bone in armature.edit_bones:
        armature.edit_bones.remove(bone)

    h = char_data.get("height", 1.0)

    if char_type == "humanoid":
        # Root
        root = armature.edit_bones.new("Root")
        root.head = (0, 0, 0)
        root.tail = (0, 0, h * 0.1)

        # Hips
        hips = armature.edit_bones.new("Hips")
        hips.head = (0, 0, h * 0.35)
        hips.tail = (0, 0, h * 0.45)
        hips.parent = root

        # Spine
        spine = armature.edit_bones.new("Spine")
        spine.head = (0, 0, h * 0.45)
        spine.tail = (0, 0, h * 0.55)
        spine.parent = hips

        # Chest
        chest = armature.edit_bones.new("Chest")
        chest.head = (0, 0, h * 0.55)
        chest.tail = (0, 0, h * 0.65)
        chest.parent = spine

        # Neck
        neck = armature.edit_bones.new("Neck")
        neck.head = (0, 0, h * 0.65)
        neck.tail = (0, 0, h * 0.72)
        neck.parent = chest

        # Head
        head_bone = armature.edit_bones.new("Head")
        head_bone.head = (0, 0, h * 0.72)
        head_bone.tail = (0, 0, h * 0.95)
        head_bone.parent = neck

        # Arms
        for side_name, sx in [("L", 1), ("R", -1)]:
            shoulder = armature.edit_bones.new(f"Shoulder_{side_name}")
            shoulder.head = (0, 0, h * 0.62)
            shoulder.tail = (sx * 0.12, 0, h * 0.6)
            shoulder.parent = chest

            upper_arm = armature.edit_bones.new(f"UpperArm_{side_name}")
            upper_arm.head = (sx * 0.12, 0, h * 0.6)
            upper_arm.tail = (sx * 0.22, 0, h * 0.48)
            upper_arm.parent = shoulder

            lower_arm = armature.edit_bones.new(f"LowerArm_{side_name}")
            lower_arm.head = (sx * 0.22, 0, h * 0.48)
            lower_arm.tail = (sx * 0.28, 0, h * 0.38)
            lower_arm.parent = upper_arm

            hand = armature.edit_bones.new(f"Hand_{side_name}")
            hand.head = (sx * 0.28, 0, h * 0.38)
            hand.tail = (sx * 0.32, 0, h * 0.35)
            hand.parent = lower_arm

        # Legs
        for side_name, sx in [("L", 1), ("R", -1)]:
            upper_leg = armature.edit_bones.new(f"UpperLeg_{side_name}")
            upper_leg.head = (sx * 0.08, 0, h * 0.35)
            upper_leg.tail = (sx * 0.08, 0, h * 0.2)
            upper_leg.parent = hips

            lower_leg = armature.edit_bones.new(f"LowerLeg_{side_name}")
            lower_leg.head = (sx * 0.08, 0, h * 0.2)
            lower_leg.tail = (sx * 0.08, 0, h * 0.06)
            lower_leg.parent = upper_leg

            foot = armature.edit_bones.new(f"Foot_{side_name}")
            foot.head = (sx * 0.08, 0, h * 0.06)
            foot.tail = (sx * 0.08, -0.06, h * 0.02)
            foot.parent = lower_leg

    else:  # Quadruped
        root = armature.edit_bones.new("Root")
        root.head = (0, 0, 0)
        root.tail = (0, 0, h * 0.1)

        body_bone = armature.edit_bones.new("Body")
        body_bone.head = (0, 0.1, h * 0.5)
        body_bone.tail = (0, -0.1, h * 0.5)
        body_bone.parent = root

        head_bone = armature.edit_bones.new("Head")
        head_bone.head = (0, -h * 0.5, h * 0.7)
        head_bone.tail = (0, -h * 0.8, h * 0.9)
        head_bone.parent = body_bone

        tail_bone = armature.edit_bones.new("Tail")
        tail_bone.head = (0, h * 0.3, h * 0.6)
        tail_bone.tail = (0, h * 0.5, h * 0.8)
        tail_bone.parent = body_bone

        # 4 legs
        for i, (lx, ly, name) in enumerate([
            (-0.1, -0.15, "FrontLeft"), (0.1, -0.15, "FrontRight"),
            (-0.1, 0.15, "BackLeft"), (0.1, 0.15, "BackRight")
        ]):
            leg_bone = armature.edit_bones.new(f"Leg_{name}")
            leg_bone.head = (lx, ly, h * 0.4)
            leg_bone.tail = (lx, ly, h * 0.05)
            leg_bone.parent = body_bone

    bpy.ops.object.mode_set(mode='OBJECT')

    # Parent mesh to armature with automatic weights
    merged.select_set(True)
    armature_obj.select_set(True)
    bpy.context.view_layer.objects.active = armature_obj
    bpy.ops.object.parent_set(type='ARMATURE_AUTO')

    # --- CREATE ANIMATIONS ---
    bpy.context.view_layer.objects.active = armature_obj
    armature_obj.select_set(True)

    for anim_name in animation_names:
        action = bpy.data.actions.new(name=anim_name)
        armature_obj.animation_data_create()
        armature_obj.animation_data.action = action

        # Simple keyframe animations (procedural)
        fps = 30
        duration = 2.0  # 2 seconds per animation
        frames = int(fps * duration)

        bpy.ops.object.mode_set(mode='POSE')

        for bone in armature_obj.pose.bones:
            bone.rotation_mode = 'XYZ'

            if anim_name in ["Idle", "IdleLookAround", "IdleBlink"]:
                # Gentle breathing / sway
                for f in range(0, frames, 5):
                    t = f / frames
                    bone.location.z = math.sin(t * math.pi * 4) * 0.005
                    if bone.name == "Head":
                        bone.rotation_euler.y = math.sin(t * math.pi * 2) * 0.05
                    bone.keyframe_insert(data_path="location", frame=f + 1)
                    bone.keyframe_insert(data_path="rotation_euler", frame=f + 1)

            elif anim_name in ["Walk", "Run"]:
                speed = 2.0 if anim_name == "Run" else 1.0
                amp = 0.15 if anim_name == "Run" else 0.08
                for f in range(0, frames, 3):
                    t = f / frames
                    if "Leg" in bone.name or "UpperLeg" in bone.name or "LowerLeg" in bone.name:
                        side = 1 if "L" in bone.name or "Left" in bone.name else -1
                        bone.rotation_euler.x = math.sin(t * math.pi * 4 * speed) * amp * side
                    if "Arm" in bone.name or "UpperArm" in bone.name:
                        side = 1 if "L" in bone.name else -1
                        bone.rotation_euler.x = -math.sin(t * math.pi * 4 * speed) * amp * 0.5 * side
                    if bone.name in ["Hips", "Body"]:
                        bone.location.z = abs(math.sin(t * math.pi * 4 * speed)) * 0.02
                    bone.keyframe_insert(data_path="rotation_euler", frame=f + 1)
                    bone.keyframe_insert(data_path="location", frame=f + 1)

            elif anim_name == "Jump":
                for f in range(0, frames, 3):
                    t = f / frames
                    if bone.name in ["Root", "Hips", "Body"]:
                        bone.location.z = math.sin(t * math.pi) * 0.3
                    if "Arm" in bone.name:
                        bone.rotation_euler.x = -math.sin(t * math.pi) * 0.5
                    bone.keyframe_insert(data_path="location", frame=f + 1)
                    bone.keyframe_insert(data_path="rotation_euler", frame=f + 1)

            elif anim_name == "Dance":
                for f in range(0, frames, 2):
                    t = f / frames
                    if bone.name in ["Hips", "Body"]:
                        bone.rotation_euler.y = math.sin(t * math.pi * 6) * 0.2
                        bone.location.z = abs(math.sin(t * math.pi * 6)) * 0.03
                    if "Arm" in bone.name:
                        bone.rotation_euler.z = math.sin(t * math.pi * 6 + 1) * 0.5
                    bone.keyframe_insert(data_path="rotation_euler", frame=f + 1)
                    bone.keyframe_insert(data_path="location", frame=f + 1)

            elif anim_name in ["Happy", "Excited"]:
                for f in range(0, frames, 3):
                    t = f / frames
                    if bone.name in ["Root", "Hips", "Body"]:
                        bone.location.z = abs(math.sin(t * math.pi * 8)) * 0.04
                    if bone.name == "Head":
                        bone.rotation_euler.x = math.sin(t * math.pi * 4) * 0.1
                    bone.keyframe_insert(data_path="location", frame=f + 1)
                    bone.keyframe_insert(data_path="rotation_euler", frame=f + 1)

            elif anim_name in ["Sad"]:
                for f in range(0, frames, 5):
                    t = f / frames
                    if bone.name == "Head":
                        bone.rotation_euler.x = 0.15 + math.sin(t * math.pi * 2) * 0.03
                    if bone.name in ["Hips", "Body"]:
                        bone.location.z = -0.02
                    bone.keyframe_insert(data_path="rotation_euler", frame=f + 1)
                    bone.keyframe_insert(data_path="location", frame=f + 1)

            elif anim_name == "Wave":
                for f in range(0, frames, 3):
                    t = f / frames
                    if bone.name in ["UpperArm_R", "Hand_R"]:
                        bone.rotation_euler.z = -1.2 + math.sin(t * math.pi * 6) * 0.3
                    bone.keyframe_insert(data_path="rotation_euler", frame=f + 1)

            elif anim_name == "Sleep":
                for f in range(0, frames, 6):
                    t = f / frames
                    if bone.name in ["Root", "Hips", "Body"]:
                        bone.location.z = math.sin(t * math.pi * 2) * 0.01
                    if bone.name == "Head":
                        bone.rotation_euler.x = 0.2
                    bone.keyframe_insert(data_path="location", frame=f + 1)
                    bone.keyframe_insert(data_path="rotation_euler", frame=f + 1)

            else:
                # Generic subtle animation for any undefined animation
                for f in range(0, frames, 5):
                    t = f / frames
                    bone.rotation_euler.x = math.sin(t * math.pi * 2) * 0.02
                    bone.location.z = math.sin(t * math.pi * 4) * 0.003
                    bone.keyframe_insert(data_path="rotation_euler", frame=f + 1)
                    bone.keyframe_insert(data_path="location", frame=f + 1)

        bpy.ops.object.mode_set(mode='OBJECT')

    # Push all actions to NLA tracks so they export
    if armature_obj.animation_data:
        for action in bpy.data.actions:
            track = armature_obj.animation_data.nla_tracks.new()
            track.name = action.name
            strip = track.strips.new(action.name, 1, action)

    # --- EXPORT GLB ---
    bpy.ops.export_scene.gltf(
        filepath=output_path,
        export_format='GLB',
        export_animations=True,
        export_skins=True,
        export_materials='EXPORT',
        export_colors=True,
        export_apply=True,
    )

    print(f"Exported {char_name} to {output_path}")
    print(f"  Meshes: {len([o for o in bpy.data.objects if o.type == 'MESH'])}")
    print(f"  Bones: {len(armature.bones)}")
    print(f"  Animations: {len(bpy.data.actions)}")
'''


@app.function(
    image=blender_image,
    timeout=600,
    cpu=4,
    memory=8192,
)
def generate_character(char_name: str, char_data: dict, animations: list) -> bytes:
    """Generate a single 3D character using Blender."""
    import subprocess
    import json
    import tempfile

    output_path = f"/tmp/{char_name}.glb"
    script_path = "/tmp/blender_build.py"

    # Write Blender script
    with open(script_path, "w") as f:
        f.write(BLENDER_SCRIPT)

    char_json = json.dumps(char_data)
    anim_json = json.dumps(animations)

    cmd = [
        "blender", "--background", "--python", script_path,
        "--", char_name, char_json, anim_json, output_path
    ]

    env = os.environ.copy()

    # Clean up stale Xvfb lock files and start virtual display
    import random
    display_num = random.randint(50, 200)
    lock_file = f"/tmp/.X{display_num}-lock"
    if os.path.exists(lock_file):
        os.remove(lock_file)
    socket_file = f"/tmp/.X11-unix/X{display_num}"
    if os.path.exists(socket_file):
        os.remove(socket_file)

    env["DISPLAY"] = f":{display_num}"
    xvfb = subprocess.Popen(
        ["Xvfb", f":{display_num}", "-screen", "0", "1024x768x24"],
        stdout=subprocess.DEVNULL, stderr=subprocess.DEVNULL
    )
    import time
    time.sleep(1)  # Give Xvfb time to start

    result = subprocess.run(cmd, capture_output=True, text=True, env=env, timeout=300)
    xvfb.terminate()
    print(f"STDOUT: {result.stdout[-2000:]}")
    if result.returncode != 0:
        print(f"STDERR: {result.stderr[-2000:]}")

    if os.path.exists(output_path):
        with open(output_path, "rb") as f:
            return f.read()
    else:
        raise RuntimeError(f"Failed to generate {char_name}: {result.stderr[-500:]}")


@app.local_entrypoint()
def main():
    """Generate all 9 characters and upload to iDrive e2."""
    import os

    output_dir = os.path.join(os.path.dirname(__file__), "..", "Assets", "Models", "Characters")
    os.makedirs(output_dir, exist_ok=True)

    results = {}

    for char_name, char_data in CHARACTERS.items():
        # Determine animations
        if char_data["type"] == "quadruped":
            anims = list(PET_ANIMATIONS)
            if char_name == "cat":
                anims.extend(CAT_EXTRA)
            elif char_name == "dog":
                anims.extend(DOG_EXTRA)
            elif char_name == "bunny":
                anims.extend(BUNNY_EXTRA)
        else:
            anims = list(HUMANOID_ANIMATIONS)

        print(f"\n{'='*60}")
        print(f"Generating {char_name} ({char_data['type']}) with {len(anims)} animations...")
        print(f"{'='*60}")

        try:
            glb_data = generate_character.remote(char_name, char_data, anims)

            # Save locally
            local_path = os.path.join(output_dir, f"{char_name}.glb")
            with open(local_path, "wb") as f:
                f.write(glb_data)

            size_kb = len(glb_data) / 1024
            print(f"  Saved: {local_path} ({size_kb:.1f} KB)")

            # Upload to iDrive e2
            s3_key = f"emersyn-big-day/models/characters/{char_name}.glb"
            try:
                upload_to_s3(local_path, s3_key)
                print(f"  Uploaded to S3: {s3_key}")
            except Exception as e:
                print(f"  S3 upload failed: {e}")

            results[char_name] = {
                "size_kb": size_kb,
                "animations": len(anims),
                "type": char_data["type"],
                "path": local_path
            }

        except Exception as e:
            print(f"  ERROR generating {char_name}: {e}")
            results[char_name] = {"error": str(e)}

    # Summary
    print(f"\n{'='*60}")
    print("GENERATION SUMMARY")
    print(f"{'='*60}")
    total_size = 0
    success_count = 0
    for name, info in results.items():
        if "error" in info:
            print(f"  {name}: FAILED - {info['error'][:80]}")
        else:
            print(f"  {name}: {info['size_kb']:.1f}KB, {info['animations']} anims ({info['type']})")
            total_size += info["size_kb"]
            success_count += 1

    print(f"\nTotal: {success_count}/{len(CHARACTERS)} characters, {total_size:.1f}KB total")
