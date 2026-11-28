# Changelog

## Unreleased

- If a game update breaks part of the mod, only that part turns off and the rest keeps working: photo mode, the ship view, aiming melee and ranged attacks with your view, the body turning with your view, the look-down limit, the close clipping plane, the first-person ship indicator and Alt+scroll not zooming can each turn off while first person and the field of view still work. The log says which part is off, in one line. If the camera itself breaks, the whole mod turns off cleanly instead of throwing errors.
- New `DisabledFeatures` setting under `[Debug]`, for testing: turns parts of the mod off by name.
- In first person, what you say in chat shows at the top centre of the screen, a little bigger and in bold, instead of jumping between the left and right edges. It moves down below a raid name or boss health bar when one is showing. It can turn off on its own after a game update ("Own chat at top in first person").
- Every keybind is now a key plus an optional modifier (`None`, `Shift`, `Ctrl` or `Alt`), both picked from a list in the config, so combos like Shift + B can be bound without typing them. Settings saved by an older version carry over.
- With `debugmode` on, this mod's key combos no longer also do what their key does in debug mode, such as Ctrl+Z also toggling flying. A key on its own still does both.
- Updated Jotunn to 2.30.3.

## 0.5.4

- The first-person camera sits higher, nearer eye level, (`Camera Height` 0.3, up from 0.1; `Camera Forward` 0.08, from 0.12), so your character no longer feels short.
- Settings that were a position or offset (`Camera Offset`, `Bow Aim Offset`, the ship HUD's `Fixed Position`) are now one number per direction, which are easier to edit. If you changed any of them, set them again under their new names.
- In first person, looking down no longer drops the camera to your shoulders, so your body and arms look the right size and you can't see into your shoulders when you look at them (`Head Nod`, 0.25; 1 is the old behaviour).
- In first person, melee swings now go through the crosshair; before, they landed a little below it.
- Fixed your character's animations freezing in first person inside crypts and other dark places.

## 0.5.3

- In first person, your shadow now has its head (and helmet, hair and beard).
- Fixed putting on a helmet in first person: it no longer shows up when you swing, and isn't huge (and breaks the lighting) when you go back to third person.

## 0.5.2

- Melee swings now tilt up or down with your view, in first and third person, so you can hit enemies above or below you on a hill (`Melee Aim Angle`, 45°; 0 is vanilla). Your upper body leans into the swing to match (`Lean Into Swings`).

## 0.5.1

- In first person, berry bushes and other plants no longer vanish when you walk right up to them.

## 0.5.0

- In photo mode, press `H` to hide your character for shots of the scenery, and `C` to bring the camera back to just behind them.

## 0.4.0

- Press `L` to switch between first and third person without zooming all the way in or out.
- Change the field of view with `Alt+Scroll`, separately for first and third person (`Field of View`, vanilla 65°).
- The first-person camera is now steady while moving and attacking. Raise `Head Bob` to have it bob and sway with your head as before.
- The shoulder the camera is over is remembered between games.

## 0.3.0

- Photo mode: press `J` for a free camera to take pictures while your character stands still.
  - Fly, zoom, roll, and look at or follow what's under the crosshair, with key hints for each control.
  - The hotbar, map and health are hidden, and `U` hides the rest of the UI.
  - Open the inventory to change gear while posing.
  - The camera stays within 100 m of your character, and photo mode ends if you take damage.

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
