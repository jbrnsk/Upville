﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/// <summary>
/// CellGrid class keeps track of the game, stores cells, units and players objects. It starts the game and makes turn transitions. 
/// It reacts to user interacting with units or cells, and raises events related to game progress. 
/// </summary>
public class CellGrid : MonoBehaviour
{
    /// <summary>
    /// LevelLoading event is invoked before Initialize method is run.
    /// </summary>
    public event EventHandler LevelLoading;
    /// <summary>
    /// LevelLoadingDone event is invoked after Initialize method has finished running.
    /// </summary>
    public event EventHandler LevelLoadingDone;
    /// <summary>
    /// GameStarted event is invoked at the beggining of StartGame method.
    /// </summary>
    public event EventHandler GameStarted;
    /// <summary>
    /// GameEnded event is invoked when there is a single player left in the game.
    /// </summary>
    public event EventHandler GameEnded;
    /// <summary>
    /// Turn ended event is invoked at the end of each turn.
    /// </summary>
    public event EventHandler TurnEnded;
    /// <summary>
    /// Style modified event invoked when style is modified.
    /// </summary>
    public event EventHandler<StyleIncreaseEventArguments> StyleModified;

    /// <summary>
    /// Whether or not game is paused.
    /// </summary>
    public bool IsPaused;

    /// <summary>
    /// Pulse time tracking.
    /// </summary>
    public float PulseTimer;

    /// <summary>
    /// Player team style points.
    /// </summary>
    public int StylePoints;

    /// <summary>
    /// Player max style points.
    /// </summary>
    public int MaxStylePoints;

    /// <summary>
    /// UnitAdded event is invoked each time AddUnit method is called.
    /// </summary>
    public event EventHandler<UnitCreatedEventArgs> UnitAdded;

    private CellGridState _cellGridState; //The grid delegates some of its behaviours to cellGridState object.

    // public GameObject ActiveUnitMenu;

    public CellGridState CellGridState
    {
        private get
        {
            return _cellGridState;
        }
        set
        {
            if (_cellGridState != null)
                _cellGridState.OnStateExit();
            _cellGridState = value;
            _cellGridState.OnStateEnter();
        }
    }

    private CameraSpaceGenerator CameraGenerator; //The grid delegates some of its behaviours to cellGridState object.

    public int NumberOfPlayers { get; private set; }

    public Player CurrentPlayer
    {
        get { return Players.Find(p => p.PlayerNumber.Equals(CurrentPlayerNumber)); }
    }
    public int CurrentPlayerNumber { get; private set; }

    /// <summary>
    /// GameObject that holds player objects.
    /// </summary>
    public Transform PlayersParent;

    public List<Player> Players { get; private set; }
    public List<Cell> Cells { get; private set; }
    public List<Cell> CurrentPath;
    public List<Unit> Units { get; private set; }

    private void Start()
    {
        if (LevelLoading != null)
            LevelLoading.Invoke(this, new EventArgs());

        Initialize();

        if (LevelLoadingDone != null)
            LevelLoadingDone.Invoke(this, new EventArgs());

        StartGame();
    }

    private void Initialize()
    {
        Players = new List<Player>();

        for (int i = 0; i < PlayersParent.childCount; i++)
        {
            var player = PlayersParent.GetChild(i).GetComponent<Player>();
            if (player != null)
                Players.Add(player);
            else
                Debug.LogError("Invalid object in Players Parent game object");
        }
        NumberOfPlayers = Players.Count;
        CurrentPlayerNumber = 1; // Players.Min(p => p.PlayerNumber);

        Cells = new List<Cell>();
        for (int i = 0; i < transform.childCount; i++)
        {
            var cell = transform.GetChild(i).gameObject.GetComponent<Cell>();
            if (cell != null)
            {
                Cells.Add(cell);
            }
            else
                Debug.LogError("Invalid object in cells parent game object");
        }

        foreach (var cell in Cells)
        {
            cell.CellClicked += OnCellClicked;
            cell.CellHighlighted += OnCellHighlighted;
            cell.CellDehighlighted += OnCellDehighlighted;
            cell.GetComponent<Cell>().GetNeighbours(Cells);
        }

        var unitGenerator = GetComponent<IUnitGenerator>();
        if (unitGenerator != null)
        {
            Units = unitGenerator.SpawnUnits(Cells);
            foreach (var unit in Units)
            {
                AddUnit(unit.GetComponent<Transform>());
            }
        }
        else
            Debug.LogError("No IUnitGenerator script attached to cell grid");

        CameraGenerator = (CameraSpaceGenerator)this.gameObject.GetComponent("CameraSpaceGenerator");
        if (unitGenerator != null)
        {
            StartCoroutine(CameraGenerator.SpawnCameraSpaces());
        }
        else
            Debug.LogError("No CustomTokenGenerator attached to cell grid");

    }

    void Update()
    {
        PulseTimer += Time.deltaTime;

        RaycastHit hit;
        var range = 100;
        var camera = Camera.main;
        var mask = 1 << LayerMask.NameToLayer("Cell");

        if (Input.GetButton("Fire1") && Physics.Raycast(camera.ScreenPointToRay(Input.mousePosition), out hit, range, mask))
        {
            var script = hit.transform.GetComponent<MyHexagon>();
            if (script != null)
            {
                script.EnterCell();
            }
        }

        if (IsPaused)
        {
            return;
        }

        CameraGenerator.Timer -= Time.deltaTime;

        if (CameraGenerator.Timer <= 0)
        {
            StartCoroutine(CameraGenerator.SpawnCameraSpaces());
            CameraGenerator.Timer = CustomTokenGenerator.InitialTimer;
        }

        foreach (Unit unit in Units)
        {
            if (!unit.IsReady)
            {
                unit.UpdateTimerBar();
            }

            if (unit.IsCharging)
            {
                unit.ChargeAbilities();
            }

            if (unit.Timer <= 0.0f && !unit.IsReady)
            {
                var myUnits = Units.FindAll(u => u.PlayerNumber.Equals(unit.PlayerNumber)).ToList();

                unit.Activate(this, myUnits);
            }
        }
    }

    private void OnCellDehighlighted(object sender, EventArgs e)
    {
        CellGridState.OnCellDeselected(sender as Cell);
    }
    private void OnCellHighlighted(object sender, EventArgs e)
    {
        CellGridState.OnCellSelected(sender as Cell);
    }
    private void OnCellClicked(object sender, EventArgs e)
    {
        CellGridState.OnCellClicked(sender as Cell);
    }

    private void OnUnitClicked(object sender, EventArgs e)
    {
        CellGridState.OnUnitClicked(sender as Unit);
    }
    private void OnUnitDestroyed(object sender, AttackEventArgs e)
    {
        Units.Remove(sender as Unit);
        var totalPlayersAlive = Units.Select(u => u.PlayerNumber).Distinct().ToList(); //Checking if the game is over
        if (totalPlayersAlive.Count == 1)
        {
            if (GameEnded != null)
                GameEnded.Invoke(this, new EventArgs());
        }
    }

    /// <summary>
    /// Adds unit to the game
    /// </summary>
    /// <param name="unit">Unit to add</param>
    public void AddUnit(Transform unit)
    {
        unit.GetComponent<Unit>().UnitClicked += OnUnitClicked;
        unit.GetComponent<Unit>().UnitDestroyed += OnUnitDestroyed;

        if (UnitAdded != null)
            UnitAdded.Invoke(this, new UnitCreatedEventArgs(unit));
    }

    /// <summary>
    /// Method is called once, at the beggining of the game.
    /// </summary>
    public void StartGame()
    {
        if (GameStarted != null)
            GameStarted.Invoke(this, new EventArgs());

        Players.Find(p => p.PlayerNumber.Equals(CurrentPlayerNumber)).Play(this);
    }

    /// <summary>
    /// Method makes turn transitions. It is called by player at the end of his turn.
    /// </summary>
    public void EndTurn()
    {
        if (Units.Select(u => u.PlayerNumber).Distinct().Count() == 1)
        {
            return;
        }
        CellGridState = new CellGridStateTurnChanging(this);

        Units.FindAll(u => u.PlayerNumber.Equals(CurrentPlayerNumber)).ForEach(u => { u.OnTurnEnd(); });

        CurrentPlayerNumber = (CurrentPlayerNumber + 1) % NumberOfPlayers;
        while (Units.FindAll(u => u.PlayerNumber.Equals(CurrentPlayerNumber)).Count == 0)
        {
            CurrentPlayerNumber = (CurrentPlayerNumber + 1) % NumberOfPlayers;
        } //Skipping players that are defeated.

        if (TurnEnded != null)
            TurnEnded.Invoke(this, new EventArgs());

        Units.FindAll(u => u.PlayerNumber.Equals(CurrentPlayerNumber)).ForEach(u => { u.OnTurnStart(); });
        Players.Find(p => p.PlayerNumber.Equals(CurrentPlayerNumber)).Play(this);
    }

    /// <summary>
    /// Method shows available units for melee attack.
    /// </summary>
    public void Taunt()
    {
        _cellGridState.Taunt();
    }

    /// <summary>
    /// Method shows available units for melee attack.
    /// </summary>
    public void ModifyStyle(int stylepointModifier)
    {
        StyleIncreaseEventArguments args = new StyleIncreaseEventArguments();
        args.StylePointIncrease = stylepointModifier;

        if (StyleModified != null)
            StyleModified.Invoke(this, args);
    }

    /// <summary>
    /// Method shows available units for melee attack.
    /// </summary>
    public void UnitAbility(UnitAbility selectedUnitAbility)
    {
        int attackRange = selectedUnitAbility.AttackRange;
        int attackFactor = selectedUnitAbility.AttackFactor;
        AbilityCost cost = new AbilityCost();
        cost.StrengthCost = selectedUnitAbility.StrengthPointCost;
        cost.SpeedCost = selectedUnitAbility.SpeedPointCost;
        cost.CunningCost = selectedUnitAbility.CunningPointCost;

        _cellGridState.UnitAbility(attackFactor, attackRange, cost);
    }
}