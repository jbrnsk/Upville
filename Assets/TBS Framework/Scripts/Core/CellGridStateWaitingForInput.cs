using UnityEngine;

class CellGridStateWaitingForInput : CellGridState
{
    public CellGridStateWaitingForInput(CellGrid cellGrid) : base(cellGrid)
    {
    }

    public override void OnUnitClicked(Unit unit)
    {
        Debug.Log("unit", unit);
        if(unit.PlayerNumber.Equals(_cellGrid.CurrentPlayerNumber))
            _cellGrid.CellGridState = new CellGridStateUnitSelected(_cellGrid, unit); 
    }
}
