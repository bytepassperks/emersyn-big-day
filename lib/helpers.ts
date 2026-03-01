// Utility helpers for Emersyn's Big Day

export const clamp = (value: number, min: number, max: number): number =>
  Math.min(max, Math.max(min, value));

export const formatCoins = (coins: number): string => {
  if (coins >= 1000) return `₹${(coins / 1000).toFixed(1)}K`;
  return `₹${coins}`;
};

export const getTimeOfDayEmoji = (segment: string): string => {
  const emojiMap: Record<string, string> = {
    morning: '🌅',
    school: '🏫',
    lunch: '🍽️',
    nap: '😴',
    park: '🏞️',
    backHome: '🏠',
    homework: '📚',
    arcade: '🕹️',
    shopping: '🛍️',
    dinner: '🌙',
    bedtime: '🌜',
    weekendStudio: '🎬',
  };
  return emojiMap[segment] ?? '🌞';
};

export const getSegmentName = (segment: string): string => {
  const nameMap: Record<string, string> = {
    morning: 'Good Morning!',
    school: 'School Time',
    lunch: 'Lunch Break',
    nap: 'Nap Time',
    park: 'Park Fun!',
    backHome: 'Back Home',
    homework: 'Homework',
    arcade: 'Arcade Hub',
    shopping: 'Shopping',
    dinner: 'Dinner Time',
    bedtime: 'Bedtime',
    weekendStudio: 'Weekend Studio',
  };
  return nameMap[segment] ?? segment;
};

export const getRandomEncouragement = (): string => {
  const messages = [
    'Great job, Emersyn! ⭐',
    'You\'re amazing! 🌟',
    'Wonderful! Keep going! 💖',
    'Yay! Well done! 🎉',
    'Super star! ✨',
    'That was fantastic! 🌈',
    'You rock! 🎸',
    'Brilliant! 💎',
    'So proud of you! 🏆',
    'That was perfect! 👑',
  ];
  return messages[Math.floor(Math.random() * messages.length)];
};

export const getRandomNPCComment = (): { name: string; comment: string; emoji: string } => {
  const comments = [
    { name: 'Luna', comment: 'So cute! 💖', emoji: '🐱' },
    { name: 'Sparkle', comment: 'Amazing! ✨', emoji: '🦄' },
    { name: 'Daisy', comment: 'Love this! 🌸', emoji: '🐰' },
    { name: 'Bubbles', comment: 'Wow! So cool!', emoji: '🐠' },
    { name: 'Rainbow', comment: 'Beautiful! 🌈', emoji: '🦋' },
    { name: 'Sunny', comment: 'Super star! ⭐', emoji: '🌻' },
    { name: 'Cookie', comment: 'So pretty! 💕', emoji: '🐻' },
    { name: 'Twinkle', comment: 'Gorgeous! 💎', emoji: '⭐' },
    { name: 'Blossom', comment: 'Love love love! 💗', emoji: '🌷' },
    { name: 'Pepper', comment: 'You go girl! 🔥', emoji: '🐶' },
  ];
  return comments[Math.floor(Math.random() * comments.length)];
};

export const generateFakeLikes = (): number =>
  Math.floor(Math.random() * 50) + 10;

export const getBeltDisplayName = (belt: string): string => {
  return belt.charAt(0).toUpperCase() + belt.slice(1) + ' Belt';
};

export const getBeltEmoji = (belt: string): string => {
  const map: Record<string, string> = {
    white: '⬜',
    yellow: '🟨',
    orange: '🟧',
    green: '🟩',
    blue: '🟦',
    purple: '🟪',
    brown: '🟫',
    black: '⬛',
  };
  return map[belt] ?? '⬜';
};
