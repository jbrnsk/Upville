using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class CameraSpaceGenerator : MonoBehaviour
{
    private System.Random _rnd = new System.Random();
    public int Amount;
    public CellGrid CellGrid;
    private RandomObstacleGenerator _obstacleGenerator;

    public static float InitialTimer = 10;
    public float Timer = InitialTimer;

    private void Start()
    {
        _obstacleGenerator = (RandomObstacleGenerator)this.gameObject.GetComponent("RandomObstacleGenerator");

        if (!_obstacleGenerator)
        {
            Debug.LogError("No RandomObstacleGenerator found by CustomTokenGenerator");
        }
    }

    /// <summary>
    /// Method sets position on obstacle objects and 
    /// sets isTaken field on cells that the obstacles are occupying
    /// </summary>
    public IEnumerator SpawnCameraSpaces()
    {
        while (CellGrid.Cells == null || !_obstacleGenerator.ObstaclesSpawned)
        {
            yield return 0;
        }

        var cells = CellGrid.Cells;

        List<Cell> freeCells = cells.FindAll(h => h.GetComponent<Cell>().IsTaken == false && h.GetComponent<Cell>().IsCamera == false);
        freeCells = freeCells.OrderBy(h => _rnd.Next()).ToList();

        for (int i = 0; i < Mathf.Clamp(Amount, Amount, freeCells.Count); i++)
        {
            var cell = freeCells.ElementAt(i);

            cell.MarkAsCamera();
        }
    }
}
