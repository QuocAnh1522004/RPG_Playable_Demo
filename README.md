# Dicegeon – Playable Ad (Unity Playable Ads Developer test)

Turn-based dice RPG playable built with Unity 6 and Luna Playworks.
The exported single-file HTML (`Dicegeon_PA.html`, under 5 MB) is submitted separately.

## Requirements

- **Unity 6000.0.85f2** (any Unity 6 should work)
- **Luna Playworks Plugin 7.2.0** – required, the project does not compile without it

## Setup

The Luna plugin is not included in this repo, so Unity needs to be pointed at your local copy before the project will compile.

1. Download and unzip **Luna Playworks Plugin 7.2.0**. Keep the whole `7.2.0` folder as it is (the plugin needs its `pipeline` and `engine` folders to build).
2. Open `My project/Packages/manifest.json` and change this line to your own path:

   ```json
   "com.unity.playworks.upp": "file:C:/Users/HELLO/Downloads/7.2.0/scripts",
   ```

   e.g. `"file:D:/Luna/7.2.0/scripts"` – it has to point at the `scripts` folder inside `7.2.0`.

   Alternatively, remove that line, open the project, then go to **Window → Package Manager → + → Install package from disk…** and pick `7.2.0/scripts/package.json`.
3. Open `My project` in Unity Hub and wait for the import.
4. Open **`Assets/Scenes/Game.unity`** and press **Play**.

The scene is set up in portrait; set the Game view to a 9:16 resolution (e.g. 1080×1920) to see it as intended.

## Building the HTML

Open the Luna Playworks window, log in, and build. The project settings needed for Luna are already in place:

- Input: old Input Manager (Luna doesn't support the new Input System)
- Color space: Gamma
- `luna.json` includes `TMP Settings` and the default style sheet (TextMeshPro fails without them)
- Physics modules are excluded, nothing in the game uses physics

## Project layout

Everything written for this test is in `Assets/_Game`; `Assets/PLA_TEST` is the provided asset pack.

```
Assets/_Game
├── Scripts
│   ├── Core      GameFlowController (the 10-step flow), StoreRedirect, IdleWatcher (AFK), CameraFollow
│   ├── Board     Board / tiles, Dice, HeroMover, TileObject (pickups and monsters on tiles)
│   ├── Battle    BattleController (scripted auto-battle), HeroStats, FxSpawner
│   ├── UI        SkillPanel, TutorialHand, HUD, HP bars, damage numbers, AFK overlay
│   ├── Data      SkillData / SkillLibrary / StageConfig (ScriptableObjects)
│   └── Audio     AudioManager, SoundLibrary
├── Editor        menu tools under "RPG Playable/" that generate the skill assets,
│                 animator controllers, UI and battle setup
├── Data          generated skill and sound assets
├── Animators     generated animator controllers
└── Prefabs
```

## How it works

- `GameFlowController` runs the whole playable as one coroutine: (pick skill → roll dice → walk → battle) × 3, then a last skill pick opens the store.
- The dice result is not random: it is the distance to the next monster's tile.
- Battles are scripted to end on the required round (1 / 2 / 3). The monster's HP after each round is `maxHp × (1 − round / rounds)`, split across that round's hits, so the picked skills change how a fight looks but never how long it lasts.
- Skills fire in the order from the spec: Flames From Above → Dagger → Lightning Dagger → Round Fire Strike → Basic Attack.

## Notes on Luna compatibility

A few things that work in the Unity editor behave differently in the Luna build, the code is written around them:

- Luna's coroutine runner stops the whole chain when a nested coroutine and its parent finish in the same step, so coroutines that wait on another one end with `yield return null` (see `TweenWait.cs`).
- `Animator.CrossFadeInFixedTime` is not supported, animations use `Animator.Play`.
- UI that follows 3D objects (dice tap area, HP bars, damage numbers) is placed with screen anchors, not `WorldToScreenPoint` pixels, which don't match the canvas in the browser.
