# Changelog

## Unreleased

- Photo mode: press `J` for a free camera to take pictures while your character stands still, with key hints for every control. Fly, zoom, roll, look at or follow what's under the crosshair, change gear from the inventory, and hide the UI with `U`. It stays within 100 m of your character and ends if you take damage.

## 0.2.0

- First person is now made for playing:
  - Your body stays visible apart from the head, so you see your arms and gear with the game's own animations, and the camera moves with your head.
  - Ranged attacks fly to the crosshair, and drawing a bow shifts the view so the bow doesn't cover it.
  - Your body turns to follow your view past 60° (`Body Turn Angle`) and always faces where you look while moving.
  - Head-worn lights shine at the crosshair, and looking down stops at 65° (`Max Look Down`).
- While seated in first person (at the helm, on a chair or riding), you can look over your shoulders and down at your legs, and turning while looking down lifts the view so it stays out of your body (`Seated Turn Angle`, `Seated Max Look Down`, `Seated Max Look Down Sideways`).
- Press `K` to switch the third-person camera between your right and left shoulder.
- The ship view key is now `Shift+C`, so `C` toggles walking as in vanilla.

## 0.1.0

- First release.
- Zoom out further: up to 16 m while walking, 40 m while steering a ship and 24 m while riding (vanilla 8 m, 16 m on ships). Each has its own limits in the config and remembers its own zoom.
- Zoom in past the closest distance for a first-person view, with your body hidden so it doesn't block the view. It stays on when you take the helm, mount up or switch views, until you zoom out.
- In first person, the ship's sail and rudder indicator sits at a fixed spot in front of you instead of beside the helm.
- While steering a ship, press `C` to switch the camera between following you and following the centre of the ship. While steering, `C` no longer toggles walking.
