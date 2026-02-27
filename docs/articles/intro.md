# Introduction

Welcome to **WitShells** — a growing library of reusable Unity scripts and packages.

## Goals

- Provide clean, documented implementations of classic design patterns adapted for Unity.
- Ship self-contained UPM packages that drop into any Unity project with zero friction.
- Keep every class small, focused, and generic enough to be useful beyond Unity.

## Repository Layout

```
Assets/
└── WitShells/
    ├── DesignPatterns/      # Core patterns (Observer, Pool, StateMachine …)
    ├── CanvasDrawTool/      # Runtime canvas draw utilities
    ├── DialogsManager/      # UI dialog lifecycle
    ├── McqUI/               # MCQ question UI
    ├── ShootingSystem/      # Modular shooting
    ├── TankControls/        # Tank vehicle controls
    ├── ThirdPersonControl/  # Third-person character
    ├── WebSocket/           # WebSocket client
    └── …
```

## Namespace Convention

All public types live under the `WitShells` root namespace:

| Namespace | Contents |
|-----------|----------|
| `WitShells.DesignPatterns.Core` | Design pattern base classes |
| `WitShells.DesignPatterns` | Layout helpers, draggable system, logger |
