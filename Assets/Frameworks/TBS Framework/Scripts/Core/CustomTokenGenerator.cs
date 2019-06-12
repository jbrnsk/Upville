using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class CustomTokenGenerator : MonoBehaviour
{
    private System.Random _rnd = new System.Random();
    public int Amount;
    public Transform TokensParent;
    public GameObject TokenPrefab;
    public CellGrid CellGrid;

    const float initialTimer = 10;
    private float _timer = initialTimer;

    public void Start()
    {
        StartCoroutine(SpawnTokens());
    }

    void Update()
    {
        _timer -= Time.deltaTime;

        if(_timer <= 0){
            StartCoroutine(SpawnTokens());
            _timer = initialTimer;
        }
        
    }

    /// <summary>
    /// Method sets position on obstacle objects and 
    /// sets isTaken field on cells that the obstacles are occupying
    /// </summary>
    public IEnumerator SpawnTokens()
    {
       while (CellGrid.Cells == null)
        {
            yield return 0;
        }

        var cells = CellGrid.Cells;

        List<GameObject> ret = new List<GameObject>();

        if (TokensParent.childCount != 0)
        {
            for (int i = 0; i < TokensParent.childCount; i++)
            {
                var token = TokensParent.GetChild(i);
                ret.Add(token.gameObject); 
                // var bounds = getBounds(token);

                // var cell = cells.OrderBy(h => Math.Abs((h.transform.position - token.transform.position).magnitude)).First();
                // if (!cell.IsTaken)
                // {
                //     cell.IsTaken = true;
                //     token.localPosition = cell.transform.localPosition + new Vector3(0, bounds.y, 0);
                      
                // }
                // else
                // {
                //     Destroy(token.gameObject);
                // }
            }
        }

        List<Cell> freeCells = cells.FindAll(h => h.GetComponent<Cell>().IsTaken == false);
        freeCells = freeCells.OrderBy(h => _rnd.Next()).ToList();

        for (int i = 0; i < Mathf.Clamp(Amount,Amount,freeCells.Count); i++)
        {
            var cell = freeCells.ElementAt(i);
            cell.GetComponent<Cell>().IsTaken = true;

            var token = Instantiate(TokenPrefab);
            token.transform.parent = TokensParent.transform;

            // Move new tokens and keep old in same position
            token.transform.localPosition = cell.transform.localPosition + new Vector3(0, cell.GetCellDimensions().y + 0.5f, 0);
            ret.Add(token);   
        }
    }

    private Vector3 getBounds(Transform transform)
    {
        var renderer = transform.GetComponent<Renderer>();
        var combinedBounds = renderer.bounds;
        var renderers = transform.GetComponentsInChildren<Renderer>();
        foreach (var childRenderer in renderers)
        {
            if (childRenderer != renderer) combinedBounds.Encapsulate(childRenderer.bounds);
        }

        return combinedBounds.size;
    }
}
