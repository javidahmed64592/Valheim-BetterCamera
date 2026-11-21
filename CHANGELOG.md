# Changelog

## Unreleased

- First person is now made for playing, not just looking around:
  - Your body stays visible apart from the head, so you see your arms and gear with the game's own animations.
  - Bows, spears, staffs and other ranged attacks fly to where the crosshair points.
  - Your body turns with your head once you look more than 60° to the side (`Body Turn Angle`), and always faces where you look while moving.
  - The camera moves with your head as the animations lean and bob, and lights worn on your head (like the Dvergr circlet's) shine at the crosshair.
  - Your hands and gear are no longer cut off close to the camera, and while drawing a bow the view shifts slightly so the bow doesn't cover the crosshair (`Bow Aim Offset`).
  - Looking down stops at 65° (`Max Look Down`), so the camera doesn't see inside your armour.
- Press `K` to move the third-person camera between your right and left shoulder.
- The ship view key is now `Shift+C` (setting `Switch Ship View`), so `C` toggles walking as in vanilla, even at the helm.

## 0.1.0

- First release.
- Zoom out further: up to 16 m while walking, 40 m while steering a ship and 24 m while riding (vanilla 8 m, 16 m on ships). Each has its own limits in the config and remembers its own zoom.
- Zoom in past the closest distance for a first-person view, with your body hidden so it doesn't block the view. It stays on when you take the helm, mount up or switch views, until you zoom out.
- In first person, the ship's sail and rudder indicator sits at a fixed spot in front of you instead of beside the helm.
- While steering a ship, press `C` to switch the camera between following you and following the centre of the ship. While steering, `C` no longer toggles walking.
