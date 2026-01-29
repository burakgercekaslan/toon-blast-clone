# Toon Blast Clone (Unity)

Toon Blast–style match puzzle prototype built with Unity 2022.3.17f1. The project recreates the signature tap-to-pop loop, combo-specific visuals, and casual-friendly UX with audio, menus, and pause/options flows so you can study, mod, or extend the mechanics.

## Gameplay Overview
- **Dynamic board:** `GameManager` instantiates an `M x N` grid with `K` colors, tracks block positions in a dictionary, and repopulates gaps after pops to maintain a full board.
- **Tap groups to pop:** Selecting any block triggers a recursive flood-fill via `Block` that gathers any adjacent blocks of the same color; groups above `A / B / C` thresholds get upgraded sprites, FX, and score potential before being destroyed.
- **Obstacle boxes:** Optional box tiles can be spawned across specific rows/columns (configured in `GameManager`). Popping blocks adjacent to a box damages it until it breaks.
- **Always-playable:** When `maxTogetherCount` drops to 1 the deck automatically shuffles (top half cleared, bottom half transposed) to guarantee that at least one move stays available.
- **Juice & feedback:** Pop/shuffle SFX hooks (`AudioManager`, `GameManager`) combine with randomized drop heights and sprite swaps to reinforce the toy-like feel.

```292:390:Assets/Scripts/GameManager.cs
    private void ChangeSprites() //change sprites according to how many objects are next to each other.
    {
        ...
        if (maxTogetherCount == 1)
        {
            Invoke("ShuffleDeck", 0.1f);
        }
    }
```

## Scenes & Flow
- `MainMenu` scene: landing screen with Play, Options, and Quit buttons (`MainMenuManager`). AudioManager is spawned here to persist across scenes.
- `SampleScene` scene: actual gameplay board and HUD. Includes pause overlay (`PauseMenuManager`) so players can resume, tweak options, return to menu, or exit.

## Systems Breakdown
- `GameManager`: grid bootstrap, neighbor-search logic (`BlockPop`, `BlockChange`), sprite-state thresholds (`A/B/C`), gap filling (`UpdateDict`, `UpdateGrid`), shuffling, and audio triggers.
- `Block`: per-block metadata (x, y, color) plus click handler that orchestrates pop → destroy → refill → shuffle checks.
- `AudioManager`: singleton that persists via `DontDestroyOnLoad`, exposes `SetMusicVolume` / `SetSFXVolume`, stores volumes in `PlayerPrefs`, and auto-configures missing AudioSources so menus/gameplay share settings.
- `MainMenuManager`, `OptionsManager`, `PauseMenuManager`: handle UI panel state, slider bindings, scene swaps, pause toggling, and graceful quit for both Editor and builds.

```11:105:Assets/Scripts/PauseMenuManager.cs
public class PauseMenuManager : MonoBehaviour
{
    ...
    public void OnMainMenuButtonClicked()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene("MainMenu");
    }
}
```

## Project Layout
- `Assets/Scripts`: core gameplay, audio, and UI controllers.
- `Assets/Prefabs/DefaultCubes`: one prefab per block color (with matching sprites located in `Assets/Sprites`).
- `Assets/Audio`: looping music clip plus pop/shuffle SFX referenced by `AudioManager` and `GameManager`.
- `Assets/Scenes`: `MainMenu` (create if missing) and `SampleScene` gameplay scene.
- `ProjectSettings`: Unity project configuration (Unity 2022.3 LTS).

## Getting Started
1. Install **Unity 2022.3.17f1** (or any 2022.3 LTS compatible build). Version info lives in `ProjectSettings/ProjectVersion.txt`.
2. Clone or download this repo and **open the root folder** (`toon-blast-clone`) in Unity Hub.
3. Open the `MainMenu` scene for the full loop or `SampleScene` to jump straight into gameplay.
4. Press Play: click groups of two or more matching blocks; the grid refills automatically and shuffles if you run out of moves.

## GameManager Setup (Scene)
1. In your gameplay scene (ex: `SampleScene`), create an empty GameObject named `GameManager`.
2. Add the `GameManager` component.
3. Assign references in the inspector (details below). If any references are missing, the game may still run, but you’ll lose those features (for example, no borders scaling, no boxes, or no SFX).

## GameManager Configuration (Inspector)
The `GameManager` component is the main place to change:
- Board row/column count
- How many colors exist
- When “combo” sprites appear
- Which prefabs represent each color
- Where obstacle boxes spawn

### Core board variables
- **`M`** (rows / height)
  - Board Y size. Valid coordinates are `y = 0..M-1`.
- **`N`** (columns / width)
  - Board X size. Valid coordinates are `x = 0..N-1`.
- **`K`** (color count)
  - Random colors are chosen with `Random.Range(0, K)`.
  - Important: `K` must be `<= DefaultCubes.Length`.

### Group thresholds (sprite upgrades)
When the board is evaluated, groups are detected and every block in the group gets a sprite based on the group size.
- **`A`**
  - If group size `> A` it uses the “tier 1” sprite.
- **`B`**
  - If group size `> B` it uses the “tier 2” sprite.
- **`C`**
  - If group size `> C` it uses the “tier 3” sprite.

The sprite lookup is:
- Base sprite index: `color * 4`
- Tier 1: `color * 4 + 1`
- Tier 2: `color * 4 + 2`
- Tier 3: `color * 4 + 3`

So for `K` colors, **`BlockSprites` should contain `K * 4` sprites** in this exact order.

### Prefabs / visuals
- **`DefaultCubes`**
  - Array of prefabs (one per color index). `DefaultCubes[color]` is instantiated.
  - Each prefab should have at least:
    - `SpriteRenderer`
    - `Rigidbody2D` (dynamic) so blocks can fall/settle
  - `BlockFactory` will add a `BoxCollider2D` if missing.
- **`BlockSprites`**
  - Sprite atlas array used by `ChangeSprites()` to swap visuals based on group size.
- **`Borders`**
  - Optional. If assigned, it is scaled at runtime using `M` (`Borders.transform.localScale = new Vector3(15, M / 2f, 0)`).
- **`Ground`**
  - Optional invisible ground object. It’s repositioned based on `M` so blocks don’t fall forever.
- **`Cubes`**
  - Optional parent `Transform` to keep the hierarchy tidy (all spawned blocks/boxes are parented here).

### Audio
- **`PopAudio`**
  - Played once when a valid group is popped.
- **`ShuffleAudio`**
  - Played when the game shuffles colors due to no available moves.

### Boxes / obstacles
Boxes are optional obstacle tiles. They are stored in the same board dictionary as blocks and act like fixed “separators” for gravity/refill.

- **`BoxPrefab`**
  - Prefab spawned for each box cell.
  - The factory forces its `Rigidbody2D` to `Static` and freezes movement.
- **`Box1Sprite`**, **`Box0Sprite`**
  - Visual states for a box as it takes damage.
  - Boxes start with `health = 2`.
    - `health >= 2` uses `Box1Sprite`
    - `health == 1` uses `Box0Sprite`
    - `health <= 0` destroys the box

#### Where boxes spawn
Box placement is controlled by these toggles/arrays:
- **`UseSelectedBoxRows`**
  - If enabled, `SelectedBoxRows[y] = true` spawns boxes across *all columns* in that row `y`.
- **`UseSelectedBoxColumns`**
  - If enabled, `SelectedBoxColumns[x] = true` spawns boxes across *all rows* in that column `x`.
- **`SelectedBoxRows` / `SelectedBoxColumns`**
  - Boolean arrays whose sizes are automatically resized in `OnValidate()` to match `M` and `N`.

Notes:
- Rows/columns can overlap. If both a row and column are selected, the intersecting cell still spawns a single box.
- Boxes replace whatever spawned there during initial grid generation.

#### How boxes take damage
When you pop a group, every popped normal block checks its 4-neighbors (up/down/left/right). Any adjacent `BoxBlock` takes 1 damage (once per box per pop).

### Input lock tuning (optional)
`GameManager` temporarily locks input while blocks are falling.
- **`InputLockMinDuration`**
  - Minimum time to keep input locked after a pop.
- **`InputLockMaxDuration`**
  - Maximum lock timeout (failsafe).
- **`SettleVelocityThreshold`**
  - How still blocks must be (velocity) before input is unlocked.

## Examples
### Example 1: Classic 8x8 board with 5 colors
- Set:
  - `M = 8`
  - `N = 8`
  - `K = 5`
- In `DefaultCubes`, provide 5 color prefabs.
- In `BlockSprites`, provide `5 * 4 = 20` sprites ordered by color then tier (base, A, B, C).

### Example 2: Add a full “box row” at the bottom
- Enable `UseSelectedBoxRows`
- Ensure `SelectedBoxRows` has length `M`
- Set `SelectedBoxRows[0] = true`

### Example 3: Add a box column (vertical wall)
- Enable `UseSelectedBoxColumns`
- Ensure `SelectedBoxColumns` has length `N`
- Set `SelectedBoxColumns[3] = true` to place a wall at column `x = 3`

## Common Pitfalls
- **`K` bigger than `DefaultCubes.Length`**
  - Some colors will try to spawn without a prefab (you’ll see missing blocks).
- **Wrong `BlockSprites` ordering/size**
  - Remember: `4` sprites per color, and the lookup is `color * 4 + tier`.
- **Boxes not spawning**
  - You must assign `BoxPrefab` and enable at least one of `UseSelectedBoxRows` / `UseSelectedBoxColumns`.
- **Blocks not falling / input never unlocks**
  - Ensure your cube prefabs include a `Rigidbody2D` and that nothing pins them in place.

## Customizing Gameplay (Other)
- Assign different prefabs under **Default Cubes** or swap sprites/audio clips to reskin the theme quickly.
- Audio volume defaults are exposed in `AudioManager`; use `OptionsManager` sliders to persist changes through `PlayerPrefs`.

## Building
- Switch to your target platform via **File → Build Settings**, add `MainMenu` and `SampleScene` to the build list (in that order), then build & run.
- For mobile builds, ensure touch input is enabled (current setup relies on `OnMouseDown`, which works with Unity’s default touch-to-mouse mapping; replace with `IPointerClickHandler` if you need explicit touch support).

## Known Limitations / Next Ideas
- Currently lacks score goals, move counters, boosters, or star rewards typical of Toon Blast—add UI + state tracking to align with production gameplay.
- Visual polish (particles, animations, tweened drops) is minimal; hooking into `toPop` and `UpdateGrid` events is the best place to inject VFX.
- Board data is stored in-memory only; consider ScriptableObjects for level definitions or saving progress if shipping.

## License
No license file is included. Treat this as a private learning project unless the author adds an explicit license.



