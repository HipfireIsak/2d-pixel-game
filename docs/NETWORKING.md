# Networking — AetherEcho: Chrono-Fractures

## Stack

| Layer | Package | Role |
| ----- | ------- | ---- |
| High-level netcode | Mirror (`com.mirrornetworking.mirror`) | spawning, sync vars, RPCs, commands |
| Transport | KCP (`KcpTransport`, bundled with Mirror) | self-hosted UDP with reliable channels |

This project adapts the multiplayer pattern from **RollABall3D / Relic Hunters**, but replaces Steam lobby transport with direct IP hosting suitable for a self-hosted MMORPG prototype.

## Connection flow

### Hosting
1. Run the game and click **Host Game** in the menu.
2. `NetworkSessionController.TryStartHost()` configures KCP on the chosen port (default `7777`) and calls `NetworkManager.StartHost()`.
3. The host machine acts as both server and local client.

### Joining
1. Enter the host machine's LAN/WAN IP address and port.
2. Click **Join Game** → `NetworkManager.StartClient()`.
3. `AetherEchoNetworkManager.OnServerAddPlayer` spawns a `PlayerAvatar` at the nearest `NetworkStartPosition`.

## Authority model

| State | Authority | Mechanism |
| ----- | --------- | --------- |
| Player position | server | client sends movement input via `Command`; server moves `CharacterController` and broadcasts with `ClientRpc` |
| Health / mana / cooldowns | server | `SyncVar` fields on `CombatantState` |
| Spell casts | server | client predicts FX locally; `CmdRequestCastSpell` validates through `SpellEngine` on the server |
| Display name | owning client → server → all | `Command` sets a `SyncVar` |

## Local testing

1. In Unity: **AetherEcho → Setup Project**, open `Assets/Scenes/Bootstrap.unity`, press Play, click **Host Game**.
2. Build a standalone player (**File → Build Settings → Build**).
3. Run the build on another machine (or the same machine with a second instance if your OS allows it).
4. Join using `127.0.0.1:7777` for same-machine tests or the host LAN IP otherwise.

## Content pipeline

Designer JSON lives in `Assets/StreamingAssets/`:

- `spells.json` — spell definitions consumed by `SpellContentManager`
- `classes.json` — class stat tables consumed by `ClassContentManager`

Gameplay code reads these at boot through `GameSystemsBootstrap` on the persistent `NetworkBootstrap` object.

## Controls

- **WASD** — move (isometric)
- **Shift** — sprint
- **Mouse** — aim direction while casting
- **1 / 2** — cast sample spells (`Chrono-Blast`, `Echo Sweep`)
- **Esc** — toggle session menu while connected
