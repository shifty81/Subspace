# Space Ship Assets - Integration Action Items

## Quick Reference: What to Do Next

This document provides actionable steps for integrating the reviewed space ship assets into Subspace.

---

## 🚀 Phase 1: Quick Wins (Recommended to Start Here)

### 1. Integrate Background Nebulae and Stars
**Status**: ✅ Ready to implement (PNG files, no conversion needed)  
**Effort**: 2-4 hours  
**Files**: `Stars-Nebulae/Stars.png`, `Nebula1.png`, `Nebula2.png`, `Nebula3.png`

**Action Steps:**
```bash
# 1. Copy files to Content folder
cp "Space ship assets to be added/Stars-Nebulae/"*.png Content/Sprites/

# 2. Add to Content.mgcb
# Add build entries for each nebula PNG (similar to existing sprites)

# 3. Update Nebula.cs
# - Load nebula textures in LoadContent
# - Render textures instead of/in addition to procedural generation
# - Add parallax scrolling for depth

# 4. Update Starfield.cs
# - Option: Use Stars.png as tiled background
# - Or: Keep procedural stars, add Stars.png as secondary layer
```

**Code Changes Needed:**
- `Content/Content.mgcb`: Add 4 new texture entries
- `Nebula.cs`: Load and render nebula textures
- `Starfield.cs`: Optional - integrate Stars.png
- `Game1.cs`: Pass textures to background systems

---

### 2. Add Example Ships as Enemy Sprites
**Status**: ✅ Ready to implement (PNG files, no conversion needed)  
**Effort**: 4-6 hours  
**Files**: `Example_ships/1.png` through `13.png` (and B variants)

**Action Steps:**
```bash
# 1. Copy files to Content folder
mkdir -p Content/Sprites/EnemyShips
cp "Space ship assets to be added/Example_ships/"*.png Content/Sprites/EnemyShips/

# 2. Add to Content.mgcb
# Add build entries for each enemy ship PNG

# 3. Create enemy ship sprite system
# Modify Ship.cs or create EnemyShipRenderer.cs
```

**Code Changes Needed:**

**Option A: Simple Approach** (Ship.cs modification)
```csharp
// Add to Ship class
public string? PrerenderedSprite { get; set; }
public Texture2D? PrerenderedTexture { get; set; }

// In Draw method, check if PrerenderedSprite is set
if (PrerenderedTexture != null)
{
    // Draw prerendered sprite instead of components
    spriteBatch.Draw(PrerenderedTexture, position, ...);
}
else
{
    // Draw components as normal
}
```

**Option B: Separate Enemy Type**
```csharp
// Create new EnemyShipSprite.cs
public class EnemyShipSprite
{
    public Texture2D Texture { get; set; }
    public float X, Y, Angle;
    public int Health;
    // ... weapon firing points, etc.
}
```

**Random Assignment:**
```csharp
// In enemy creation, randomly assign sprite
var spriteNumber = random.Next(1, 14); // 1-13
var variant = random.Next(0, 2) == 0 ? "" : "B";
enemy.PrerenderedSprite = $"EnemyShips/{spriteNumber}{variant}";
```

---

## 🔧 Phase 2: PSD Conversion (Requires Tools)

### 3. Convert Alien Ship PSDs to PNG
**Status**: ⚠️ Requires PSD conversion  
**Effort**: 2 hours conversion + 4 hours integration  
**Files**: `Alien-Ships/*.psd` (10 files)

**Tools Required:**
- **Option A**: GIMP (free) - `sudo apt install gimp`
- **Option B**: ImageMagick - `sudo apt install imagemagick`
- **Option C**: Photoshop (if available)
- **Option D**: Online converter (e.g., photopea.com)

**Conversion Steps (GIMP):**
```bash
# Install GIMP
sudo apt install gimp

# For batch conversion, ImageMagick is simpler (see below)
# GIMP can be used interactively to open and export PSDs to PNG
# File > Export As > Choose PNG format > Export
```

**Or ImageMagick (simpler):**
```bash
# Install ImageMagick
sudo apt install imagemagick

# Convert all PSDs
for file in "Space ship assets to be added/Alien-Ships/"*.psd; do
    convert "$file" -flatten "${file%.psd}.png"
done
```

**Integration Steps:**
```bash
# Copy converted PNGs to Content
mkdir -p Content/Sprites/AlienShips
cp "Space ship assets to be added/Alien-Ships/"*.png Content/Sprites/AlienShips/

# Add to Content pipeline
# Create AlienShip enemy type with different behavior
```

---

### 4. Convert and Integrate Engine Exhaust
**Status**: ⚠️ Requires PSD conversion + animation system  
**Effort**: 2 hours conversion + 6-10 hours animation system  
**Files**: `Engine_exhaust/*.psd` (3 files)

**Step 1: Convert PSDs to Sprite Sheets**
```bash
# Convert PSDs to PNG
convert "Space ship assets to be added/Engine_exhaust/Engine_exhaust1_frames.psd" -flatten Engine_exhaust1.png
convert "Space ship assets to be added/Engine_exhaust/Engine_exhaust2_frames.psd" -flatten Engine_exhaust2.png
convert "Space ship assets to be added/Engine_exhaust/Engine_exhaust3_frames.psd" -flatten Engine_exhaust3.png

# Inspect to determine frame layout (may need to export layers separately)
```

**Step 2: Create Animation System**
```csharp
// Create new SpriteAnimation.cs
public class SpriteAnimation
{
    public Texture2D SpriteSheet { get; set; }
    public int FrameCount { get; set; }
    public int FrameWidth { get; set; }
    public int FrameHeight { get; set; }
    public int FramesPerRow { get; set; }
    public float FrameDuration { get; set; }
    private float currentTime;
    private int currentFrame;

    public Rectangle GetCurrentFrameRect()
    {
        int x = (currentFrame % FramesPerRow) * FrameWidth;
        int y = (currentFrame / FramesPerRow) * FrameHeight;
        return new Rectangle(x, y, FrameWidth, FrameHeight);
    }

    public void Update(float deltaTime)
    {
        currentTime += deltaTime;
        if (currentTime >= FrameDuration)
        {
            currentTime = 0;
            currentFrame = (currentFrame + 1) % FrameCount;
        }
    }
}
```

**Step 3: Integrate with Engine Components**
```csharp
// In Component.cs or ComponentRenderer
if (component.Type == ComponentType.ENGINE && component.IsPowered)
{
    // Draw engine exhaust animation behind engine
    var exhaustRect = engineExhaustAnimation.GetCurrentFrameRect();
    spriteBatch.Draw(engineExhaustTexture, exhaustPosition, exhaustRect, ...);
}
```

---

### 5. Extract and Integrate Muzzle Flashes
**Status**: ⚠️ Requires PSD extraction  
**Effort**: 2 hours extraction + 2-4 hours integration  
**Files**: `Muzzle_flashes/Muzzle_flashes.psd`

**Extraction Steps:**
```bash
# Convert PSD (may contain multiple flashes)
convert "Space ship assets to be added/Muzzle_flashes/Muzzle_flashes.psd" -flatten Muzzle_flashes_full.png

# If PSD has layers, export each layer:
# (This requires manual work in GIMP or Photoshop)
# Export as: muzzle_flash_1.png, muzzle_flash_2.png, etc.
```

**Integration Steps:**
```csharp
// Add to Projectile.cs or create MuzzleFlash particle effect
public class MuzzleFlash
{
    public Texture2D Texture { get; set; }
    public Vector2 Position { get; set; }
    public float Lifetime { get; set; } // Very short, like 0.1 seconds
    public float Angle { get; set; }
}

// In weapon fire code:
if (weapon.Fire())
{
    // Create muzzle flash
    muzzleFlashes.Add(new MuzzleFlash 
    { 
        Position = weapon.Position, 
        Angle = weapon.Angle,
        Lifetime = 0.1f,
        Texture = muzzleFlashTexture
    });
}
```

---

## 🔍 Phase 3: Investigation Required

### 6. Inspect Spaceship-Parts.psd
**Status**: 🔍 Needs manual inspection  
**Effort**: 1-2 hours investigation  
**Files**: `Spaceship-Parts/Spaceship-Parts.psd`

**Investigation Checklist:**

```bash
# 1. Open in GIMP or Photoshop
gimp "Space ship assets to be added/Spaceship-Parts/Spaceship-Parts.psd"

# Or use online tool: https://www.photopea.com/
```

**Questions to Answer:**
- [ ] What components are included?
  - Core, engines, weapons, armor?
  - Do they match current component types?
- [ ] What are the dimensions of each part?
  - Are they 32x32 pixels (current grid size)?
  - Larger? Smaller?
- [ ] Are layers separated?
  - Can individual parts be extracted?
  - Or is it a single flattened image?
- [ ] What is the quality/style?
  - Better than current assets?
  - Compatible style?
- [ ] Are there variants?
  - Damage states?
  - Team colors?
  - Different types?

**If Parts Are Usable:**
```bash
# Export each layer as separate PNG
# Name according to component type: component_weapon_railgun.png, etc.

# Add to Content pipeline
# Either replace existing components or add as variants
```

**If Parts Are Not Compatible:**
- Document why (wrong size, wrong style, etc.)
- Keep as reference/inspiration only

---

## 📋 Implementation Priority

### Must Do First (Blocking Others)
1. ✅ **Review SPACE_SHIP_ASSETS_REVIEW.md** - Read full analysis
2. ✅ **Install conversion tools** - GIMP or ImageMagick
3. 🔍 **Inspect Spaceship-Parts.psd** - Could change entire plan

### High Priority (Easy Wins)
4. ✅ **Integrate Stars-Nebulae** - Immediate visual upgrade
5. ✅ **Add Example_ships sprites** - More enemy variety

### Medium Priority (More Work)
6. ⚠️ **Convert Alien-Ships** - After tool setup
7. ⚠️ **Engine exhaust animations** - Requires animation system

### Lower Priority (Polish)
8. ⚠️ **Muzzle flashes** - Nice to have
9. 📊 **Performance optimization** - After integration

---

## 🛠️ Tools and Commands Reference

### Install Required Tools (Ubuntu/Debian)
```bash
# GIMP for PSD editing
sudo apt update
sudo apt install gimp

# ImageMagick for batch conversion
sudo apt install imagemagick

# Check installations
gimp --version
convert --version
```

### Quick PSD to PNG Conversion
```bash
# Single file
convert input.psd -flatten output.png

# Batch conversion
for f in *.psd; do convert "$f" -flatten "${f%.psd}.png"; done

# Preserve layers (exports as multiple PNGs)
convert input.psd output.png  # Creates output-0.png, output-1.png, etc.
```

### Add Asset to Content Pipeline
```bash
# Edit Content/Content.mgcb, add:

#begin Sprites/your_asset.png
/importer:TextureImporter
/processor:TextureProcessor
/processorParam:ColorKeyColor=255,0,255,255
/processorParam:ColorKeyEnabled=True
/processorParam:GenerateMipmaps=False
/processorParam:PremultiplyAlpha=True
/processorParam:ResizeToPowerOfTwo=False
/processorParam:MakeSquare=False
/processorParam:TextureFormat=Color
/build:Sprites/your_asset.png
```

### Load in Game Code
```csharp
// In Game1.cs LoadContent() or similar
var texture = Content.Load<Texture2D>("Sprites/your_asset");
```

---

## ⚠️ Important Notes

### Before You Start
1. **Backup the project** - `git commit -am "Before asset integration"`
2. **Test incrementally** - Add one asset type at a time
3. **Monitor performance** - Check frame rate after adding assets
4. **Keep originals** - Don't delete PSD files after conversion

### Common Pitfalls
- ❌ Don't add all assets at once - harder to debug
- ❌ Don't forget to add to Content.mgcb - assets won't load
- ❌ Don't assume PSD structure - inspect first
- ❌ Don't skip testing - verify each asset loads correctly

### Success Criteria
- ✅ All assets load without errors
- ✅ Game runs at 60 FPS (no performance regression)
- ✅ Transparency renders correctly
- ✅ Assets fit the game's visual style
- ✅ File sizes are reasonable (<10MB total)

---

## 📞 Need Help?

### If Assets Don't Load
1. Check Content.mgcb syntax
2. Verify file paths match exactly
3. Rebuild Content project (`dotnet build`)
4. Check for typos in asset names

### If Performance Drops
1. Check texture sizes (may need to downscale)
2. Use texture atlases for many small sprites
3. Profile to find bottleneck

### If Conversion Fails
1. Try different tool (GIMP vs ImageMagick)
2. Use online converter as backup
3. Check PSD file isn't corrupted

---

**Last Updated**: December 19, 2025  
**Related Document**: SPACE_SHIP_ASSETS_REVIEW.md  
**Project**: Subspace
