﻿using UnityEngine;
using System.Collections.Generic;
using System.Linq;

class CellGridStateUnitSelected : CellGridState
{
    private Unit _unit;
    private HashSet<Cell> _pathsInRange;
    private List<Unit> _unitsInRange;
    private GameObject _activeUnitMenu;

    private Cell _unitCell;

    private List<Cell> _currentPath;

    public CellGridStateUnitSelected(CellGrid cellGrid, Unit unit) : base(cellGrid)
    {
        _unit = unit;
        _pathsInRange = new HashSet<Cell>();
        _currentPath = new List<Cell>();
        _unitsInRange = new List<Unit>();
    }

    public override void OnCellClicked(Cell cell)
    {
        _cellGrid.IsPaused = false;
        
        if (_unit.isMoving)
            return;
        if(cell.IsTaken || !_pathsInRange.Contains(cell))
        {
            _cellGrid.CellGridState = new CellGridStateWaitingForInput(_cellGrid);
            return;
        }
            
        var path = _unit.FindPath(_cellGrid.Cells, cell);
        _unit.Move(cell,path,_cellGrid);
        _cellGrid.CellGridState = new CellGridStateUnitSelected(_cellGrid, _unit);
    }
    public override void OnUnitClicked(Unit unit)
    {
        _cellGrid.IsPaused = true;

        if (unit.Equals(_unit) || _unit.isMoving)
            return;

        if (_unitsInRange.Contains(unit) && _unit.ActionPoints > 0)
        {
            _unit.DealDamage(unit);
            _cellGrid.CellGridState = new CellGridStateUnitSelected(_cellGrid, _unit);
        }

        if (unit.PlayerNumber.Equals(_unit.PlayerNumber))
        {
            _cellGrid.CellGridState = new CellGridStateUnitSelected(_cellGrid, unit);
        }
            
    }
    public override void OnCellDeselected(Cell cell)
    {
        base.OnCellDeselected(cell);
        foreach(var _cell in _currentPath)
        {
            if (_pathsInRange.Contains(_cell))
                _cell.MarkAsReachable();
            else
                _cell.UnMark();
        }
    }
    public override void OnCellSelected(Cell cell)
    {
        base.OnCellSelected(cell);
        if (!_pathsInRange.Contains(cell)) return;

        _currentPath = _unit.FindPath(_cellGrid.Cells, cell);
        foreach (var _cell in _currentPath)
        {
            _cell.MarkAsPath();
        }
    }

    public override void OnStateEnter()
    {
        base.OnStateEnter();
        if(!_unit.IsReady) {
            return;
        }
        _unit.OnUnitSelected();
        _unitCell = _unit.Cell;

        _pathsInRange = _unit.GetAvailableDestinations(_cellGrid.Cells);

        var cellsNotInRange = _cellGrid.Cells.Except(_pathsInRange);

        foreach (var cell in cellsNotInRange)
        {
            cell.UnMark();
        }
        foreach (var cell in _pathsInRange)
        {
            cell.MarkAsReachable();
        }

        if (_unit.ActionPoints <= 0) return;
        
        if (_unitCell.GetNeighbours(_cellGrid.Cells).FindAll(c => c.MovementCost <= _unit.MovementPoints).Count == 0 
            && _unitsInRange.Count == 0)
            _unit.SetState(new UnitStateMarkedAsFinished(_unit));
    }
    public override void OnStateExit()
    {
        _unit.OnUnitDeselected();
        foreach (var unit in _unitsInRange)
        {
            if (unit == null) continue;
            unit.SetState(new UnitStateNormal(unit));
        }
        foreach (var cell in _cellGrid.Cells)
        {
            cell.UnMark();
        }   
    }

    public override void UnitAbility(int attackFactor, int attackRange)
    {
        foreach (var currentUnit in _cellGrid.Units)
        {
            if (currentUnit.PlayerNumber.Equals(_unit.PlayerNumber))
                continue;
        
            if (_unit.IsUnitAttackable(currentUnit, _unit.Cell, attackRange))
            {
                currentUnit.SetState(new UnitStateMarkedAsReachableEnemy(currentUnit));
                _unitsInRange.Add(currentUnit);
                _unit.AttackFactor = attackFactor;
            }
        }
    }

    public override void Taunt()
    {
        Debug.Log("GET OUT OF HERE VICTOR");
    }
}

