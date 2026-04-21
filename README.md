# Ubisoft Recruitment Test - Multiplayer Egg Collection

## 1) Project Overview

This project is a top-down multiplayer simulation game where players compete to collect eggs within a fixed match duration.  
The runtime model is intentionally **server-authoritative**: the server simulator owns canonical gameplay state (movement, egg spawning, collision, score), while the client is responsible for rendering, smoothing, and local responsiveness.

The implementation targets interview requirements for modularity, networking architecture, and latency handling, with the code structured so the in-process server simulator can later be replaced by a real remote backend.

---

## 2) Key Technical Features

- **Server-Authoritative Architecture**
  - `ServerSimulator` updates authoritative `GameState` and publishes snapshots.
  - Client never writes canonical world state directly.

- **Custom A* Pathfinding (Grid-based)**
  - Bots use a custom A*-style implementation over `GridSystem`.
  - No third-party pathfinding package/plugin is used.

- **Advanced Latency Compensation**
  - **Human player**: client-side prediction + server reconciliation (sequence-ack based).
  - **Remote players (bots)**: snapshot buffering + interpolation/extrapolation for delayed updates.
  - **Egg collection UX**: predictive local hide + pending confirmation/reconciliation flow.

---

## 3) Architecture Breakdown

### Networking and Message Flow

1. Human input is captured on client and sent as JSON via `FakeNetwork.SendFromClient`.
2. Server receives delayed input (`OnMessageFromClient`), applies it in authoritative simulation tick.
3. Server sends delayed snapshots (`PlayerStateMessage`) via `FakeNetwork.Send`.
4. Client receives snapshots (`OnMessageReceived`) and updates:
   - interpolation buffer,
   - player/egg render state,
   - reconciliation for the local human player.

**Key scripts**
- Server loop: [`Assets/Project/Scripts/Server/Core/ServerSimulator.cs`](Assets/Project/Scripts/Server/Core/ServerSimulator.cs)
- Transport simulation: [`Assets/Project/Scripts/Server/Core/FakeNetwork.cs`](Assets/Project/Scripts/Server/Core/FakeNetwork.cs)
- Message contracts: [`Assets/Project/Scripts/Shared/Messages/PlayerStateMessage.cs`](Assets/Project/Scripts/Shared/Messages/PlayerStateMessage.cs)
- Serialization: [`Assets/Project/Scripts/Shared/Messages/MessageSerializer.cs`](Assets/Project/Scripts/Shared/Messages/MessageSerializer.cs)

### Human Movement: Prediction + Reconciliation

- Client predicts movement immediately in `InputSystem` (`LateUpdate`) and sends sequenced input commands.
- Server applies ordered inputs and returns `lastAcknowledgedHumanInputSequence`.
- Client reconciles by replaying unacknowledged input history from server position.

**Key scripts**
- Input + prediction: [`Assets/Project/Scripts/Client/Systems/InputSystem.cs`](Assets/Project/Scripts/Client/Systems/InputSystem.cs)
- Shared movement utility: [`Assets/Project/Scripts/Shared/Movement/HumanGridMovement.cs`](Assets/Project/Scripts/Shared/Movement/HumanGridMovement.cs)
- Snapshot handling bridge: [`Assets/Project/Scripts/Client/Systems/ClientRuntimeBridge.cs`](Assets/Project/Scripts/Client/Systems/ClientRuntimeBridge.cs)

### Bot Rendering: Snapshot Interpolation

- Incoming snapshots are buffered in timestamp order.
- Render time is delayed (`now - interpolationDelay`) to sample between snapshots.
- When the render time is ahead of latest snapshot, short extrapolation is used.
- Bot transforms are visually smoothed in client runtime presentation.

**Key scripts**
- Snapshot buffer and sampling: [`Assets/Project/Scripts/Client/Systems/InterpolationSystem.cs`](Assets/Project/Scripts/Client/Systems/InterpolationSystem.cs)
- Bot view sync and animation/facing updates: [`Assets/Project/Scripts/Client/Systems/ClientRuntimeBridge.cs`](Assets/Project/Scripts/Client/Systems/ClientRuntimeBridge.cs)

### Bot AI and Pathfinding

- Bot FSM progression: Idle -> FindNearestEgg -> Pathfinding -> Moving.
- Target selection chooses nearest non-collected egg.
- Pathfinding uses custom A* with open set / closed set and grid neighbors.

**Key scripts**
- Bot FSM: [`Assets/Project/Scripts/Server/Systems/BotController.cs`](Assets/Project/Scripts/Server/Systems/BotController.cs)
- Pathfinding core: [`Assets/Project/Scripts/Server/Systems/Pathfinding.cs`](Assets/Project/Scripts/Server/Systems/Pathfinding.cs)
- Grid graph and neighbors: [`Assets/Project/Scripts/Server/Systems/GridSystem.cs`](Assets/Project/Scripts/Server/Systems/GridSystem.cs)

---

## 4) Project Structure

```text
Assets/Project/Scripts/
|-- Client/
|   |-- Entities/
|   |   |-- PlayerView.cs
|   `-- Systems/
|       |-- ClientRuntimeBridge.cs
|       |-- ClientPlayerBootstrap.cs
|       |-- InputSystem.cs
|       |-- InterpolationSystem.cs
|       |-- IsometricPlayerCamera.cs
|       `-- MatchHudController.cs
|-- Server/
|   |-- Core/
|   |   |-- FakeNetwork.cs
|   |   `-- ServerSimulator.cs
|   `-- Systems/
|       |-- BotController.cs
|       |-- GridSystem.cs
|       `-- Pathfinding.cs
`-- Shared/
    |-- Config/
    |   `-- GameConfig.cs
    |-- Messages/
    |   |-- BaseMessage.cs
    |   |-- MessageSerializer.cs
    |   `-- PlayerStateMessage.cs
    |-- Models/
    |   |-- EggState.cs
    |   |-- GameState.cs
    |   `-- PlayerState.cs
    `-- Movement/
        `-- HumanGridMovement.cs
```

---

## 5) How to Play & Configuration

### Gameplay
- One human player is controlled by keyboard input (WASD / arrow mapping through Unity axis input).
- Remaining players are server-driven bots.
- Eggs are spawned by server authority and collected on overlap.
- Highest score at match end wins.

### Latency / Jitter Configuration

In `GameConfig`:
- `baseNetworkLatency`: baseline transport delay.
- `networkJitter`: random extra delay per message.
- `minUpdateInterval`, `maxUpdateInterval`: server snapshot send window (currently project-configurable, typically set to simulate high delay scenarios such as 1s-5s in this test context).

Runtime:
- `ClientRuntimeBridge.SetUserLatencyMilliseconds(...)` forwards user-configured latency into `FakeNetwork.SetUserLatency(...)`.

---

## 6) Current Status & Self-Evaluation

### Completed
- Server-authoritative simulation loop with delayed message transport.
- Clean domain models and explicit message contracts between modules.
- Custom grid-based A* bot navigation without external pathfinding libraries.
- Human prediction + reconciliation pipeline with input sequencing.
- Bot interpolation pipeline with snapshot buffering and render smoothing.
- Client-side predictive egg collection UX under high latency.

This implementation is designed to demonstrate strong fundamentals in gameplay networking architecture while keeping the code understandable, modular, and extensible for production-style evolution.