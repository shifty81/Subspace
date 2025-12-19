using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Subspace;

/// <summary>
/// Represents a single crew member
/// </summary>
public class CrewMember
{
    public int Id { get; set; }
    public float X { get; set; }
    public float Y { get; set; }
    public Component? AssignedComponent { get; set; }
    public Component? TargetComponent { get; set; }
    public string State { get; set; } = "idle"; // idle, walking, working
    public float WorkProgress { get; set; } = 0f;
    public Color CrewColor { get; set; }

    // Path finding
    private List<Point> currentPath = new List<Point>();
    private int pathIndex = 0;
    private const float MOVE_SPEED_CORRIDOR = 100f; // pixels per second in corridors
    private const float MOVE_SPEED_ROOM = 50f; // pixels per second in rooms

    public CrewMember(int id, float x, float y)
    {
        Id = id;
        X = x;
        Y = y;
        // Random crew color for visual variety
        Random rand = new Random(id);
        CrewColor = new Color(
            150 + rand.Next(100),
            150 + rand.Next(100),
            150 + rand.Next(100)
        );
    }

    public void Update(float dt, Ship ship)
    {
        if (TargetComponent != null && AssignedComponent != TargetComponent)
        {
            // Move towards target
            MoveToComponent(dt, ship, TargetComponent);
        }
        else if (AssignedComponent != null)
        {
            // Work at assigned component
            State = "working";
            WorkProgress += dt;
        }
        else
        {
            State = "idle";
        }
    }

    private void MoveToComponent(float dt, Ship ship, Component target)
    {
        State = "walking";
        
        // Calculate target position
        float targetX = ship.X + (target.GridX - ship.GridWidth / 2f) * Config.GRID_SIZE;
        float targetY = ship.Y + (target.GridY - ship.GridHeight / 2f) * Config.GRID_SIZE;

        // Simple direct movement (can be enhanced with A* pathfinding)
        float dx = targetX - X;
        float dy = targetY - Y;
        float distance = (float)Math.Sqrt(dx * dx + dy * dy);

        if (distance < 5f)
        {
            // Reached target
            X = targetX;
            Y = targetY;
            AssignedComponent = target;
            TargetComponent = null;
            State = "working";
        }
        else
        {
            // Move towards target
            float speed = MOVE_SPEED_ROOM; // Default to room speed
            // TODO: Check if in corridor for speed bonus
            
            float vx = (dx / distance) * speed * dt;
            float vy = (dy / distance) * speed * dt;
            X += vx;
            Y += vy;
        }
    }

    public void AssignTo(Component component)
    {
        TargetComponent = component;
        WorkProgress = 0f;
    }

    public void Render(SpriteBatch spriteBatch, Texture2D pixelTexture, float cameraX, float cameraY)
    {
        int screenX = (int)(X - cameraX);
        int screenY = (int)(Y - cameraY);

        // Draw crew member as a small circle
        int radius = 3;
        for (int y = -radius; y <= radius; y++)
        {
            for (int x = -radius; x <= radius; x++)
            {
                if (x * x + y * y <= radius * radius)
                {
                    spriteBatch.Draw(pixelTexture, new Rectangle(screenX + x, screenY + y, 1, 1), CrewColor);
                }
            }
        }

        // Draw state indicator
        Color stateColor = State switch
        {
            "working" => Color.Green,
            "walking" => Color.Yellow,
            _ => Color.Gray
        };
        spriteBatch.Draw(pixelTexture, new Rectangle(screenX - 1, screenY - 6, 2, 2), stateColor);
    }
}

/// <summary>
/// Manages all crew on a ship
/// </summary>
public class CrewManager
{
    private List<CrewMember> crew = new List<CrewMember>();
    private int nextCrewId = 0;
    private Ship ship;

    public CrewManager(Ship ship)
    {
        this.ship = ship;
    }

    public void AddCrew(int count, float shipX, float shipY)
    {
        for (int i = 0; i < count; i++)
        {
            crew.Add(new CrewMember(nextCrewId++, shipX, shipY));
        }
    }

    public void Update(float dt)
    {
        foreach (var crewMember in crew)
        {
            crewMember.Update(dt, ship);
        }

        // Auto-assign idle crew to components that need them
        AssignIdleCrew();
    }

    private void AssignIdleCrew()
    {
        var idleCrew = crew.Where(c => c.State == "idle" && c.AssignedComponent == null).ToList();
        
        // Find components that need crew
        foreach (var component in ship.Components)
        {
            // Components that need crew: reactors, weapons, engines
            if (component.ComponentType == ComponentType.POWER ||
                component.ComponentType == ComponentType.WEAPON_LASER ||
                component.ComponentType == ComponentType.WEAPON_CANNON ||
                component.ComponentType == ComponentType.ENGINE)
            {
                // Check if component already has crew
                var assignedCrew = crew.Where(c => c.AssignedComponent == component).Count();
                
                // Most components need 1 crew, reactors need 2
                int requiredCrew = component.ComponentType == ComponentType.POWER ? 2 : 1;
                
                if (assignedCrew < requiredCrew && idleCrew.Count > 0)
                {
                    var crewToAssign = idleCrew[0];
                    crewToAssign.AssignTo(component);
                    idleCrew.RemoveAt(0);
                }
            }
        }
    }

    public void Render(SpriteBatch spriteBatch, Texture2D pixelTexture, float cameraX, float cameraY)
    {
        foreach (var crewMember in crew)
        {
            crewMember.Render(spriteBatch, pixelTexture, cameraX, cameraY);
        }
    }

    public int GetTotalCrew() => crew.Count;
    public int GetIdleCrew() => crew.Count(c => c.State == "idle");
    public int GetWorkingCrew() => crew.Count(c => c.State == "working");
}
