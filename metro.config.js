// metro.config.js - Configure metro to bundle .glb 3D model files
const { getDefaultConfig } = require('expo/metro-config');

const config = getDefaultConfig(__dirname);

// Add .glb to asset extensions so metro bundles them
config.resolver.assetExts.push('glb', 'gltf');

module.exports = config;
