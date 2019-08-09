using UnityEngine;
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
    Animator animator;
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
    public int StrengthPoints;
    public int StrengthPointsMaximum;
    public int SpeedPoints;
    public int SpeedPointsMaximum;
    public int CunningPoints;
    public int CunningPointsMaximum;
    public int AttackRange;
    public int AttackFactor;
    public int StrengthCost;
    public int SpeedCost;
    public int CunningCost;
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
    /// Whether unit is charging abilities.
    /// </summary>
    public bool IsCharging;
    /// <summary>
    /// Time to charge more powa!
    /// </summary>
    public float InitialChargeTime = 3.0f;
    /// <summary>
    /// Time to charge more powa!
    /// </summary>
    public float ChargeTimer;

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
    private static IPathfinding _morallyObjectionablePathfinder = new BStarPathfinding();

    /// <summary>
    /// Method called after object instantiation to initialize fields etc. 
    /// </summary>
    public virtual void Initialize()
    {
        Buffs = new List<Buff>();

        UnitState = new UnitStateNormal(this);

        CellGrid = (CellGrid)GameObject.Find("CellGrid").GetComponent("CellGrid");

        animator = GetComponentInChildren<Animator>();

        TotalHitPoints = HitPoints;
        TotalMovementPoints = MovementPoints;
        TotalActionPoints = ActionPoints;
        StrengthPoints = StrengthPointsMaximum;
        SpeedPoints = SpeedPointsMaximum;
        CunningPoints = CunningPointsMaximum;
        Timer = ActionSpeed;
        ChargeTimer = InitialChargeTime;
    }

    private void OnTriggerEnter(Collider target)
    {
        switch (target.tag)
        {
            case "StrengthToken":
                IncrementAbilityPoint("Strength", 3);
                Destroy(target.gameObject);
                break;
            case "SpeedToken":
                IncrementAbilityPoint("Speed", 3);
                Destroy(target.gameObject);
                break;
            case "CunningToken":
                IncrementAbilityPoint("Cunning", 3);
                Destroy(target.gameObject);
                break;
            default:
                break;
        }
    }
    protected virtual void OnMouseDown()
    {
        if (UnitClicked != null && (PlayerNumber != 0 || Timer <= 0.0f))
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
        if (!IsReady)
        {
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
    public virtual void UpdateTimerBar()
    {
        Timer -= Time.deltaTime;
        var timerGraphic = MoveTimer?.GetComponent<Image>();

        if (timerGraphic != null)
        {
            timerGraphic.transform.localScale = new Vector3((float)((float)Timer / (float)ActionSpeed), 1, 1);
            timerGraphic.color = Color.Lerp(Color.blue, Color.red,
                (float)((float)Timer / (float)ActionSpeed));
        }
    }

    /// <summary>
    /// Method is called when units HP drops below 1.
    /// </summary>
    public virtual void ChargeAbilities()
    {
        ChargeTimer -= Time.deltaTime;

        if (ChargeTimer <= 0)
        {
            IncrementAbilityPoint("Strength", 1);
            IncrementAbilityPoint("Speed", 1);
            IncrementAbilityPoint("Cunning", 1);
            ChargeTimer = InitialChargeTime;
        }
    }

    /// <summary>
    /// Method when incrementing character ability points.
    /// </summary>
    public virtual void IncrementAbilityPoint(string _ability, int _points)
    {
        switch (_ability)
        {
            case "Strength":
                StrengthPoints += _points;

                if (StrengthPoints >= StrengthPointsMaximum)
                {
                    StrengthPoints = StrengthPointsMaximum;
                }
                break;
            case "Speed":
                SpeedPoints += _points;

                if (SpeedPoints >= SpeedPointsMaximum)
                {
                    SpeedPoints = SpeedPointsMaximum;
                }
                break;
            case "Cunning":
                CunningPoints += _points;

                if (CunningPoints >= CunningPointsMaximum)
                {
                    CunningPoints = CunningPointsMaximum;
                }
                break;
            default:
                break;
        }
    }

    /// <summary>
    /// Method called when entering camera square.
    /// </summary>
    public virtual void EnterCameraSquare(Cell _cell, List<Cell> _path, List<string> _apPath)
    {
        if (_path.IndexOf(_cell) == _path.Count - 1)
        {
            CheckCameraMatch(_cell, _apPath);
        }

        foreach (Transform child in _cell.transform)
        {
            if (child.tag == "Token")
            {
                GameObject.Destroy(child.gameObject);
            }
        }

        MyHexagon _hexScript = (MyHexagon)_cell.gameObject.GetComponent("MyHexagon");
        _hexScript.SuperCameraRequirement = new List<string> { };
    }

    /// <summary>
    /// Method called when entering camera square.
    /// </summary>
    public virtual void CheckCameraMatch(Cell _cell, List<string> _apPath)
    {
        List<string> _abilityList = new List<string> { };
        for (int i = 0; i < _apPath.Count - 1; i++)
        {
            _abilityList.Add(_apPath[i]);
        }

        MyHexagon _hexScript = (MyHexagon)_cell.gameObject.GetComponent("MyHexagon");

        var _cameraReq = _hexScript.SuperCameraRequirement;

        _cameraReq.Sort();
        _abilityList.Sort();

        if (_abilityList.Count == _cameraReq.Count && _abilityList.SequenceEqual(_cameraReq))
        {
            Debug.Log("YOU ARE AMAZING AT THIS GAME!");

            IncrementAbilityPoint("Strength", StrengthPointsMaximum);
            IncrementAbilityPoint("Speed", SpeedPointsMaximum);
            IncrementAbilityPoint("Cunning", CunningPointsMaximum);
        }
    }

    /// <summary>
    /// Method is called when unit is selected.
    /// </summary>
    public virtual void OnUnitSelected()
    {
        if (!IsReady)
        {
            return;
        }

        ActionMenu.SetActive(true);
        CellGrid.IsPaused = true;

        DetermineAvailableActions(CellGrid);

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
        IsCharging = ActionPoints > 0 ? true : false;

        SetState(new UnitStateMarkedAsFriendly(this));
        if (UnitDeselected != null)
            UnitDeselected.Invoke(this, new EventArgs());
    }

    /// <summary>
    /// Method indicates if it is possible to attack unit given as parameter, 
    /// from cell given as second parameter.
    /// </summary>
    public virtual bool IsUnitAttackable(Unit other, Cell sourceCell, int range = 1, AbilityCost cost = null)
    {
        if (cost?.StrengthCost > StrengthPoints || cost?.SpeedCost > SpeedPoints || cost?.CunningCost > CunningPoints)
        {
            return false;
        }

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
        StrengthPoints -= StrengthCost;
        SpeedPoints -= SpeedCost;
        CunningPoints -= CunningCost;

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

        if (UnitMoved != null)
        {
            UnitMoved.Invoke(this, new MovementEventArgs(Cell, destinationCell, path));
        }
    }
    protected virtual IEnumerator MovementAnimation(List<Cell> path, CellGrid cellGrid)
    {
        animator.SetBool("Idling", false);
        isMoving = true;
        path.Reverse();

        List<string> _abilityPointPath = new List<string>(
            path.Select(_cell =>
                {
                    MyHexagon _hexScript = (MyHexagon)_cell.gameObject.GetComponent("MyHexagon");

                    return _hexScript.AbilityPointType;
                })
                );

        foreach (var cell in path)
        {
            Vector3 destination_pos = new Vector3(cell.transform.localPosition.x, transform.localPosition.y, cell.transform.localPosition.z);

            Vector2 targetDir = new Vector2(cell.transform.localPosition.x, cell.transform.localPosition.z) - new Vector2(transform.localPosition.x, transform.localPosition.z);

            float angle = Vector2.Angle(targetDir, new Vector2(0, 1));
            float changeAngle = targetDir.x < 0 ? 180 - angle : angle - 180;
            transform.localRotation = Quaternion.Euler(0, changeAngle, 0);

            if (cell.AbilityPointType != "Camera")
            {
                IncrementAbilityPoint(cell.AbilityPointType, 1);
            }
            else
            {
                EnterCameraSquare(cell, path, _abilityPointPath);
            }

            while (transform.localPosition != destination_pos)
            {
                transform.localPosition = Vector3.MoveTowards(transform.localPosition, destination_pos, Time.deltaTime * MovementSpeed);

                yield return 0;
            }

            cell.RandomizeAbilityPointType();
        }
        // Reorient to camera
        transform.localRotation = Quaternion.Euler(0, 0, 0);

        cellGrid.CurrentPath = new List<Cell>();

        if (!ActionMenu)
        {
            animator.SetBool("Idling", true);
            isMoving = false;
            yield break;
        }

        // Set ability buttons as interactable based on whether or not enemies in range. 
        DetermineAvailableActions(cellGrid);

        animator.SetBool("Idling", true);
        isMoving = false;
    }


    ///<summary>
    /// Method sets available actions on action menu based on enemies in range and current ability points.
    /// </summary>
    public void DetermineAvailableActions(CellGrid cellGrid)
    {
        GameObject cards = ActionMenu.transform.Find("Cards").gameObject;

        foreach (Transform child in cards.transform)
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

            button.interactable = hasUnitInRange && unitAbility.StrengthPointCost <= StrengthPoints && unitAbility.SpeedPointCost <= SpeedPoints && unitAbility.CunningPointCost <= CunningPoints;
        }
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
        if (cachedPaths != null && cachedPaths.ContainsKey(destination))
        {
            return _morallyObjectionablePathfinder.FindPath(GetGraphEdges(cells), Cell, destination, CellGrid.CurrentPath, MovementPoints);
        }
        else
        {
            return _fallbackPathfinder.FindPath(GetGraphEdges(cells), Cell, destination, null, 0);
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
