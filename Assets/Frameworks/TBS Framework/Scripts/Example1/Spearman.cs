using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class Spearman : MyUnit
{
    public override void Activate(CellGrid cellGrid, List<Unit> myUnits) {
        IsReady = true;
        StartCoroutine(Play(cellGrid, myUnits)); 

        Debug.Log("What be happening");
        IsReady = false;
        Timer = ActionSpeed;
    }

    private IEnumerator Play(CellGrid cellGrid, List<Unit> myUnits){   
        var _rnd = new System.Random();

        var enemyUnits = cellGrid.Units.Except(myUnits).ToList();
        var unitsInRange = new List<Unit>();
        foreach (var enemyUnit in enemyUnits)
        {
            if (IsUnitAttackable(enemyUnit,Cell))
            {
                unitsInRange.Add(enemyUnit);
            }
        }//Looking for enemies that are in attack range.
        if (unitsInRange.Count != 0)
        {
            var index = _rnd.Next(0, unitsInRange.Count);
            DealDamage(unitsInRange[index]);
            yield return new WaitForSeconds(0.5f);
            yield return 0; // Should be continue?
        }//If there is an enemy in range, attack it.

        List<Cell> potentialDestinations = new List<Cell>();
        
        foreach (var enemyUnit in enemyUnits)
        {
            potentialDestinations.AddRange(cellGrid.Cells.FindAll(c=> IsCellMovableTo(c) && IsUnitAttackable(enemyUnit, c))); 
        }//Making a list of cells that the unit can attack from.
    
        var notInRange = potentialDestinations.FindAll(c => c.GetDistance(Cell) > MovementPoints);
        potentialDestinations = potentialDestinations.Except(notInRange).ToList();

        if (potentialDestinations.Count == 0 && notInRange.Count !=0)
        {
            potentialDestinations.Add(notInRange.ElementAt(_rnd.Next(0,notInRange.Count-1)));
        }     

        potentialDestinations = potentialDestinations.OrderBy(h => _rnd.Next()).ToList();
        List<Cell> shortestPath = null;
        foreach (var potentialDestination in potentialDestinations)
        {
            var path = FindPath(cellGrid.Cells, potentialDestination);
            if ((shortestPath == null && path.Sum(h => h.MovementCost) > 0) || shortestPath != null && (path.Sum(h => h.MovementCost) < shortestPath.Sum(h => h.MovementCost) && path.Sum(h => h.MovementCost) > 0))
                shortestPath = path;

            var pathCost = path.Sum(h => h.MovementCost);
            if (pathCost > 0 && pathCost <= MovementPoints)
            {
                Move(potentialDestination, path, cellGrid);
                while (isMoving)
                    yield return 0;
                shortestPath = null;
                break;
            }
            yield return 0;
        }//If there is a path to any cell that the unit can attack from, move there.

        if (shortestPath != null)
        {      
            foreach (var potentialDestination in shortestPath.Intersect(GetAvailableDestinations(cellGrid.Cells)).OrderByDescending(h => h.GetDistance(Cell)))
            {
                var path = FindPath(cellGrid.Cells, potentialDestination);
                var pathCost = path.Sum(h => h.MovementCost);
                if (pathCost > 0 && pathCost <= MovementPoints)
                {
                    Move(potentialDestination, path, cellGrid);
                    while (isMoving)
                        yield return 0;
                    break;
                }
                yield return 0;
            }
        }//If the path cost is greater than unit movement points, move as far as possible.
        
        foreach (var enemyUnit in enemyUnits)
        {
            var enemyCell = enemyUnit.Cell;
            if (IsUnitAttackable(enemyUnit,Cell))
            { 
                DealDamage(enemyUnit);
                yield return new WaitForSeconds(0.5f);
                break;
            }
        }//Look for enemies in range and attack.
    }

    protected override void Defend(Unit other, int damage)
    {
        var realDamage = damage;
        if (other is Archer)
            realDamage *= 2;//Archer deals double damage to spearman.

        base.Defend(other, realDamage);
    }
}
