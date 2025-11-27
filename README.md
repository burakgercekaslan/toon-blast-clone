# Toon Blast Clone (Unity)

Toon Blast–style match puzzle prototype built with Unity 2022.3.17f1. The project recreates the signature tap-to-pop loop, combo-specific visuals, and casual-friendly UX with audio, menus, and pause/options flows so you can study, mod, or extend the mechanics.

## Gameplay Overview
- **Dynamic board:** `GameManager` instantiates an `M x N` grid with `K` colors, tracks block positions in a dictionary, and repopulates gaps after pops to maintain a full board.
- **Tap groups to pop:** Selecting any block triggers a recursive flood-fill via `Block` that gathers any adjacent blocks of the same color; groups above `A / B / C` thresholds get upgraded sprites, FX, and score potential before being destroyed.
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

## Customizing Gameplay
- Tweak `M`, `N`, `K`, `A`, `B`, `C` in the `GameManager` inspector to change board size, color count, and sprite thresholds.
- Assign different prefabs under **Default Cubes** or swap sprites and audio clips to reskin the theme quickly.
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



