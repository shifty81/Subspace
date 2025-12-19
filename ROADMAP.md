# Subspace Development Roadmap

This document outlines the planned features and improvements for the Subspace game project.

## Current Status

✅ **Phase 0: Basic Prototype** (Complete)
- Basic ship building with modular components
- Simple combat mechanics with projectiles
- AI-controlled enemy ships
- Grid-based ship editor
- Power management system

✅ **Phase 0.5: Development Tools** (Complete)
- Shell launcher scripts for easy game startup
- Cross-platform support (Linux/macOS/Windows)
- Automated dependency installation

## Future Development Phases

### Phase 1: Visual Enhancements 🎨

Goal: Make the game visually appealing with professional graphics

#### 1.1 Space Environment
- [x] Enhanced starfield background with parallax scrolling
- [x] Nebula and space dust effects
- [ ] Dynamic lighting and ambient effects
- [ ] Asteroid fields and space debris

#### 1.2 Particle Systems
- [ ] Weapon fire particle effects (lasers, projectiles)
- [ ] Explosion animations with debris
- [ ] Engine thrust particles
- [ ] Shield impact effects
- [ ] Damage sparks and fire effects

#### 1.3 Ship Graphics
- [ ] Improved component sprites with more detail
- [ ] Component damage visual states (intact → damaged → destroyed)
- [ ] Ship hull textures and materials
- [ ] Glow effects for powered systems
- [ ] Weapon charge-up animations

### Phase 2: User Interface & HUD 📊

Goal: Create a professional, informative interface

#### 2.1 Combat HUD
- [ ] Health/shield status bars
- [ ] Power generation/consumption display
- [ ] Weapon status and cooldowns
- [ ] Target lock indicator
- [ ] Damage indicators (directional)

#### 2.2 Ship Management UI
- [ ] Fleet/ship selection bar at bottom
- [ ] Component status panel
- [ ] Resource counters (if economy added)
- [ ] Minimap showing battlefield overview
- [ ] Tactical zoom controls

#### 2.3 Mission System UI
- [ ] Mission/objective panel
- [ ] Quest tracker
- [ ] Bounty/reward display
- [ ] Wave counter for survival mode

### Phase 3: Gameplay Features ⚔️

Goal: Add depth and variety to gameplay

#### 3.1 Extended Combat
- [ ] Additional weapon types (missiles, torpedoes, point defense)
- [ ] Shield systems with recharge mechanics
- [ ] Critical hits and component-specific damage
- [ ] Weapon targeting priorities
- [ ] Formation combat for multiple ships

#### 3.2 Ship Building
- [ ] Ship templates and presets
- [ ] Save/load custom ship designs
- [ ] Symmetry tools for building
- [ ] Component rotation
- [ ] Ship validation and balance warnings

#### 3.3 Progression System
- [ ] Campaign mode with story missions
- [ ] Resource gathering and economy
- [ ] Ship upgrades and tech tree
- [ ] Unlock new components through progression
- [ ] Player leveling and skills

### Phase 4: Advanced Features 🚀

Goal: Add features that increase replayability

#### 4.1 Game Modes
- [ ] Survival/wave mode
- [ ] Mission-based campaign
- [ ] Sandbox/creative mode
- [ ] Challenge scenarios
- [ ] Boss battles

#### 4.2 AI Improvements
- [ ] Advanced enemy tactics and formations
- [ ] Different enemy ship types and behaviors
- [ ] Friendly AI wingmen
- [ ] Dynamic difficulty adjustment

#### 4.3 Content & Polish
- [ ] Multiple factions with unique ship designs
- [ ] Sound effects and background music
- [ ] Voice-overs or audio cues
- [ ] Achievements and statistics tracking
- [ ] Settings menu (graphics, audio, controls)

### Phase 5: Technical Improvements 🔧

#### 5.1 Engine Migration (Under Consideration)
- [ ] **Evaluate MonoGame as alternative to Pygame**
  - Better performance for complex graphics
  - Cross-platform deployment (including mobile)
  - More robust rendering pipeline
  - Better suited for commercial-quality games
  - C# instead of Python (better for game development at scale)
- [ ] Prototype core mechanics in MonoGame
- [ ] Migration plan if benefits justify the effort
- [ ] Asset pipeline setup

#### 5.2 Performance Optimization
- [ ] Spatial partitioning for collision detection
- [ ] Object pooling for projectiles/particles
- [ ] Optimized rendering with culling
- [ ] Multi-threading for AI and physics

#### 5.3 Multiplayer (Long-term)
- [ ] Local co-op/versus mode
- [ ] Network multiplayer architecture
- [ ] Lobby and matchmaking system
- [ ] Server-client synchronization

## Technology Considerations

### Current Stack
- **Language**: Python 3.7+
- **Framework**: Pygame 2.5+
- **Pros**: Easy to develop, rapid prototyping, good for learning
- **Cons**: Limited performance, not ideal for complex graphics

### Potential Migration to MonoGame
- **Language**: C#
- **Framework**: MonoGame (XNA successor)
- **Pros**: 
  - Professional game engine used by successful indie games
  - Better performance and graphics capabilities
  - Cross-platform (Desktop, Mobile, Consoles)
  - Extensive documentation and community
  - More suitable for commercial release
- **Cons**: 
  - Complete rewrite required
  - Steeper learning curve
  - More complex setup
- **Decision Point**: After Phase 1-2 completion, evaluate if current engine meets needs

## Contributing

Interested in contributing to any of these features? Check out [CONTRIBUTING.md](CONTRIBUTING.md) for guidelines.

Priority items and active development can be tracked in the GitHub Issues and Projects tabs.

## Timeline

This is a living roadmap. Features may be added, removed, or reprioritized based on:
- Community feedback
- Technical feasibility
- Developer availability
- Engine capabilities

**Last Updated**: 2025-12-19
