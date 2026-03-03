export interface StickerInfo {
  id: string;
  name: string;
  emoji: string;
  category: string;
}

export const stickers: StickerInfo[] = [
  // Daily Routine
  { id: 'sticker_first_wake', name: 'Early Bird', emoji: '\u{1F426}', category: 'Daily Routine' },
  { id: 'sticker_brush_teeth', name: 'Sparkle Smile', emoji: '\u{1F601}', category: 'Daily Routine' },
  { id: 'sticker_first_meal', name: 'Chef Emersyn', emoji: '\u{1F469}\u200D\u{1F373}', category: 'Daily Routine' },
  { id: 'sticker_clean_room', name: 'Tidy Room', emoji: '\u{1F9F9}', category: 'Daily Routine' },
  { id: 'sticker_bath_time', name: 'Squeaky Clean', emoji: '\u{1F6C1}', category: 'Daily Routine' },
  { id: 'sticker_bedtime', name: 'Sweet Dreams', emoji: '\u{1F319}', category: 'Daily Routine' },
  { id: 'sticker_tidy_emersyn', name: 'Tidy Emersyn', emoji: '\u2728', category: 'Daily Routine' },

  // School
  { id: 'sticker_first_school', name: 'First Day!', emoji: '\u{1F392}', category: 'School' },
  { id: 'sticker_math_star', name: 'Math Whiz', emoji: '\u{1F522}', category: 'School' },
  { id: 'sticker_reading', name: 'Book Worm', emoji: '\u{1F4DA}', category: 'School' },
  { id: 'sticker_little_artist', name: 'Little Artist', emoji: '\u{1F3A8}', category: 'School' },
  { id: 'sticker_good_student', name: 'Good Student', emoji: '\u{1F4DD}', category: 'School' },
  { id: 'sticker_homework_5', name: 'Homework Hero', emoji: '\u270F\uFE0F', category: 'School' },
  { id: 'sticker_perfect_score', name: 'Perfect Score!', emoji: '\u{1F4AF}', category: 'School' },
  { id: 'sticker_brainiac', name: 'Brainiac', emoji: '\u{1F9E0}', category: 'School' },

  // Cooking
  { id: 'sticker_first_cook', name: 'First Recipe', emoji: '\u{1F373}', category: 'Cooking' },
  { id: 'sticker_pizza_master', name: 'Pizza Master', emoji: '\u{1F355}', category: 'Cooking' },
  { id: 'sticker_burger_stack', name: 'Burger Builder', emoji: '\u{1F354}', category: 'Cooking' },
  { id: 'sticker_dessert_queen', name: 'Dessert Queen', emoji: '\u{1F9C1}', category: 'Cooking' },
  { id: 'sticker_10_recipes', name: '10 Recipes!', emoji: '\u{1F4D6}', category: 'Cooking' },
  { id: 'sticker_all_recipes', name: 'Master Chef', emoji: '\u{1F451}', category: 'Cooking' },

  // Park & Outdoors
  { id: 'sticker_first_park', name: 'Park Visit', emoji: '\u{1F333}', category: 'Park' },
  { id: 'sticker_swing_master', name: 'Swing Master', emoji: '\u{1F3A0}', category: 'Park' },
  { id: 'sticker_cyclist', name: 'Cyclist', emoji: '\u{1F6B2}', category: 'Park' },
  { id: 'sticker_scooty_ride', name: 'Scooty Star', emoji: '\u{1F6F4}', category: 'Park' },
  { id: 'sticker_trampoline', name: 'Bounce King', emoji: '\u{1F938}', category: 'Park' },
  { id: 'sticker_trampoline_pro', name: 'Trampoline Pro', emoji: '\u{1F3C6}', category: 'Park' },

  // Arcade
  { id: 'sticker_first_game', name: 'Player One', emoji: '\u{1F3AE}', category: 'Arcade' },
  { id: 'sticker_scooty_master', name: 'Scooty Master', emoji: '\u{1F6F5}', category: 'Arcade' },
  { id: 'sticker_dance_star', name: 'Dance Star', emoji: '\u{1F483}', category: 'Arcade' },
  { id: 'sticker_karate_master', name: 'Karate Master', emoji: '\u{1F94B}', category: 'Arcade' },
  { id: 'sticker_high_score', name: 'High Score!', emoji: '\u{1F3C6}', category: 'Arcade' },
  { id: 'sticker_arcade_master', name: 'Arcade Master', emoji: '\u{1F451}', category: 'Arcade' },

  // Shopping
  { id: 'sticker_first_purchase', name: 'First Buy!', emoji: '\u{1F6D2}', category: 'Shopping' },
  { id: 'sticker_10_items', name: '10 Items!', emoji: '\u{1F381}', category: 'Shopping' },
  { id: 'sticker_full_outfit', name: 'Full Outfit', emoji: '\u{1F457}', category: 'Shopping' },

  // Brave
  { id: 'sticker_brave_badge', name: 'Brave Badge', emoji: '\u{1F3C5}', category: 'Brave' },
  { id: 'sticker_first_bug', name: 'First Bug!', emoji: '\u{1F41B}', category: 'Brave' },
  { id: 'sticker_bug_brave', name: 'Super Brave!', emoji: '\u{1F9B8}', category: 'Brave' },

  // Weekend
  { id: 'sticker_content_creator', name: 'Content Creator', emoji: '\u{1F4F8}', category: 'Weekend' },
  { id: 'sticker_makeup_artist', name: 'Makeup Artist', emoji: '\u{1F484}', category: 'Weekend' },
  { id: 'sticker_viral_reel', name: 'Viral Reel', emoji: '\u{1F3AC}', category: 'Weekend' },
  { id: 'sticker_glam_queen', name: 'Glam Queen', emoji: '\u{1F451}', category: 'Weekend' },
  { id: 'sticker_first_reel', name: 'First Reel!', emoji: '\u{1F4F1}', category: 'Weekend' },
  { id: 'sticker_100_likes', name: '100 Likes!', emoji: '\u2764\uFE0F', category: 'Weekend' },
  { id: 'sticker_popular', name: 'Popular Star', emoji: '\u{1F31F}', category: 'Weekend' },
];
