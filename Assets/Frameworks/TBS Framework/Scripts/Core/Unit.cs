﻿using UnityEngine;
using UnityEngine.UI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Collections;

/// <summary>
/// Base class for all units in the game.
/// </summary>
public abstract class Unit : MonoBehaviour
{
    Dictionary<Cell, List<Cell>> cachedPaths = null;
    /// <summary>
    /// UnitClicked event is invoked when user clicks the unit. 
    /// It requires a collider on the unit game object to work.
    /// </summary>
    public event EventHandler UnitClicked;
    /// <summary>
    /// UnitSelected event is invoked when user clicks on unit that belongs to him. 
    /// It requires a collider on the unit game object to work.
    /// </summary>
    public event EventHandler UnitSelected;
    /// <summary>
    /// UnitDeselected event is invoked when user click outside of currently selected unit's collider.
    /// It requires a collider on the unit game object to work.
    /// </summary>
    public event EventHandler UnitDeselected;
    /// <summary>
    /// UnitHighlighted event is invoked when user moves cursor over the unit. 
    /// It requires a collider on the unit game object to work.
    /// </summary>
    public event EventHandler UnitHighlighted;
    /// <summary>
    /// UnitDehighlighted event is invoked when cursor exits unit's collider. 
    /// It requires a collider on the unit game object to work.
    /// </summary>
    public event EventHandler UnitDehighlighted;
    /// <summary>
    /// UnitAttacked event is invoked when the unit is attacked.
    /// </summary>
    public event EventHandler<AttackEventArgs> UnitAttacked;
    /// <summary>
    /// UnitDestroyed event is invoked when unit's hitpoints drop below 0.
    /// </summary>
    public event EventHandler<AttackEventArgs> UnitDestroyed;
    /// <summary>
    /// UnitMoved event is invoked when unit moves from one cell to another.
    /// </summary>
    public event EventHandler<MovementEventArgs> UnitMoved;

    public UnitState UnitState { get; set; }
    public void SetState(UnitState state)
    {
        UnitState.MakeTransition(state);
    }

    /// <summary>
    /// A list of buffs that are applied to the unit.
    /// </summary>
    public List<Buff> Buffs { get; private set; }

    public int TotalHitPoints { get; private set; }
    protected int TotalMovementPoints;
    protected int TotalActionPoints;

    /// <summary>
    /// The cell grid game object.
    /// </summary>
    public CellGrid CellGrid;

    /// <summary>
    /// Cell that the unit is currently occupying.
    /// </summary>
    public Cell Cell { get; set; }

    public float ActionSpeed;
    public bool IsReady = false;
    public float Timer;
    public int HitPoints;
    public int AttackRange;
    public int AttackFactor;
    public int DefenceFactor;
    /// <summary>
    /// Determines how far on the grid the unit can move.
    /// </summary>
    public int MovementPoints;
    /// <summary>
    /// Determines speed of movement animation.
    /// </summary>
    public float MovementSpeed;
    /// <summary>
    /// Determines how many attacks unit can perform in one turn.
    /// </summary>
    public int ActionPoints;
    /// <summary>
    /// The object that contains the interactable actions that this unit can perform.
    /// </summary>
    public GameObject ActionMenu;
    /// <summary>
    /// The health bar game object.
    /// </summary>
    public GameObject HealthBar;
    /// <summary>
    /// The move timer game object.
    /// </summary>
    public GameObject MoveTimer;

    /// <summary>
    /// Indicates the player that the unit belongs to. 
    /// Should correspoond with PlayerNumber variable on Player script.
    /// </summary>
    public int PlayerNumber;

    /// <summary>
    /// Indicates if movement animation is playing.
    /// </summary>
    public bool isMoving { get; set; }

    private static DijkstraPathfinding _pathfinder = new DijkstraPathfinding();
    private static IPathfinding _fallbackPathfinder = new AStarPathfinding();

    /// <summary>
    /// Method called after object instantiation to initialize fields etc. 
    /// </summary>
    public virtual void Initialize()
    {
        Buffs = new List<Buff>();

        UnitState = new UnitStateNormal(this);

        CellGrid = (CellGrid)GameObject.Find("CellGrid").GetComponent("CellGrid");

        TotalHitPoints = HitPoints;
        TotalMovementPoints = MovementPoints;
        TotalActionPoints = ActionPoints;
        Timer = ActionSpeed;
    }
 
    protected virtual void OnMouseDown()
    {
        if (UnitClicked != null)
            UnitClicked.Invoke(this, new EventArgs());
    }
    protected virtual void OnMouseEnter()
    {
        if (UnitHighlighted != null)
            UnitHighlighted.Invoke(this, new EventArgs());
    }
    protected virtual void OnMouseExit()
    {
        if (UnitDehighlighted != null)
            UnitDehighlighted.Invoke(this, new EventArgs());
    }

    /// <summary>
    /// Method is called at the start of each turn.
    /// </summary>
    public virtual void OnTurnStart()
    {
        if(!IsReady) {
            return;
        }
        MovementPoints = TotalMovementPoints;
        ActionPoints = TotalActionPoints;

        SetState(new UnitStateMarkedAsFriendly(this));
    }
    /// <summary>
    /// Method is called at the end of each turn.
    /// </summary>
    public virtual void OnTurnEnd()
    {
        cachedPaths = null;
        Buffs.FindAll(b => b.Duration == 0).ForEach(b => { b.Undo(this); });
        Buffs.RemoveAll(b => b.Duration == 0);
        Buffs.ForEach(b => { b.Duration--; });

        SetState(new UnitStateNormal(this));
    }

    /// <summary>
    /// Method is called when unit becomes available to activate.
    /// </summary>
    public virtual void Activate(CellGrid _cellGrid, List<Unit> myUnits)
    {
    }

    /// <summary>
    /// Method is called when units HP drops below 1.
    /// </summary>
    protected virtual void OnDestroyed()
    {
        Cell.IsTaken = false;
        MarkAsDestroyed();
        Destroy(gameObject);
    }

    /// <summary>
    /// Method is called when units HP drops below 1.
    /// </summary>
    public virtual void UpdateTimerBar() {
        var timerGraphic = MoveTimer?.GetComponent<Image>();

        if (timerGraphic != null)
        {
            timerGraphic.transform.localScale = new Vector3((float)((float)Timer / (float)ActionSpeed), 1, 1);
            timerGraphic.color = Color.Lerp(Color.blue, Color.red,
                (float)((float)Timer / (float)ActionSpeed));
        }
    }

    /// <summary>
    /// Method is called when unit is selected.
    /// </summary>
    public virtual void OnUnitSelected()
    {
        if(!IsReady) {
            return;
        }

        ActionMenu.SetActive(true);
        CellGrid.IsPaused = true;

        SetState(new UnitStateMarkedAsSelected(this));
        if (UnitSelected != null)
        {
            UnitSelected.Invoke(this, new EventArgs());
        }
            
    }
    /// <summary>
    /// Method is called when unit is deselected.
    /// </summary>
    public virtual void OnUnitDeselected()
    {
        ActionMenu.SetActive(false);
        CellGrid.IsPaused = false;

        SetState(new UnitStateMarkedAsFriendly(this));
        if (UnitDeselected != null)
            UnitDeselected.Invoke(this, new EventArgs());
    }

    /// <summary>
    /// Method indicates if it is possible to attack unit given as parameter, 
    /// from cell given as second parameter.
    /// </summary>
    public virtual bool IsUnitAttackable(Unit other, Cell sourceCell, int range = 1)
    {
        if (sourceCell.GetDistance(other.Cell) <= range)
            return true;

        return false;
    }

    /// <summary>
    /// Method deals damage to unit given as parameter.
    /// </summary>
    public virtual void DealDamage(Unit other)
    {
        if (isMoving)
            return;
        if (ActionPoints == 0)
            return;

        // Commenting this out may create bugs.
        // if (!IsUnitAttackable(other, Cell, AttackRange))
        //     return;

        MarkAsAttacking(other);
        ActionPoints--;
        other.Defend(this, AttackFactor);

        if (ActionPoints == 0)
        {
            SetState(new UnitStateMarkedAsFinished(this));
            MovementPoints = 0;
        }  
    }

    /// <summary>
    /// Attacking unit calls Defend method on defending unit. 
    /// </summary>
    protected virtual void Defend(Unit other, int damage)
    {
        MarkAsDefending(other);
        //Damage is calculated by subtracting attack factor of attacker and defence factor of defender. 
        //If result is below 1, it is set to 1. This behaviour can be overridden in derived classes.
        HitPoints -= Mathf.Clamp(damage - DefenceFactor, 1, damage);  
        if (UnitAttacked != null)
            UnitAttacked.Invoke(this, new AttackEventArgs(other, this, damage));

        if (HitPoints <= 0)
        {
            if (UnitDestroyed != null)
                UnitDestroyed.Invoke(this, new AttackEventArgs(other, this, damage));
            OnDestroyed();
        }
    }

    /// <summary>
    /// Moves the unit to destinationCell along the path.
    /// </summary>
    public virtual void Move(Cell destinationCell, List<Cell> path, CellGrid cellGrid)
    {
        if (isMoving)
            return;

        var totalMovementCost = path.Sum(h => h.MovementCost);
        if (MovementPoints < totalMovementCost)
            return;

        MovementPoints -= totalMovementCost;

        Cell.IsTaken = false;
        Cell = destinationCell;
        destinationCell.IsTaken = true;

        if (MovementSpeed > 0)
            StartCoroutine(MovementAnimation(path, cellGrid));
        else
            transform.position = Cell.transform.position;

        if (UnitMoved != null){
            UnitMoved.Invoke(this, new MovementEventArgs(Cell, destinationCell, path));   
        }
    }
    protected virtual IEnumerator MovementAnimation(List<Cell> path, CellGrid cellGrid)
    {
        isMoving = true;
        path.Reverse();
        foreach (var cell in path)
        {
            Vector3 destination_pos = new Vector3(cell.transform.localPosition.x, transform.localPosition.y, cell.transform.localPosition.z);
            while (transform.localPosition != destination_pos)
            {
                transform.localPosition = Vector3.MoveTowards(transform.localPosition, destination_pos, Time.deltaTime * MovementSpeed);
                yield return 0;
            }
        }
        
        if(!ActionMenu) {
            isMoving = false;
            yield break;
        }

        // Set ability buttons as interactable based on whether or not enemies in range. 
        foreach(Transform child in ActionMenu.transform)
        {
            UnitAbility unitAbility = (UnitAbility)child.gameObject.GetComponent("UnitAbility");
            Button button = (Button)child.gameObject.GetComponent("Button");
            int attackRange = unitAbility.AttackRange;
            bool hasUnitInRange = false;

            foreach (var currentUnit in cellGrid.Units)
            {
                if (currentUnit.PlayerNumber.Equals(PlayerNumber))
                    continue;
            
                if (IsUnitAttackable(currentUnit, Cell, attackRange))
                {
                    hasUnitInRange = true;
                    break;
                }
            }

            button.interactable = hasUnitInRange;
        }

        isMoving = false;
        MarkAsFinished();
    }

    ///<summary>
    /// Method indicates if unit is capable of moving to cell given as parameter.
    /// </summary>
    public virtual bool IsCellMovableTo(Cell cell)
    {
        return !cell.IsTaken;
    }
    /// <summary>
    /// Method indicates if unit is capable of moving through cell given as parameter.
    /// </summary>
    public virtual bool IsCellTraversable(Cell cell)
    {
        return !cell.IsTaken;
    }
    /// <summary>
    /// Method returns all cells that the unit is capable of moving to.
    /// </summary>
    public HashSet<Cell> GetAvailableDestinations(List<Cell> cells)
    {
        cachedPaths = new Dictionary<Cell, List<Cell>>();
        
        var paths = cachePaths(cells);
        foreach (var key in paths.Keys)
        {
            if (!IsCellMovableTo(key))
                continue;
            var path = paths[key];

            var pathCost = path.Sum(c => c.MovementCost);
            if (pathCost <= MovementPoints)
            {
                cachedPaths.Add(key, path);
            }
        }
        return new HashSet<Cell>(cachedPaths.Keys);
    }

    private Dictionary<Cell, List<Cell>> cachePaths(List<Cell> cells)
    {
        var edges = GetGraphEdges(cells);
        var paths = _pathfinder.findAllPaths(edges, Cell);
        return paths;
    }

    public List<Cell> FindPath(List<Cell> cells, Cell destination)
    {
        if(cachedPaths != null && cachedPaths.ContainsKey(destination))
        {
            return cachedPaths[destination];
        }
        else
        {
            return _fallbackPathfinder.FindPath(GetGraphEdges(cells), Cell, destination);
        }
    }
    /// <summary>
    /// Method returns graph representation of cell grid for pathfinding.
    /// </summary>
    protected virtual Dictionary<Cell, Dictionary<Cell, int>> GetGraphEdges(List<Cell> cells)
    {
        Dictionary<Cell, Dictionary<Cell, int>> ret = new Dictionary<Cell, Dictionary<Cell, int>>();
        foreach (var cell in cells)
        {
            if (IsCellTraversable(cell) || cell.Equals(Cell))
            {
                ret[cell] = new Dictionary<Cell, int>();
                foreach (var neighbour in cell.GetNeighbours(cells).FindAll(IsCellTraversable))
                {
                    ret[cell][neighbour] = neighbour.MovementCost;
                }
            }
        }
        return ret;
    }

    /// <summary>
    /// Gives visual indication that the unit is under attack.
    /// </summary>
    /// <param name="other"></param>
    public abstract void MarkAsDefending(Unit other);
    /// <summary>
    /// Gives visual indication that the unit is attacking.
    /// </summary>
    /// <param name="other"></param>
    public abstract void MarkAsAttacking(Unit other);
    /// <summary>
    /// Gives visual indication that the unit is destroyed. It gets called right before the unit game object is
    /// destroyed, so either instantiate some new object to indicate destruction or redesign Defend method. 
    /// </summary>
    public abstract void MarkAsDestroyed();

    /// <summary>
    /// Method marks unit as current players unit.
    /// </summary>
    public abstract void MarkAsFriendly();
    /// <summary>
    /// Method mark units to indicate user that the unit is in range and can be attacked.
    /// </summary>
    public abstract void MarkAsReachableEnemy();
    /// <summary>
    /// Method marks unit as currently selected, to distinguish it from other units.
    /// </summary>
    public abstract void MarkAsSelected();
    /// <summary>
    /// Method marks unit to indicate user that he can't do anything more with it this turn.
    /// </summary>
    public abstract void MarkAsFinished();
    /// <summary>
    /// Method returns the unit to its base appearance
    /// </summary>
    public abstract void UnMark();
}

public class MovementEventArgs : EventArgs
{
    public Cell OriginCell;
    public Cell DestinationCell;
    public List<Cell> Path;

    public MovementEventArgs(Cell sourceCell, Cell destinationCell, List<Cell> path)
    {
        OriginCell = sourceCell;
        DestinationCell = destinationCell;
        Path = path;
    }
}
public class AttackEventArgs : EventArgs
{
    public Unit Attacker;
    public Unit Defender;

    public int Damage;

    public AttackEventArgs(Unit attacker, Unit defender, int damage)
    {
        Attacker = attacker;
        Defender = defender;

        Damage = damage;
    }
}
public class UnitCreatedEventArgs : EventArgs
{
    public Transform unit;

    public UnitCreatedEventArgs(Transform unit)
    {
        this.unit = unit;
    }
}
