# Icarus — Game Design Document

> Status: Skeleton / Early Design
> Engine: Unity | Art: Isometric Pixel Art | Genre: Space Survival MMO-lite

---

## 1. Elevator Pitch

A lonely, atmospheric deep space survival game. You captain a single persistent ship in a shared universe — mining, salvaging, trading, and upgrading your way deeper into the unknown. Small crew, big decisions, slow burn.

---

## 2. Core Loop

```
JUMP to sector
    ↓
SCAN environment (asteroids, wrecks, stations, players)
    ↓
DECIDE (mine / salvage / trade / avoid / engage)
    ↓
GATHER resources
    ↓
MANAGE crew & ship systems
    ↓
UPGRADE ship (modules, blueprints)
    ↓
JUMP deeper ──────────────────────────────┐
    ↓                                     │
ENCOUNTER (PvE threat or async PvP raid)  │
    ↓                                     │
SURVIVE / RETREAT / FIGHT                 │
    └─────────────────────────────────────┘
```

---

## 3. World Structure

### Shared Universe
- One persistent universe shared by all players
- Divided into **Sectors** — procedurally generated regions within fixed zones
- Zones vary by danger level and resource richness

### Zone Tiers
| Zone | Danger | Resources | Notes |
|------|--------|-----------|-------|
| Fringe | Low | Common | Starting area |
| Mid-Belt | Medium | Uncommon | First real threats |
| Deep Space | High | Rare | Endgame content |
| Void | Extreme | Legendary | TBD |

### Sector Contents (proc-gen per instance)
- Asteroid fields
- Derelict wrecks
- Anomalies
- NPC stations (trading posts, faction outposts)
- Other player ships

---

## 4. Crew System

### Crew Size
- 3 to 6 crew members per ship
- Each crew member assigned to one **station**

### Stations
| Station | Function |
|---------|----------|
| Bridge | Navigation speed, jump range |
| Engineering | Repair rate, power efficiency |
| Weapons | Combat damage, accuracy |
| Mining Bay | Resource yield, extraction speed |
| Medical | Crew recovery, morale |
| Cargo | Carry capacity, trade bonuses |

### Crew Traits (TBD)
- Each crew member has 1–2 traits affecting their station
- Can be recruited at stations, rescued from wrecks
- Permanent death on critical failures (optional mechanic — TBD)

---

## 5. Resource System

### Resource Types
| Resource | Source | Use |
|----------|--------|-----|
| Ore | Asteroid mining | Crafting, repair |
| Scrap Metal | Salvaging wrecks | Modules, hull repair |
| Fuel Cells | Stations, wrecks | Jumping between sectors |
| Components | Drops, trade | Upgrades, blueprints |
| Credits | Trading, missions | Station purchases |
| Rare Materials | Deep space, anomalies | Advanced upgrades |

### Resource Flow
- Mine → refine → craft or sell
- Salvage → strip → repurpose or sell
- Trade → buy what you can't find

---

## 6. Ship Module System

### Modular Ship
- Ship has a fixed number of **slots** by category
- Slots unlocked via hull upgrades

### Module Categories
| Category | Examples |
|----------|---------|
| Propulsion | Thrusters, jump drives |
| Weapons | Lasers, missiles, railguns |
| Defense | Shields, hull plating, point defense |
| Utility | Scanners, tractor beams, cloaking |
| Mining | Drills, refineries, extractors |
| Crew | Quarters, med-bay, training pods |

### Upgrade Path
- Modules found as **blueprint drops** or purchased at stations
- Blueprints require resources + components to craft
- Higher tier modules require rarer materials (zone-gated)

---

## 7. Progression Curve

```
Early Game
  └── Fringe zones, common resources, basic modules
  └── Build out crew stations, learn systems

Mid Game
  └── Mid-Belt, uncommon resources, module synergies
  └── First async PvP encounters
  └── Specialization begins (combat vs. trade vs. exploration build)

Late Game
  └── Deep Space, rare materials, legendary blueprints
  └── Full ship build optimized
  └── PvP raiding, defending your ship
```

---

## 8. PvP — Async Raiding (Planned)

- Players can flag their ship for PvP in certain zones
- Other players can initiate a **raid** while you're offline
- Raid plays out via simulation — your crew/modules defend automatically
- Win/lose determines resource loss or gain
- Detailed design TBD

---

## 9. Tone & Aesthetics

- **Atmosphere:** Lonely, vast, quiet — punctuated by danger
- **Art:** Isometric pixel art, dark palette, neon accents for UI/tech
- **Audio:** Ambient space soundscapes, minimal music
- **UI:** Clean, diegetic where possible

---

## 10. Open Questions

- [ ] Permadeath for crew? Morale system?
- [ ] Is there a faction/reputation system at stations?
- [ ] How are players visible to each other on the map?
- [ ] Void zone — what makes it distinct?
- [ ] Monetization model
- [ ] Platform targets (PC, mobile, both?)
- [ ] Multiplayer backend (Photon, Mirror, custom?)

---

## 11. Out of Scope (for now)

- Multiple ships / fleet management
- Real-time PvP
- Story / narrative
- NPC factions with quests
