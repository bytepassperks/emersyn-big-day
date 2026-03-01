import { Activity, BugEvent } from '@/lib/types';

export const activities: Activity[] = [
  // ========== MORNING ==========
  { id: 'wake_up', name: 'Wake Up!', emoji: '🌅', segment: 'morning', statDeltas: { energy: 5 }, coinReward: 2, starReward: 0, xpReward: 5, durationSeconds: 3 },
  { id: 'brush_teeth', name: 'Brush Teeth', emoji: '🪥', segment: 'morning', statDeltas: { cleanliness: 15 }, coinReward: 3, starReward: 0, xpReward: 5, durationSeconds: 5 },
  { id: 'wash_face', name: 'Wash Face', emoji: '🧼', segment: 'morning', statDeltas: { cleanliness: 10 }, coinReward: 2, starReward: 0, xpReward: 3, durationSeconds: 3 },
  { id: 'get_dressed', name: 'Get Dressed', emoji: '👗', segment: 'morning', statDeltas: { fun: 5 }, coinReward: 2, starReward: 0, xpReward: 3, durationSeconds: 3 },
  { id: 'breakfast', name: 'Cook Breakfast', emoji: '🍳', segment: 'morning', statDeltas: { hunger: 25, energy: 10 }, coinReward: 5, starReward: 1, xpReward: 10, durationSeconds: 10, miniGameRoute: '/cooking' },
  { id: 'make_bed', name: 'Make Bed', emoji: '🛏️', segment: 'morning', statDeltas: { cleanliness: 5 }, coinReward: 3, starReward: 0, xpReward: 5, durationSeconds: 3 },

  // ========== SCHOOL ==========
  { id: 'school_shapes', name: 'Shape Sorting', emoji: '🔷', segment: 'school', statDeltas: { fun: 10 }, coinReward: 8, starReward: 1, xpReward: 15, durationSeconds: 15, miniGameRoute: '/minigames/homework' },
  { id: 'school_counting', name: 'Counting Fun', emoji: '🔢', segment: 'school', statDeltas: { fun: 10 }, coinReward: 8, starReward: 1, xpReward: 15, durationSeconds: 15, miniGameRoute: '/minigames/homework' },
  { id: 'school_patterns', name: 'Pattern Match', emoji: '🎨', segment: 'school', statDeltas: { fun: 10 }, coinReward: 8, starReward: 1, xpReward: 15, durationSeconds: 15, miniGameRoute: '/minigames/homework' },
  { id: 'school_art', name: 'Art Class', emoji: '🖍️', segment: 'school', statDeltas: { fun: 15 }, coinReward: 10, starReward: 1, xpReward: 10, durationSeconds: 10 },

  // ========== LUNCH ==========
  { id: 'cook_lunch', name: 'Cook Lunch', emoji: '🍕', segment: 'lunch', statDeltas: { hunger: 30, energy: 5 }, coinReward: 10, starReward: 1, xpReward: 15, durationSeconds: 15, miniGameRoute: '/cooking' },
  { id: 'eat_snack', name: 'Eat Snack', emoji: '🍪', segment: 'lunch', statDeltas: { hunger: 15 }, coinReward: 3, starReward: 0, xpReward: 5, durationSeconds: 5 },

  // ========== NAP ==========
  { id: 'take_nap', name: 'Cozy Nap', emoji: '😴', segment: 'nap', statDeltas: { energy: 30 }, coinReward: 5, starReward: 0, xpReward: 5, durationSeconds: 8 },

  // ========== PARK ==========
  { id: 'park_trampoline', name: 'Trampoline', emoji: '🤸', segment: 'park', statDeltas: { fun: 20, energy: -10 }, coinReward: 8, starReward: 1, xpReward: 12, durationSeconds: 15, miniGameRoute: '/minigames/trampoline' },
  { id: 'park_scooty', name: 'Scooty Ride', emoji: '🛴', segment: 'park', statDeltas: { fun: 20, energy: -10 }, coinReward: 8, starReward: 1, xpReward: 12, durationSeconds: 15, miniGameRoute: '/minigames/endless-runner' },
  { id: 'park_skating', name: 'Skating', emoji: '⛸️', segment: 'park', statDeltas: { fun: 18, energy: -12 }, coinReward: 10, starReward: 1, xpReward: 15, durationSeconds: 15 },
  { id: 'park_slides', name: 'Slide Fun', emoji: '🎢', segment: 'park', statDeltas: { fun: 15, energy: -5 }, coinReward: 5, starReward: 0, xpReward: 8, durationSeconds: 8 },
  { id: 'park_swings', name: 'Swings', emoji: '🪂', segment: 'park', statDeltas: { fun: 12, energy: -3 }, coinReward: 4, starReward: 0, xpReward: 5, durationSeconds: 5 },

  // ========== BACK HOME ==========
  { id: 'remove_shoes', name: 'Remove Shoes', emoji: '👟', segment: 'backHome', statDeltas: {}, coinReward: 2, starReward: 0, xpReward: 3, durationSeconds: 2 },
  { id: 'wash_hands', name: 'Wash Hands', emoji: '🧼', segment: 'backHome', statDeltas: { cleanliness: 10 }, coinReward: 3, starReward: 0, xpReward: 5, durationSeconds: 3 },
  { id: 'wash_face_back', name: 'Wash Face', emoji: '💦', segment: 'backHome', statDeltas: { cleanliness: 8 }, coinReward: 2, starReward: 0, xpReward: 3, durationSeconds: 3 },
  { id: 'change_clothes', name: 'Change Clothes', emoji: '👚', segment: 'backHome', statDeltas: { cleanliness: 5 }, coinReward: 2, starReward: 0, xpReward: 3, durationSeconds: 3 },
  { id: 'drink_water', name: 'Drink Water', emoji: '💧', segment: 'backHome', statDeltas: { energy: 8 }, coinReward: 2, starReward: 0, xpReward: 3, durationSeconds: 2 },

  // ========== HOMEWORK ==========
  { id: 'homework_shapes', name: 'Shape Homework', emoji: '🔷', segment: 'homework', statDeltas: { fun: 5 }, coinReward: 10, starReward: 2, xpReward: 20, durationSeconds: 15, miniGameRoute: '/minigames/homework' },
  { id: 'homework_counting', name: 'Counting Homework', emoji: '🔢', segment: 'homework', statDeltas: { fun: 5 }, coinReward: 10, starReward: 2, xpReward: 20, durationSeconds: 15, miniGameRoute: '/minigames/homework' },
  { id: 'homework_coloring', name: 'Coloring Book', emoji: '🎨', segment: 'homework', statDeltas: { fun: 12 }, coinReward: 8, starReward: 1, xpReward: 10, durationSeconds: 10 },

  // ========== ARCADE ==========
  { id: 'arcade_runner', name: 'Scooty Dash', emoji: '🏃', segment: 'arcade', statDeltas: { fun: 25, energy: -5 }, coinReward: 15, starReward: 2, xpReward: 20, durationSeconds: 30, miniGameRoute: '/minigames/endless-runner' },
  { id: 'arcade_dance', name: 'Dance Party', emoji: '💃', segment: 'arcade', statDeltas: { fun: 25, energy: -5 }, coinReward: 15, starReward: 2, xpReward: 20, durationSeconds: 30, miniGameRoute: '/minigames/dance-party' },
  { id: 'arcade_karate', name: 'Karate Star', emoji: '🥋', segment: 'arcade', statDeltas: { fun: 25, energy: -8 }, coinReward: 15, starReward: 2, xpReward: 20, durationSeconds: 30, miniGameRoute: '/minigames/karate-star' },

  // ========== SHOPPING ==========
  { id: 'shop_visit', name: 'Visit Shop', emoji: '🛍️', segment: 'shopping', statDeltas: { fun: 10 }, coinReward: 0, starReward: 0, xpReward: 5, durationSeconds: 5, miniGameRoute: '/shop' },

  // ========== DINNER ==========
  { id: 'cook_dinner', name: 'Cook Dinner', emoji: '🍽️', segment: 'dinner', statDeltas: { hunger: 35, energy: 5 }, coinReward: 12, starReward: 1, xpReward: 15, durationSeconds: 15, miniGameRoute: '/cooking' },

  // ========== BEDTIME ==========
  { id: 'bath_time', name: 'Bath Time', emoji: '🛁', segment: 'bedtime', statDeltas: { cleanliness: 30 }, coinReward: 5, starReward: 0, xpReward: 5, durationSeconds: 8 },
  { id: 'pajamas', name: 'Put on Pajamas', emoji: '😴', segment: 'bedtime', statDeltas: {}, coinReward: 2, starReward: 0, xpReward: 3, durationSeconds: 3 },
  { id: 'bedtime_story', name: 'Bedtime Story', emoji: '📖', segment: 'bedtime', statDeltas: { fun: 10, energy: 5 }, coinReward: 5, starReward: 1, xpReward: 8, durationSeconds: 8 },
  { id: 'go_sleep', name: 'Go to Sleep', emoji: '💤', segment: 'bedtime', statDeltas: { energy: 50 }, coinReward: 5, starReward: 1, xpReward: 10, durationSeconds: 5 },

  // ========== WEEKEND STUDIO ==========
  { id: 'weekend_dressup', name: 'Dress Up', emoji: '👗', segment: 'weekendStudio', statDeltas: { fun: 15 }, coinReward: 5, starReward: 0, xpReward: 8, durationSeconds: 8 },
  { id: 'weekend_makeup', name: 'Play Makeup', emoji: '💄', segment: 'weekendStudio', statDeltas: { fun: 15 }, coinReward: 5, starReward: 0, xpReward: 8, durationSeconds: 8 },
  { id: 'weekend_record', name: 'Record Reel', emoji: '🎬', segment: 'weekendStudio', statDeltas: { fun: 20, popularity: 10 }, coinReward: 15, starReward: 2, xpReward: 20, durationSeconds: 15, miniGameRoute: '/studio' },
  { id: 'weekend_post', name: 'Post to Feed', emoji: '📱', segment: 'weekendStudio', statDeltas: { popularity: 15, fun: 10 }, coinReward: 10, starReward: 1, xpReward: 12, durationSeconds: 5 },
];

export const bugEvents: BugEvent[] = [
  {
    id: 'bug_mosquito_bedroom',
    type: 'mosquito',
    location: 'bedroom',
    actions: [
      { id: 'close_window', name: 'Close Window', emoji: '🪟', effectiveness: 80 },
      { id: 'turn_fan', name: 'Turn on Fan', emoji: '🌀', effectiveness: 60 },
      { id: 'use_net', name: 'Use Mosquito Net', emoji: '🥅', effectiveness: 95 },
      { id: 'shoo_away', name: 'Shoo Away', emoji: '👋', effectiveness: 40 },
    ],
  },
  {
    id: 'bug_fly_kitchen',
    type: 'fly',
    location: 'kitchen',
    actions: [
      { id: 'close_window_k', name: 'Close Window', emoji: '🪟', effectiveness: 70 },
      { id: 'cover_food', name: 'Cover Food', emoji: '🍽️', effectiveness: 85 },
      { id: 'shoo_fly', name: 'Shoo Fly', emoji: '👋', effectiveness: 50 },
      { id: 'clean_counter', name: 'Clean Counter', emoji: '🧹', effectiveness: 75 },
    ],
  },
  {
    id: 'bug_ant_kitchen',
    type: 'ant',
    location: 'kitchen',
    actions: [
      { id: 'clean_crumbs', name: 'Clean Crumbs', emoji: '🧹', effectiveness: 90 },
      { id: 'seal_food', name: 'Seal Food Boxes', emoji: '📦', effectiveness: 85 },
      { id: 'wipe_surface', name: 'Wipe Surface', emoji: '🧽', effectiveness: 70 },
    ],
  },
  {
    id: 'bug_mosquito_window',
    type: 'mosquito',
    location: 'window',
    actions: [
      { id: 'close_window_w', name: 'Close Window Fast!', emoji: '🪟', effectiveness: 90 },
      { id: 'put_net', name: 'Put Up Net', emoji: '🥅', effectiveness: 95 },
      { id: 'spray_room', name: 'Flower Room Spray', emoji: '🌺', effectiveness: 80 },
    ],
  },
];

export const getActivitiesBySegment = (segment: string): Activity[] =>
  activities.filter((a) => a.segment === segment);

export const getRandomBugEvent = (): BugEvent =>
  bugEvents[Math.floor(Math.random() * bugEvents.length)];
