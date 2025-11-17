# Caligo

A voxel engine in C# (.NET 10) with procedural world generation using OpenGL rendering.

## Description

Caligo generates infinite terrain using chunk-based world generation (16³ voxel chunks). Multiple generators create different terrain types (flat, hilly, layered) using custom noise algorithms. The engine uses multi-threaded generation, greedy meshing, and GPU-side rendering optimizations including texture atlasing and indirect draw buffers.

World data is stored using Z-order curves for cache-efficient access. Chunks are generated asynchronously on worker threads and automatically loaded/unloaded based on camera position.

## Content System

Content is organized into **modules** stored in the `modules/` directory. Each module contains:

```
modules/mymodule/
├── config.json          # Module metadata
├── blocks/              # Block definitions (JSON)
├── textures/            # Block textures (PNG)
└── blockmodels/         # Block geometry models (JSON)
```

Modules are loaded at runtime with namespaced identifiers (`core:stone`, `mymod:block`). Add new content by creating a module directory—no recompilation needed.

## Building

Requires .NET 10.0 SDK and OpenGL 4.0+.

```bash
dotnet build
dotnet run --project Caligo.Client
```

**Controls**: WASD to move, mouse to look, Shift for speed boost, scroll to adjust speed.

