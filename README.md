# AetherEcho: Chrono-Fractures

Minimal Unity 2022.3 LTS project for a self-hosted isometric action RPG prototype.

## Quick start

1. Open this folder in **Unity 2022.3.62f1**.
2. Run **AetherEcho → Rebuild World Visuals** (or **Setup Project**) to wire pixel-art sprites, enemies, and the Lost Ark-style camera.
3. Open `Assets/Scenes/Bootstrap.unity` and press Play.
4. Click **Host Game**, explore the tile field, press **E** near the Chrono Sage, and cast with **1 / 2 / 3**.

See [docs/NETWORKING.md](docs/NETWORKING.md) for the full multiplayer architecture.

## Architecture

```
[ StreamingAssets JSON ] → [ Content Managers ] → [ SpellEngine / Combat ]
                                                          │
[ Mirror + KCP Session ] ←────────────────────────────────┘
```

## Included systems

- Self-hosted Mirror networking (adapted from RollABall3D, without Steam)
- JSON-driven spells and classes
- Server-authoritative combatant state and spell validation
- Rolling 5-second threat matrix for progressive aggro
- 2.5D isometric movement with billboard sprite facing

## Unity version

Target editor: **2022.3.62f1 LTS**
