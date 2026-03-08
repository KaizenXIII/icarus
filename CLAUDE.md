# Icarus

Isometric pixel art spaceship game built in Unity.

## Project Status
Brainstorming phase — no implementation yet.

## Concept
- Genre: Isometric pixel art space game
- Engine: Unity
- Theme: TBD (named after Icarus — hubris, space exploration, sun, etc.)

## Tech Stack
- Engine: Unity (version TBD)
- Language: C#
- Art style: Pixel art, isometric perspective

## Project Structure
```
icarus/
  unity/                        ← Unity project root
    Assets/
      Scripts/
        Core/                   ← Game loop, managers
        Ship/                   ← Ship systems, modules
        Crew/                   ← Crew logic
        World/                  ← Sectors, map, spawning
        Combat/                 ← PvE combat
        UI/                     ← HUD, menus
      Sprites/
      Scenes/
      Prefabs/
      Tilemaps/
      Audio/
      ScriptableObjects/
  CLAUDE.md
  GDD.md
  README.md
```

## Conventions
- C# naming: PascalCase for classes/methods, camelCase for fields
- Prefix private fields with `_`
- One component per file

## Design Notes
See `GDD.md` for the full Game Design Document.

**Locked pillars:** persistent ship, shared universe, small crew (3–6), isometric pixel art, no story, pure systems, async PvP (planned).

**References:** Pixel Starships, FTL, Eve Online (tone).
