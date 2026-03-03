"""
Modal GPU AI Texture Generation for Emersyn's Big Day
Generates cute kawaii-style textures using Stable Diffusion XL on GPU.
"""
import modal
import io
import os

# Define the Modal app
app = modal.App("emersyn-textures")

# Image with diffusers + torch
sdxl_image = (
    modal.Image.debian_slim(python_version="3.11")
    .pip_install(
        "diffusers==0.30.0",
        "transformers==4.44.0",
        "accelerate==0.33.0",
        "safetensors==0.4.4",
        "torch==2.4.0",
        "Pillow==10.4.0",
    )
)

# Volume to store generated textures
vol = modal.Volume.from_name("emersyn-textures", create_if_missing=True)

TEXTURE_PROMPTS = {
    # Room textures
    "bedroom_wall": "cute kawaii pastel pink bedroom wallpaper, soft floral pattern, seamless texture, gentle colors, children room, high quality",
    "bedroom_floor": "light wooden floor texture, warm oak planks, seamless tileable, cute cozy bedroom, high quality",
    "kitchen_wall": "cute pastel yellow kitchen tiles, kawaii style, seamless pattern, warm cheerful, children game",
    "kitchen_floor": "checkered pastel mint and white kitchen floor tiles, seamless texture, cute style, high quality",
    "bathroom_wall": "cute pastel blue bathroom tiles, kawaii bubble pattern, seamless, soft colors, children game",
    "bathroom_floor": "white and light blue mosaic bathroom floor tiles, seamless texture, cute clean style",
    "park_grass": "lush green cartoon grass texture, cute kawaii style, seamless tileable, bright cheerful, game asset",
    "park_sky": "beautiful pastel blue sky with fluffy white clouds, kawaii style, soft gradient, children game background",
    "school_wall": "cute pastel green classroom wall, bulletin board pattern, kawaii school, seamless texture",
    "school_floor": "polished light gray school corridor floor, seamless texture, clean cute style",
    "arcade_wall": "colorful neon arcade wall, kawaii pixel art pattern, fun vibrant, seamless texture, children game",
    "arcade_floor": "dark carpet with colorful star pattern, arcade floor, seamless texture, fun vibrant",
    "studio_wall": "creative art studio wall, paint splashes, pastel rainbow, kawaii style, seamless texture",
    
    # Food textures
    "food_pizza": "cute kawaii pizza slice, chibi food art, pastel colors, game icon, transparent background, adorable",
    "food_cake": "cute kawaii birthday cake, chibi food art, pastel pink frosting, sprinkles, game icon, adorable",
    "food_cookie": "cute kawaii chocolate chip cookie, chibi food art, golden brown, game icon, adorable",
    "food_icecream": "cute kawaii ice cream cone, chibi food art, pastel rainbow scoops, game icon, adorable",
    "food_sushi": "cute kawaii sushi roll, chibi food art, happy face, game icon, adorable",
    "food_burger": "cute kawaii hamburger, chibi food art, sesame bun, game icon, adorable",
    "food_donut": "cute kawaii donut, chibi food art, pink frosting sprinkles, game icon, adorable",
    "food_cupcake": "cute kawaii cupcake, chibi food art, swirl frosting cherry top, game icon, adorable",
    "food_pancake": "cute kawaii pancake stack, chibi food art, butter syrup, game icon, adorable",
    "food_fruit_bowl": "cute kawaii fruit bowl, chibi food art, colorful fruits smiling, game icon, adorable",
    
    # Clothing textures
    "dress_princess": "cute kawaii princess dress, chibi fashion art, pastel pink sparkles, game icon, adorable",
    "dress_casual": "cute kawaii casual outfit, chibi fashion art, jeans and t-shirt, game icon, adorable",
    "dress_party": "cute kawaii party dress, chibi fashion art, glitter purple, game icon, adorable",
    "shoes_sneakers": "cute kawaii sneakers, chibi shoe art, pastel rainbow, game icon, adorable",
    "shoes_boots": "cute kawaii rain boots, chibi shoe art, yellow with ducks, game icon, adorable",
    "hat_crown": "cute kawaii princess crown, chibi accessory art, gold sparkle, game icon, adorable",
    "bag_backpack": "cute kawaii school backpack, chibi bag art, pastel with patches, game icon, adorable",
    "accessory_bow": "cute kawaii hair bow, chibi accessory art, big pink satin, game icon, adorable",
    
    # Furniture textures
    "furniture_bed": "cute kawaii bed, chibi furniture art, pink canopy princess bed, game asset, adorable",
    "furniture_desk": "cute kawaii study desk, chibi furniture art, pastel with books, game asset, adorable",
    "furniture_sofa": "cute kawaii sofa, chibi furniture art, fluffy pastel blue couch, game asset, adorable",
    "furniture_bookshelf": "cute kawaii bookshelf, chibi furniture art, colorful books, game asset, adorable",
    "furniture_lamp": "cute kawaii table lamp, chibi furniture art, star shaped, warm glow, game asset, adorable",
    "furniture_mirror": "cute kawaii vanity mirror, chibi furniture art, heart shaped frame, game asset, adorable",
    
    # Decor textures
    "decor_rug": "cute kawaii rainbow rug, round fluffy carpet, pastel colors, seamless texture, children room",
    "decor_curtain": "cute kawaii window curtain, pastel floral pattern, soft fabric texture, children room",
    "decor_poster": "cute kawaii motivational poster, pastel art print, rainbow stars, children room wall art",
    "decor_plant": "cute kawaii potted plant, chibi succulent art, happy face pot, game asset, adorable",
    "decor_teddy": "cute kawaii teddy bear, chibi toy art, fluffy brown bear, game asset, adorable",
    "decor_unicorn": "cute kawaii unicorn plush toy, chibi art, pastel rainbow mane, game asset, adorable",
}


@app.function(
    image=sdxl_image,
    gpu="T4",
    timeout=600,
    volumes={"/textures": vol},
)
def generate_texture(name: str, prompt: str, size: int = 512):
    """Generate a single texture using Stable Diffusion."""
    import torch
    from diffusers import StableDiffusionPipeline
    from PIL import Image

    output_path = f"/textures/{name}.png"
    
    # Check if already generated
    if os.path.exists(output_path):
        print(f"Texture {name} already exists, skipping")
        return name

    print(f"Generating texture: {name}")
    
    # Use SD 1.5 for efficiency (faster on T4, lower VRAM)
    pipe = StableDiffusionPipeline.from_pretrained(
        "runwayml/stable-diffusion-v1-5",
        torch_dtype=torch.float16,
        safety_checker=None,
    )
    pipe = pipe.to("cuda")
    pipe.enable_attention_slicing()
    
    # Generate
    image = pipe(
        prompt=prompt,
        negative_prompt="ugly, blurry, low quality, dark, scary, violent, realistic photo, nsfw",
        num_inference_steps=30,
        guidance_scale=7.5,
        width=size,
        height=size,
    ).images[0]
    
    image.save(output_path)
    vol.commit()
    print(f"Saved texture: {name} ({size}x{size})")
    return name


@app.function(
    image=sdxl_image,
    gpu="T4",
    timeout=1800,
    volumes={"/textures": vol},
)
def generate_all_textures():
    """Generate all textures in batch on a single GPU."""
    import torch
    from diffusers import StableDiffusionPipeline

    print("Loading Stable Diffusion pipeline...")
    pipe = StableDiffusionPipeline.from_pretrained(
        "runwayml/stable-diffusion-v1-5",
        torch_dtype=torch.float16,
        safety_checker=None,
    )
    pipe = pipe.to("cuda")
    pipe.enable_attention_slicing()

    generated = []
    total = len(TEXTURE_PROMPTS)
    
    for i, (name, prompt) in enumerate(TEXTURE_PROMPTS.items()):
        output_path = f"/textures/{name}.png"
        
        if os.path.exists(output_path):
            print(f"[{i+1}/{total}] Texture {name} already exists, skipping")
            generated.append(name)
            continue
        
        print(f"[{i+1}/{total}] Generating: {name}")
        
        image = pipe(
            prompt=prompt,
            negative_prompt="ugly, blurry, low quality, dark, scary, violent, realistic photo, nsfw",
            num_inference_steps=25,
            guidance_scale=7.5,
            width=512,
            height=512,
        ).images[0]
        
        image.save(output_path)
        generated.append(name)
        print(f"[{i+1}/{total}] Saved: {name}")
    
    vol.commit()
    print(f"\nDone! Generated {len(generated)} textures total.")
    return generated


@app.function(
    image=sdxl_image,
    volumes={"/textures": vol},
)
def download_texture(name: str) -> bytes:
    """Download a generated texture as bytes."""
    path = f"/textures/{name}.png"
    if not os.path.exists(path):
        raise FileNotFoundError(f"Texture {name} not found")
    with open(path, "rb") as f:
        return f.read()


@app.function(
    image=sdxl_image,
    volumes={"/textures": vol},
)
def list_textures() -> list:
    """List all generated textures."""
    vol.reload()
    textures = []
    if os.path.exists("/textures"):
        for f in os.listdir("/textures"):
            if f.endswith(".png"):
                textures.append(f.replace(".png", ""))
    return textures


@app.local_entrypoint()
def main():
    """Run texture generation from CLI."""
    print(f"Starting AI texture generation for Emersyn's Big Day")
    print(f"Total textures to generate: {len(TEXTURE_PROMPTS)}")
    print(f"Using Stable Diffusion v1.5 on Modal T4 GPU")
    print()
    
    # Generate all textures in a single GPU session (more efficient)
    result = generate_all_textures.remote()
    print(f"\nCompleted! Generated {len(result)} textures.")
    
    # Download textures to local assets folder
    output_dir = os.path.join(os.path.dirname(os.path.dirname(__file__)), "assets", "textures")
    os.makedirs(output_dir, exist_ok=True)
    
    for name in result:
        print(f"Downloading {name}...")
        data = download_texture.remote(name)
        with open(os.path.join(output_dir, f"{name}.png"), "wb") as f:
            f.write(data)
    
    print(f"\nAll textures saved to {output_dir}")
