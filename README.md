[![CI](https://img.shields.io/github/actions/workflow/status/javidahmed64592/valheim-bettercamera/ci.yml?branch=main&style=flat-square&label=CI&logo=github)](https://github.com/javidahmed64592/valheim-bettercamera/actions/workflows/ci.yml)
[![Package](https://img.shields.io/github/actions/workflow/status/javidahmed64592/valheim-bettercamera/package.yml?branch=main&style=flat-square&label=Package&logo=github)](https://github.com/javidahmed64592/valheim-bettercamera/actions/workflows/package.yml)
[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](https://opensource.org/licenses/MIT)

# BetterCamera

Zoom further out or into first person, with zoom limits for walking, sailing and riding, and switch the camera to follow the centre of your ship or mount.

- **Zoom further**: Zoom out up to 16 m while walking, 40 m while steering a ship and 24 m while riding.
- **First person**: Zoom all the way in to look around through your character's eyes.
- **Travel views**: While steering a ship or riding, switch the camera to follow the ship's or mount's centre, so you can look all around it.

## Zoom

Scroll to zoom as in vanilla. Walking (and swimming), steering a ship, and riding a tamed animal each have their own closest and furthest distance, and each remembers how far you were zoomed, so getting off a ship brings back your walking zoom.

Zoom in past the closest distance to switch to first person. Your body is hidden so it doesn't block the view, but its shadow stays. Zoom out to go back to third person. First person is a way to look around: attacking and building work, but aren't adjusted for it yet.

## Travel Views

While steering a ship or riding, press `C` to switch the camera between following you, as in vanilla, and following the centre of the ship or mount. Turning the camera then swings it around the whole ship or mount instead of around you at the helm or in the saddle, which makes it much easier to see what's around you.

- The camera moves smoothly to the new view. The ship and the mount each remember the view you last used.
- First person is only available while the camera follows you.
- While steering or riding, `C` no longer toggles walking, which has no effect on a ship or a mount. Rebind the key in the game's controls menu.

## Configuration

Settings are in the BepInEx config file `javidahmed64592.bettercamera.cfg`.

## Compatibility

- Mods that also change the camera's zoom or position (such as first-person mods) may clash with this one.
- This mod has not been tested with a gamepad, only with keyboard and mouse.

## Uninstalling

Nothing is saved to your world or character, so the mod can be removed at any time.

## License

This project is licensed under the MIT License - see the [LICENSE](https://github.com/javidahmed64592/Valheim-BetterCamera/blob/main/LICENSE) file for details.
