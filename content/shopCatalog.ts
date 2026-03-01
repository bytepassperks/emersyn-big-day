import { ShopItem, ShopCategory } from '@/lib/types';

export const shopCatalog: ShopItem[] = [
  // ========== BAGS (15 items) ==========
  { id: 'bag_cat_pink', name: 'Pink Cat Bag', category: 'bags', price: 25, emoji: '🐱', description: 'A cute pink cat-shaped bag', rarity: 'common', unlockLevel: 1 },
  { id: 'bag_cat_purple', name: 'Purple Cat Bag', category: 'bags', price: 30, emoji: '🐱', description: 'A sparkly purple cat bag', rarity: 'common', unlockLevel: 1 },
  { id: 'bag_rainbow', name: 'Rainbow Bag', category: 'bags', price: 35, emoji: '🌈', description: 'All the colors of the rainbow!', rarity: 'common', unlockLevel: 2 },
  { id: 'bag_star', name: 'Star Bag', category: 'bags', price: 40, emoji: '⭐', description: 'A shining star backpack', rarity: 'rare', unlockLevel: 2 },
  { id: 'bag_unicorn_mini', name: 'Unicorn Mini Bag', category: 'bags', price: 50, emoji: '🦄', description: 'A tiny unicorn crossbody bag', rarity: 'rare', unlockLevel: 3 },
  { id: 'bag_bunny', name: 'Bunny Bag', category: 'bags', price: 30, emoji: '🐰', description: 'A fluffy white bunny bag', rarity: 'common', unlockLevel: 1 },
  { id: 'bag_bear', name: 'Bear Bag', category: 'bags', price: 28, emoji: '🧸', description: 'A teddy bear backpack', rarity: 'common', unlockLevel: 1 },
  { id: 'bag_flower', name: 'Flower Bag', category: 'bags', price: 35, emoji: '🌸', description: 'A beautiful flower bag', rarity: 'common', unlockLevel: 2 },
  { id: 'bag_butterfly', name: 'Butterfly Bag', category: 'bags', price: 45, emoji: '🦋', description: 'A sparkling butterfly bag', rarity: 'rare', unlockLevel: 3 },
  { id: 'bag_heart', name: 'Heart Bag', category: 'bags', price: 32, emoji: '💖', description: 'A heart-shaped bag', rarity: 'common', unlockLevel: 1 },
  { id: 'bag_school_cool', name: 'Cool School Bag', category: 'bags', price: 55, emoji: '🎒', description: 'The coolest school bag ever!', rarity: 'rare', unlockLevel: 4 },
  { id: 'bag_glitter', name: 'Glitter Bag', category: 'bags', price: 60, emoji: '✨', description: 'A bag covered in glitter', rarity: 'epic', unlockLevel: 5 },
  { id: 'bag_panda', name: 'Panda Bag', category: 'bags', price: 38, emoji: '🐼', description: 'An adorable panda bag', rarity: 'common', unlockLevel: 2 },
  { id: 'bag_princess', name: 'Princess Bag', category: 'bags', price: 75, emoji: '👑', description: 'A royal princess bag', rarity: 'epic', unlockLevel: 6 },
  { id: 'bag_cloud', name: 'Cloud Bag', category: 'bags', price: 42, emoji: '☁️', description: 'A soft fluffy cloud bag', rarity: 'rare', unlockLevel: 3 },

  // ========== UNICORN ITEMS (12 items) ==========
  { id: 'uni_headband', name: 'Unicorn Headband', category: 'unicorn', price: 20, emoji: '🦄', description: 'A sparkly unicorn horn headband', rarity: 'common', unlockLevel: 1 },
  { id: 'uni_plushie', name: 'Unicorn Plushie', category: 'unicorn', price: 45, emoji: '🧸', description: 'A cuddly unicorn stuffed animal', rarity: 'rare', unlockLevel: 2 },
  { id: 'uni_pillow', name: 'Unicorn Pillow', category: 'unicorn', price: 35, emoji: '💤', description: 'Dream on a unicorn pillow', rarity: 'common', unlockLevel: 2 },
  { id: 'uni_lamp', name: 'Unicorn Night Light', category: 'unicorn', price: 55, emoji: '🔮', description: 'A glowing unicorn night light', rarity: 'rare', unlockLevel: 3 },
  { id: 'uni_blanket', name: 'Unicorn Blanket', category: 'unicorn', price: 40, emoji: '🌈', description: 'A rainbow unicorn blanket', rarity: 'common', unlockLevel: 2 },
  { id: 'uni_slippers', name: 'Unicorn Slippers', category: 'unicorn', price: 30, emoji: '🥿', description: 'Cozy unicorn slippers', rarity: 'common', unlockLevel: 1 },
  { id: 'uni_diary', name: 'Unicorn Diary', category: 'unicorn', price: 25, emoji: '📔', description: 'A secret unicorn diary', rarity: 'common', unlockLevel: 1 },
  { id: 'uni_wings', name: 'Unicorn Wings', category: 'unicorn', price: 80, emoji: '🪽', description: 'Magical unicorn wings!', rarity: 'epic', unlockLevel: 5 },
  { id: 'uni_crown', name: 'Unicorn Crown', category: 'unicorn', price: 90, emoji: '👑', description: 'A majestic unicorn crown', rarity: 'legendary', unlockLevel: 7 },
  { id: 'uni_stickers', name: 'Unicorn Sticker Pack', category: 'unicorn', price: 15, emoji: '🏷️', description: 'Sparkly unicorn stickers', rarity: 'common', unlockLevel: 1 },
  { id: 'uni_mug', name: 'Unicorn Mug', category: 'unicorn', price: 22, emoji: '☕', description: 'A cute unicorn mug', rarity: 'common', unlockLevel: 1 },
  { id: 'uni_umbrella', name: 'Unicorn Umbrella', category: 'unicorn', price: 38, emoji: '☂️', description: 'A rainbow unicorn umbrella', rarity: 'rare', unlockLevel: 3 },

  // ========== CLOTHES - TOPS (15 items) ==========
  { id: 'top_pink_tee', name: 'Pink T-Shirt', category: 'clothes_top', price: 15, emoji: '👚', description: 'A sweet pink t-shirt', rarity: 'common', unlockLevel: 1 },
  { id: 'top_rainbow_hoodie', name: 'Rainbow Hoodie', category: 'clothes_top', price: 35, emoji: '🌈', description: 'Cozy rainbow hoodie', rarity: 'rare', unlockLevel: 2 },
  { id: 'top_star_sweater', name: 'Star Sweater', category: 'clothes_top', price: 30, emoji: '⭐', description: 'A sparkly star sweater', rarity: 'common', unlockLevel: 2 },
  { id: 'top_cat_tee', name: 'Cat T-Shirt', category: 'clothes_top', price: 18, emoji: '🐱', description: 'A kitty face t-shirt', rarity: 'common', unlockLevel: 1 },
  { id: 'top_unicorn_tee', name: 'Unicorn T-Shirt', category: 'clothes_top', price: 20, emoji: '🦄', description: 'A magical unicorn shirt', rarity: 'common', unlockLevel: 1 },
  { id: 'top_flower_blouse', name: 'Flower Blouse', category: 'clothes_top', price: 25, emoji: '🌺', description: 'A pretty flower blouse', rarity: 'common', unlockLevel: 2 },
  { id: 'top_sparkle_vest', name: 'Sparkle Vest', category: 'clothes_top', price: 40, emoji: '✨', description: 'A glittery vest', rarity: 'rare', unlockLevel: 3 },
  { id: 'top_butterfly_cardigan', name: 'Butterfly Cardigan', category: 'clothes_top', price: 45, emoji: '🦋', description: 'Soft butterfly cardigan', rarity: 'rare', unlockLevel: 3 },
  { id: 'top_heart_sweater', name: 'Heart Sweater', category: 'clothes_top', price: 28, emoji: '💖', description: 'A heart pattern sweater', rarity: 'common', unlockLevel: 2 },
  { id: 'top_princess_cape', name: 'Princess Cape', category: 'clothes_top', price: 65, emoji: '👸', description: 'A royal princess cape', rarity: 'epic', unlockLevel: 5 },
  { id: 'top_sporty_jacket', name: 'Sporty Jacket', category: 'clothes_top', price: 35, emoji: '🏃', description: 'A cool sporty jacket', rarity: 'rare', unlockLevel: 3 },
  { id: 'top_denim_jacket', name: 'Denim Jacket', category: 'clothes_top', price: 38, emoji: '🧥', description: 'A stylish denim jacket', rarity: 'rare', unlockLevel: 3 },
  { id: 'top_polka_dot', name: 'Polka Dot Top', category: 'clothes_top', price: 22, emoji: '⚫', description: 'A cute polka dot top', rarity: 'common', unlockLevel: 1 },
  { id: 'top_karate_gi', name: 'Karate Gi Top', category: 'clothes_top', price: 50, emoji: '🥋', description: 'Official karate training top', rarity: 'rare', unlockLevel: 4 },
  { id: 'top_dance_crop', name: 'Dance Crop Top', category: 'clothes_top', price: 42, emoji: '💃', description: 'A sparkly dance crop top', rarity: 'rare', unlockLevel: 4 },

  // ========== CLOTHES - DRESSES (15 items) ==========
  { id: 'dress_default_pink', name: 'Pink Day Dress', category: 'clothes_dress', price: 0, emoji: '👗', description: 'Emersyn\'s everyday pink dress', rarity: 'common', unlockLevel: 1 },
  { id: 'dress_rainbow', name: 'Rainbow Dress', category: 'clothes_dress', price: 40, emoji: '🌈', description: 'A beautiful rainbow dress', rarity: 'rare', unlockLevel: 2 },
  { id: 'dress_princess_pink', name: 'Princess Pink Dress', category: 'clothes_dress', price: 60, emoji: '👸', description: 'A fluffy pink princess dress', rarity: 'epic', unlockLevel: 4 },
  { id: 'dress_fairy', name: 'Fairy Dress', category: 'clothes_dress', price: 55, emoji: '🧚', description: 'A magical fairy dress with wings', rarity: 'epic', unlockLevel: 4 },
  { id: 'dress_flower_garden', name: 'Flower Garden Dress', category: 'clothes_dress', price: 35, emoji: '🌸', description: 'A dress with flower patterns', rarity: 'common', unlockLevel: 2 },
  { id: 'dress_blue_sky', name: 'Blue Sky Dress', category: 'clothes_dress', price: 30, emoji: '☁️', description: 'Light blue like the sky', rarity: 'common', unlockLevel: 2 },
  { id: 'dress_party_sparkle', name: 'Party Sparkle Dress', category: 'clothes_dress', price: 70, emoji: '✨', description: 'The ultimate party dress!', rarity: 'epic', unlockLevel: 5 },
  { id: 'dress_tutu', name: 'Dance Tutu', category: 'clothes_dress', price: 45, emoji: '🩰', description: 'A ballet tutu for dancing', rarity: 'rare', unlockLevel: 3 },
  { id: 'dress_sundress_yellow', name: 'Yellow Sundress', category: 'clothes_dress', price: 28, emoji: '🌻', description: 'A cheerful yellow sundress', rarity: 'common', unlockLevel: 1 },
  { id: 'dress_polka_dot', name: 'Polka Dot Dress', category: 'clothes_dress', price: 32, emoji: '⚫', description: 'A fun polka dot dress', rarity: 'common', unlockLevel: 2 },
  { id: 'dress_mermaid', name: 'Mermaid Dress', category: 'clothes_dress', price: 85, emoji: '🧜', description: 'A shimmery mermaid dress', rarity: 'legendary', unlockLevel: 7 },
  { id: 'dress_winter_cozy', name: 'Winter Cozy Dress', category: 'clothes_dress', price: 38, emoji: '❄️', description: 'Warm and cozy winter dress', rarity: 'rare', unlockLevel: 3 },
  { id: 'dress_pajama', name: 'Pajama Dress', category: 'clothes_dress', price: 20, emoji: '😴', description: 'Soft pajama nightie', rarity: 'common', unlockLevel: 1 },
  { id: 'dress_festival', name: 'Festival Dress', category: 'clothes_dress', price: 65, emoji: '🎆', description: 'A colorful festival dress', rarity: 'epic', unlockLevel: 5 },
  { id: 'dress_unicorn_gala', name: 'Unicorn Gala Dress', category: 'clothes_dress', price: 100, emoji: '🦄', description: 'The most magical dress ever!', rarity: 'legendary', unlockLevel: 8 },

  // ========== SHOES (12 items) ==========
  { id: 'shoes_default_white', name: 'White Sneakers', category: 'shoes', price: 0, emoji: '👟', description: 'Comfy white sneakers', rarity: 'common', unlockLevel: 1 },
  { id: 'shoes_pink_ballet', name: 'Pink Ballet Shoes', category: 'shoes', price: 25, emoji: '🩰', description: 'Ballet dance shoes', rarity: 'common', unlockLevel: 1 },
  { id: 'shoes_rainbow_boots', name: 'Rainbow Boots', category: 'shoes', price: 40, emoji: '🌈', description: 'Colorful rain boots', rarity: 'rare', unlockLevel: 2 },
  { id: 'shoes_sparkle_sandals', name: 'Sparkle Sandals', category: 'shoes', price: 35, emoji: '✨', description: 'Glittery sandals', rarity: 'rare', unlockLevel: 2 },
  { id: 'shoes_princess_heels', name: 'Princess Heels', category: 'shoes', price: 55, emoji: '👠', description: 'Tiny princess heels', rarity: 'epic', unlockLevel: 4 },
  { id: 'shoes_cat_slippers', name: 'Cat Slippers', category: 'shoes', price: 20, emoji: '🐱', description: 'Kitty cat house slippers', rarity: 'common', unlockLevel: 1 },
  { id: 'shoes_sport_running', name: 'Running Shoes', category: 'shoes', price: 30, emoji: '🏃', description: 'Fast running shoes', rarity: 'common', unlockLevel: 2 },
  { id: 'shoes_flower_sandals', name: 'Flower Sandals', category: 'shoes', price: 28, emoji: '🌺', description: 'Pretty flower sandals', rarity: 'common', unlockLevel: 2 },
  { id: 'shoes_skating', name: 'Skating Shoes', category: 'shoes', price: 45, emoji: '⛸️', description: 'Cool skating shoes', rarity: 'rare', unlockLevel: 3 },
  { id: 'shoes_cowgirl_boots', name: 'Cowgirl Boots', category: 'shoes', price: 42, emoji: '🤠', description: 'Cute cowgirl boots', rarity: 'rare', unlockLevel: 3 },
  { id: 'shoes_glass_slipper', name: 'Glass Slipper', category: 'shoes', price: 95, emoji: '💎', description: 'A magical glass slipper!', rarity: 'legendary', unlockLevel: 8 },
  { id: 'shoes_karate_shoes', name: 'Karate Shoes', category: 'shoes', price: 35, emoji: '🥋', description: 'Dojo training shoes', rarity: 'rare', unlockLevel: 3 },

  // ========== HAIR ACCESSORIES (12 items) ==========
  { id: 'hair_pink_bow', name: 'Pink Bow', category: 'hair', price: 10, emoji: '🎀', description: 'A sweet pink bow', rarity: 'common', unlockLevel: 1 },
  { id: 'hair_rainbow_clip', name: 'Rainbow Clip', category: 'hair', price: 15, emoji: '🌈', description: 'A colorful hair clip', rarity: 'common', unlockLevel: 1 },
  { id: 'hair_flower_crown', name: 'Flower Crown', category: 'hair', price: 30, emoji: '🌸', description: 'A beautiful flower crown', rarity: 'rare', unlockLevel: 2 },
  { id: 'hair_star_pins', name: 'Star Pins', category: 'hair', price: 18, emoji: '⭐', description: 'Sparkly star hair pins', rarity: 'common', unlockLevel: 1 },
  { id: 'hair_butterfly_clip', name: 'Butterfly Clip', category: 'hair', price: 22, emoji: '🦋', description: 'A delicate butterfly clip', rarity: 'common', unlockLevel: 2 },
  { id: 'hair_cat_ears', name: 'Cat Ears Headband', category: 'hair', price: 25, emoji: '🐱', description: 'Cute cat ears!', rarity: 'common', unlockLevel: 1 },
  { id: 'hair_tiara', name: 'Princess Tiara', category: 'hair', price: 60, emoji: '👑', description: 'A sparkling tiara', rarity: 'epic', unlockLevel: 5 },
  { id: 'hair_scrunchie_set', name: 'Scrunchie Set', category: 'hair', price: 12, emoji: '🔵', description: 'Colorful scrunchies', rarity: 'common', unlockLevel: 1 },
  { id: 'hair_heart_pins', name: 'Heart Pins', category: 'hair', price: 16, emoji: '💖', description: 'Heart-shaped hair pins', rarity: 'common', unlockLevel: 1 },
  { id: 'hair_ribbon_long', name: 'Long Ribbon', category: 'hair', price: 14, emoji: '🎗️', description: 'A flowing silk ribbon', rarity: 'common', unlockLevel: 1 },
  { id: 'hair_glitter_spray', name: 'Glitter Hair Spray', category: 'hair', price: 35, emoji: '✨', description: 'Make your hair sparkle!', rarity: 'rare', unlockLevel: 3 },
  { id: 'hair_unicorn_clip', name: 'Unicorn Horn Clip', category: 'hair', price: 28, emoji: '🦄', description: 'A magical unicorn horn clip', rarity: 'rare', unlockLevel: 2 },

  // ========== ACCESSORIES (12 items) ==========
  { id: 'acc_bracelet_friendship', name: 'Friendship Bracelet', category: 'accessories', price: 12, emoji: '📿', description: 'A colorful friendship bracelet', rarity: 'common', unlockLevel: 1 },
  { id: 'acc_necklace_heart', name: 'Heart Necklace', category: 'accessories', price: 25, emoji: '💖', description: 'A shiny heart necklace', rarity: 'common', unlockLevel: 1 },
  { id: 'acc_sunglasses_star', name: 'Star Sunglasses', category: 'accessories', price: 20, emoji: '🕶️', description: 'Star-shaped sunglasses', rarity: 'common', unlockLevel: 1 },
  { id: 'acc_watch_flower', name: 'Flower Watch', category: 'accessories', price: 30, emoji: '⌚', description: 'A cute flower watch', rarity: 'rare', unlockLevel: 2 },
  { id: 'acc_ring_rainbow', name: 'Rainbow Ring', category: 'accessories', price: 18, emoji: '💍', description: 'A tiny rainbow ring', rarity: 'common', unlockLevel: 1 },
  { id: 'acc_earrings_star', name: 'Star Earrings', category: 'accessories', price: 22, emoji: '⭐', description: 'Clip-on star earrings', rarity: 'common', unlockLevel: 2 },
  { id: 'acc_scarf_rainbow', name: 'Rainbow Scarf', category: 'accessories', price: 28, emoji: '🧣', description: 'A warm rainbow scarf', rarity: 'common', unlockLevel: 2 },
  { id: 'acc_wings_fairy', name: 'Fairy Wings', category: 'accessories', price: 50, emoji: '🧚', description: 'Sparkling fairy wings', rarity: 'epic', unlockLevel: 4 },
  { id: 'acc_crown_queen', name: 'Queen Crown', category: 'accessories', price: 85, emoji: '👑', description: 'A golden queen crown', rarity: 'legendary', unlockLevel: 7 },
  { id: 'acc_microphone', name: 'Star Microphone', category: 'accessories', price: 40, emoji: '🎤', description: 'For the weekend shows!', rarity: 'rare', unlockLevel: 3 },
  { id: 'acc_magic_wand', name: 'Magic Wand', category: 'accessories', price: 55, emoji: '🪄', description: 'A sparkly magic wand', rarity: 'epic', unlockLevel: 5 },
  { id: 'acc_cat_ear_headset', name: 'Cat Ear Headset', category: 'accessories', price: 45, emoji: '🎧', description: 'Headphones with cat ears', rarity: 'rare', unlockLevel: 3 },

  // ========== PLAY MAKEUP (8 items) ==========
  { id: 'makeup_lip_gloss_pink', name: 'Pink Lip Gloss', category: 'makeup', price: 15, emoji: '💋', description: 'Sparkly pink lip gloss', rarity: 'common', unlockLevel: 1 },
  { id: 'makeup_face_glitter', name: 'Face Glitter', category: 'makeup', price: 20, emoji: '✨', description: 'Safe sparkly face glitter', rarity: 'common', unlockLevel: 2 },
  { id: 'makeup_nail_stickers', name: 'Nail Stickers', category: 'makeup', price: 12, emoji: '💅', description: 'Cute press-on nail stickers', rarity: 'common', unlockLevel: 1 },
  { id: 'makeup_face_paint_cat', name: 'Cat Face Paint', category: 'makeup', price: 18, emoji: '🐱', description: 'Paint whiskers on your face!', rarity: 'common', unlockLevel: 1 },
  { id: 'makeup_face_paint_butterfly', name: 'Butterfly Face Paint', category: 'makeup', price: 22, emoji: '🦋', description: 'Pretty butterfly face design', rarity: 'common', unlockLevel: 2 },
  { id: 'makeup_blush_sparkle', name: 'Sparkle Blush', category: 'makeup', price: 18, emoji: '🌸', description: 'Rosy sparkle blush', rarity: 'common', unlockLevel: 2 },
  { id: 'makeup_eye_stickers', name: 'Eye Gem Stickers', category: 'makeup', price: 25, emoji: '💎', description: 'Stick-on eye gems', rarity: 'rare', unlockLevel: 3 },
  { id: 'makeup_temp_tattoo', name: 'Temporary Tattoos', category: 'makeup', price: 15, emoji: '🌟', description: 'Fun temporary tattoos', rarity: 'common', unlockLevel: 1 },

  // ========== TOYS (10 items) ==========
  { id: 'toy_teddy_bear', name: 'Teddy Bear', category: 'toys', price: 30, emoji: '🧸', description: 'A cuddly teddy bear friend', rarity: 'common', unlockLevel: 1 },
  { id: 'toy_fidget_pop', name: 'Pop It Fidget', category: 'toys', price: 15, emoji: '🔵', description: 'Satisfying pop it toy', rarity: 'common', unlockLevel: 1 },
  { id: 'toy_mini_pet_cat', name: 'Mini Pet Cat', category: 'toys', price: 45, emoji: '🐱', description: 'A tiny virtual pet cat', rarity: 'rare', unlockLevel: 3 },
  { id: 'toy_mini_pet_bunny', name: 'Mini Pet Bunny', category: 'toys', price: 45, emoji: '🐰', description: 'A tiny virtual pet bunny', rarity: 'rare', unlockLevel: 3 },
  { id: 'toy_doll_house_set', name: 'Doll House Set', category: 'toys', price: 70, emoji: '🏠', description: 'A miniature doll house', rarity: 'epic', unlockLevel: 5 },
  { id: 'toy_puzzle_box', name: 'Puzzle Box', category: 'toys', price: 20, emoji: '🧩', description: 'A fun puzzle box', rarity: 'common', unlockLevel: 1 },
  { id: 'toy_squishy_set', name: 'Squishy Set', category: 'toys', price: 18, emoji: '🍩', description: 'Squishy food toys', rarity: 'common', unlockLevel: 1 },
  { id: 'toy_kaleidoscope', name: 'Kaleidoscope', category: 'toys', price: 25, emoji: '🔮', description: 'A magical kaleidoscope', rarity: 'common', unlockLevel: 2 },
  { id: 'toy_music_box', name: 'Music Box', category: 'toys', price: 55, emoji: '🎵', description: 'A tinkling music box', rarity: 'epic', unlockLevel: 4 },
  { id: 'toy_magic_8ball', name: 'Magic 8 Ball', category: 'toys', price: 22, emoji: '🎱', description: 'Ask it anything!', rarity: 'common', unlockLevel: 2 },

  // ========== BOOKS (8 items) ==========
  { id: 'book_coloring_animals', name: 'Animal Coloring Book', category: 'books', price: 15, emoji: '🎨', description: 'Color cute animals', rarity: 'common', unlockLevel: 1 },
  { id: 'book_coloring_princess', name: 'Princess Coloring Book', category: 'books', price: 18, emoji: '👸', description: 'Color princess scenes', rarity: 'common', unlockLevel: 1 },
  { id: 'book_coloring_unicorn', name: 'Unicorn Coloring Book', category: 'books', price: 18, emoji: '🦄', description: 'Color magical unicorns', rarity: 'common', unlockLevel: 2 },
  { id: 'book_sticker_fun', name: 'Sticker Fun Book', category: 'books', price: 20, emoji: '🏷️', description: 'A sticker activity book', rarity: 'common', unlockLevel: 1 },
  { id: 'book_drawing_basics', name: 'How to Draw Book', category: 'books', price: 22, emoji: '✏️', description: 'Learn to draw cute things', rarity: 'common', unlockLevel: 2 },
  { id: 'book_fairy_tales', name: 'Fairy Tales Book', category: 'books', price: 25, emoji: '📖', description: 'Beautiful fairy tales', rarity: 'rare', unlockLevel: 2 },
  { id: 'book_comic_cat', name: 'Cat Comics', category: 'books', price: 20, emoji: '🐱', description: 'Funny cat comic strips', rarity: 'common', unlockLevel: 1 },
  { id: 'book_space_adventure', name: 'Space Adventure Book', category: 'books', price: 28, emoji: '🚀', description: 'Explore outer space!', rarity: 'rare', unlockLevel: 3 },

  // ========== ROOM DECOR (12 items) ==========
  { id: 'decor_star_lamp', name: 'Star Night Lamp', category: 'room_decor', price: 30, emoji: '⭐', description: 'A glowing star lamp', rarity: 'common', unlockLevel: 1 },
  { id: 'decor_rainbow_rug', name: 'Rainbow Rug', category: 'room_decor', price: 35, emoji: '🌈', description: 'A colorful rainbow rug', rarity: 'common', unlockLevel: 2 },
  { id: 'decor_fairy_lights', name: 'Fairy Lights', category: 'room_decor', price: 25, emoji: '💡', description: 'Twinkling fairy lights', rarity: 'common', unlockLevel: 1 },
  { id: 'decor_flower_wall', name: 'Flower Wall Art', category: 'room_decor', price: 28, emoji: '🌸', description: 'A pretty flower painting', rarity: 'common', unlockLevel: 2 },
  { id: 'decor_cloud_shelf', name: 'Cloud Shelf', category: 'room_decor', price: 40, emoji: '☁️', description: 'A cloud-shaped wall shelf', rarity: 'rare', unlockLevel: 3 },
  { id: 'decor_cat_poster', name: 'Cat Poster', category: 'room_decor', price: 15, emoji: '🐱', description: 'A cute cat poster', rarity: 'common', unlockLevel: 1 },
  { id: 'decor_unicorn_poster', name: 'Unicorn Poster', category: 'room_decor', price: 18, emoji: '🦄', description: 'A magical unicorn poster', rarity: 'common', unlockLevel: 1 },
  { id: 'decor_princess_bed', name: 'Princess Bed Set', category: 'room_decor', price: 60, emoji: '🛏️', description: 'A royal princess bed set', rarity: 'epic', unlockLevel: 5 },
  { id: 'decor_butterfly_mobile', name: 'Butterfly Mobile', category: 'room_decor', price: 32, emoji: '🦋', description: 'A hanging butterfly mobile', rarity: 'rare', unlockLevel: 2 },
  { id: 'decor_music_player', name: 'Music Player', category: 'room_decor', price: 45, emoji: '🎵', description: 'Plays fun tunes!', rarity: 'rare', unlockLevel: 3 },
  { id: 'decor_photo_frame', name: 'Photo Frame', category: 'room_decor', price: 20, emoji: '🖼️', description: 'A pretty photo frame', rarity: 'common', unlockLevel: 1 },
  { id: 'decor_beanbag', name: 'Rainbow Beanbag', category: 'room_decor', price: 38, emoji: '🫘', description: 'A comfy rainbow beanbag', rarity: 'rare', unlockLevel: 3 },

  // ========== BUG SAFETY (8 items) ==========
  { id: 'bug_mosquito_net', name: 'Star Mosquito Net', category: 'bug_safety', price: 30, emoji: '🌟', description: 'Keep mosquitoes away at night', rarity: 'common', unlockLevel: 1 },
  { id: 'bug_kitty_fan', name: 'Kitty Fan', category: 'bug_safety', price: 25, emoji: '🐱', description: 'A cat-shaped fan to blow bugs away', rarity: 'common', unlockLevel: 1 },
  { id: 'bug_rainbow_curtain', name: 'Rainbow Curtain', category: 'bug_safety', price: 35, emoji: '🌈', description: 'Block bugs with style!', rarity: 'common', unlockLevel: 2 },
  { id: 'bug_night_light_safe', name: 'Safe Room Night Light', category: 'bug_safety', price: 28, emoji: '🔮', description: 'A gentle bug-repelling night light', rarity: 'common', unlockLevel: 1 },
  { id: 'bug_sticker_patch', name: 'Bug Patch Stickers', category: 'bug_safety', price: 10, emoji: '🏷️', description: 'Cute anti-bug stickers for your arm', rarity: 'common', unlockLevel: 1 },
  { id: 'bug_brave_teddy', name: 'Brave Teddy', category: 'bug_safety', price: 40, emoji: '🧸', description: 'A teddy that keeps bugs away', rarity: 'rare', unlockLevel: 2 },
  { id: 'bug_flower_spray', name: 'Flower Room Spray', category: 'bug_safety', price: 20, emoji: '🌺', description: 'Smells nice, bugs don\'t like it', rarity: 'common', unlockLevel: 1 },
  { id: 'bug_unicorn_net', name: 'Bug-Proof Unicorn Net', category: 'bug_safety', price: 55, emoji: '🦄', description: 'The ultimate magical bug net!', rarity: 'epic', unlockLevel: 4 },
];

export const getItemsByCategory = (category: ShopCategory): ShopItem[] =>
  shopCatalog.filter((item) => item.category === category);

export const getItemById = (id: string): ShopItem | undefined =>
  shopCatalog.find((item) => item.id === id);

export const getAffordableItems = (coins: number, level: number): ShopItem[] =>
  shopCatalog.filter((item) => item.price <= coins && item.unlockLevel <= level);

export const getCategoryEmoji: Record<ShopCategory, string> = {
  bags: '🎒',
  unicorn: '🦄',
  clothes_top: '👚',
  clothes_bottom: '👖',
  clothes_dress: '👗',
  shoes: '👟',
  accessories: '📿',
  hair: '🎀',
  makeup: '💄',
  toys: '🧸',
  books: '📚',
  room_decor: '🏠',
  food_ingredients: '🍕',
  bug_safety: '🛡️',
};

export const getCategoryName: Record<ShopCategory, string> = {
  bags: 'Bags',
  unicorn: 'Unicorn',
  clothes_top: 'Tops',
  clothes_bottom: 'Bottoms',
  clothes_dress: 'Dresses',
  shoes: 'Shoes',
  accessories: 'Accessories',
  hair: 'Hair',
  makeup: 'Play Makeup',
  toys: 'Toys',
  books: 'Books',
  room_decor: 'Room Decor',
  food_ingredients: 'Food',
  bug_safety: 'Bug Safety',
};
