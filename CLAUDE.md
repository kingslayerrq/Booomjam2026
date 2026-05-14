# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

Unity 6 (URP) game about monitoring prisoners through surveillance cameras during the day and defending a surveillance room at night. The project lives in `Assets/` — all game code is under `Assets/Scripts/`.

There are two scenes: `RQScene.unity` (gameplay) and `RQScene _Art.unity` (art pass). No build or test CLI — everything is run through the Unity Editor.

---

## Architecture

### Manager Hierarchy

`GameManager` is the top-level orchestrator. It owns all game-state transitions (new game, load, game over, day complete) and subscribes to `DayManager` and `PlayerHealth` events. All other managers are injected via Inspector references on the GameManager GameObject.

```
GameManager
├── DayManager          — day/night time loop, emits phase events
├── PrisonerManager     — spawns prisoners, assigns schedules and bad roles each day
│   └── PrisonerEvidenceManager  — assigns evidence after schedule is built
├── RoomManager         — singleton string→GameObject registry for all rooms
├── PlayerResource      — battery + energy pools
├── PlayerHealth        — HP; depleted triggers GameManager.HandleGameOver
└── SurveillanceManager — holds SurveillanceFeed array (light wrapper)
```

### Day Phase Flow

1. `GameManager.StartNewGame()` → `DayManager.StartDay(1)`
2. `DayManager.OnDayStarted` → `PrisonerManager.HandleNewDayStart()`
   - `ResetPrisoners` → `AssignBadPrisoners` → `AssignDailySchedule`
   - `AssignDailySchedule` calls `PopulateOfficialSchedule` then `PrisonerEvidenceManager.AssignEvidenceForDay`
3. Each frame: `PrisonerActionController.Update()` calls `UpdateSchedule()`, which compares `DayManager.CurrentHour` against the prisoner's `DailySchedule` (list of `ScheduleBlock`) and transitions actions.
4. `DayPhase.Day` expires → `DayPhase.Night` starts → `DayManager.OnNightStarted` → `PrisonerEvidenceManager.ResolveUncaughtHighRiskEvidence()`
5. Night expires → `DayManager.OnNightEnded` + `OnDayEnded`.

### Prisoner Action System

Actions are `ScriptableObject` subclasses of `PrisonerAction`. Key contract:

- `StartAction(controller)` — called once when a block becomes active; moves prisoner to room and starts wandering
- `UpdateAction(controller)` — called every frame
- `EndAction(controller)` — called when block ends
- `GetTargetRoomName(controller)` — **virtual, must override for any action that resolves its room dynamically**. Used by `OutOfScheduleRoom` aux and `PrisonerSchedule` doc. The resolved name is cached on `ScheduleBlock.resolvedTargetRoomName` at schedule-build time so that `StartAction` and `GetTargetRoomName` always return the same room.

`NormalAction` uses `NormalActionRoomMode` (UseTargetRoom / UseAssignedCell / PickRandomFromAllowedRooms). When mode is `PickRandomFromAllowedRooms`, the room is resolved once during `PopulateOfficialSchedule` and cached — `StartAction` reads `CurrentScheduleBlock.resolvedTargetRoomName` rather than re-rolling.

`SleepAction` always sends the prisoner to their `PrisonerData.AssignedCellRoom` (overrides `GetTargetRoomName`).

### Schedule Assignment (per day)

```
DetermineConcreteBadCount(day)
  days 1–earlyGameDayThreshold (default 3) → exactly 1 concrete bad action
  later days → 50% chance of 1, else 0

concreteBadSet = first N shuffled bad prisoners
Per block:
  concreteBadSet members → badPool action, isConcreteBadAction=true
  everyone else          → goodPool action
```

Evidence is layered on top by `PrisonerEvidenceManager.AssignEvidenceForDay`:
- Concrete bad prisoner → **no evidence** (the action is their tell)
- Other bad prisoners → **HighRisk** (one of: CameraFlicker / StrangeSound / SpiritOrb) + **Aux** (1–N blocks)
- Good prisoners → **Aux** only, rolled per block at `goodAuxiliaryChance`

### Evidence System

Two tiers live on separate objects:

| Tier | Stored on | Types |
|---|---|---|
| HighRisk | `Prisoner` | CameraFlicker, StrangeSound, SpiritOrb |
| Auxiliary | `ScheduleBlock` | OutOfScheduleRoom, StareAtCamera, ConstantMovement, AbnormalBatteryDrain, FeatureMismatch, ObjectMoved |

`PrisonerEvidenceManager` drives auxiliary at runtime via `StartAuxiliaryBehavior` / `UpdateAuxiliaryBehavior` / `EndAuxiliaryBehavior`, called from `PrisonerActionController` around each block transition.

`AbnormalBatteryDrain` does not self-tick — `SurveillanceCamController.ConsumeControlBattery()` calls `PrisonerEvidenceManager.GetAuxBatteryDrainRate(room)` each frame while the camera is controlled.

### Official Schedule Document

`PrisonerSchedule` (singleton MonoBehaviour) holds the public-facing record of what each prisoner *should* be doing — always good actions on paper. Populated by `PrisonerManager.PopulateOfficialSchedule` immediately after runtime schedule assignment. Concrete bad prisoners show a randomly chosen cover good action in their entry. Query via `PrisonerSchedule.Instance.GetSchedule(prisonerID)`.

### Surveillance UI / Player Interaction

`SurveillanceUI` owns camera feed display and prisoner raycasting (hover highlight + click to open `SurveillancePrisonerInteractionPanel`). Prisoner interaction (highlight + panel) is **gated on `DayManager.IsDayPhase`** — both `CanUpdatePrisonerTarget()` and `CanProcessExpandedFeedClick()` check this, so no prisoner can be locked up at night.

`PlayerInteract` handles world-space interactables (doors, computers, coffee mug) via spherecast. It is blocked when `GameManager.IsMenuOpen`, `GameManager.IsCursorUnlocked`, or `SurveillanceUI.IsOpen`.

### Room System

`RoomManager` is a singleton that maps `string roomName → GameObject`. Room names are synced from `GameObject.name` via `OnValidate`. All runtime room lookups go through `RoomManager.Instance.GetRoomByName(name)`. The `[RoomDropdown]` attribute on serialized string fields draws a populated dropdown in the Inspector (implemented in `Editor/RoomDropdownDrawer.cs`).

`RoomAnchorSet` (on room GameObjects) provides per-room wander center positions and entry spawn offsets, indexed by `MovementIndex` (derived from prisoner ID so it's stable across frames).

---

## Key Conventions

**ScriptableObject data assets** live in `Assets/Datas/`. Schedule configs are per-day (`Day01.asset`–`Day07.asset`). Action assets are in `ActionDatas/GoodActions/` and `ActionDatas/BadActions/`.

**Adding a new PrisonerAction:** subclass `PrisonerAction`, add `[CreateAssetMenu]`, override `GetTargetRoomName(controller)` if the destination is dynamic, override `StartAction` to call `GoToRoom` + `StartWanderingInRoom`.

**Adding a new AuxiliaryEvidenceType:** add the enum value to `PrisonerEvidenceTypes.cs`, create an `AuxiliaryEvidenceDefinition` asset, handle it in `PrisonerEvidenceManager.StartAuxiliaryBehavior` / `UpdateAuxiliaryBehavior` / `EndAuxiliaryBehavior`.

**`IsDayRunning` vs `IsDayPhase`:** `IsDayRunning` is an alias for `IsDayPhase` — both are true only during the Day phase (not Night). `PrisonerActionController.Update` gates on `IsDayRunning`; prisoner movement and actions stop at night.
