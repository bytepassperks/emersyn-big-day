// Emersyn's Big Day - Color Palette
// Soft, kid-friendly pastels with vibrant accents

export const Colors = {
  // Primary
  pink: '#FF6B9D',
  pinkLight: '#FFE4F0',
  pinkDark: '#D4477A',

  // Secondary
  purple: '#B06AFF',
  purpleLight: '#E8D5FF',
  purpleDark: '#7B3DBF',

  // Accents
  yellow: '#FFD93D',
  yellowLight: '#FFF5CC',
  orange: '#FF9F43',
  orangeLight: '#FFE0B2',
  mint: '#6BCB77',
  mintLight: '#D4F5D4',
  sky: '#6CBFFF',
  skyLight: '#D4EEFF',
  coral: '#FF6B6B',
  coralLight: '#FFD4D4',

  // Rarity colors
  rarityCommon: '#A8D8EA',
  rarityRare: '#B06AFF',
  rarityEpic: '#FF6B9D',
  rarityLegendary: '#FFD93D',

  // UI
  white: '#FFFFFF',
  offWhite: '#FFF8FA',
  gray100: '#F7F3F5',
  gray200: '#EDE7EB',
  gray300: '#D4CCD0',
  gray400: '#9E949A',
  gray500: '#6E636A',
  dark: '#2D1F2D',
  black: '#1A0F1A',

  // Status
  success: '#6BCB77',
  warning: '#FFD93D',
  error: '#FF6B6B',
  info: '#6CBFFF',

  // Stat meters
  meterHunger: '#FF9F43',
  meterEnergy: '#6CBFFF',
  meterClean: '#6BCB77',
  meterFun: '#FF6B9D',
  meterPopularity: '#B06AFF',

  // Backgrounds
  bgMorning: '#FFE4F0',
  bgSchool: '#E8D5FF',
  bgPark: '#D4F5D4',
  bgKitchen: '#FFE0B2',
  bgBedroom: '#D4EEFF',
  bgShop: '#FFF5CC',
  bgArcade: '#E8D5FF',
  bgStudio: '#FFD4D4',

  // Overlay
  overlay: 'rgba(45, 31, 45, 0.5)',
  overlayLight: 'rgba(45, 31, 45, 0.2)',
};

export const getRarityColor = (rarity: string): string => {
  switch (rarity) {
    case 'common': return Colors.rarityCommon;
    case 'rare': return Colors.rarityRare;
    case 'epic': return Colors.rarityEpic;
    case 'legendary': return Colors.rarityLegendary;
    default: return Colors.gray300;
  }
};

export const getStatColor = (stat: string): string => {
  switch (stat) {
    case 'hunger': return Colors.meterHunger;
    case 'energy': return Colors.meterEnergy;
    case 'cleanliness': return Colors.meterClean;
    case 'fun': return Colors.meterFun;
    case 'popularity': return Colors.meterPopularity;
    default: return Colors.gray400;
  }
};
