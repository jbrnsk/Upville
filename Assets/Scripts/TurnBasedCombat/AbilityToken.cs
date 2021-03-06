using UnityEngine;
using UnityEngine.UI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Collections;

/// <summary>
/// Base class for all tokens in the game.
/// </summary>
public abstract class AbilityToken : MonoBehaviour
{
    /// <summary>
    /// Cell that the token is on
    /// </summary>
    public GameObject CellObject { get; set; }
    /// <summary>
    /// Cell that the token is on
    /// </summary>
    public GameObject CellGrid { get; set; }

    /// <summary>
    /// CellClicked event is invoked when user clicks on the cell. 
    /// It requires a collider on the cell game object to work.
    /// </summary>
    public event EventHandler CellClicked;
    /// <summary>
    /// CellHighlighed event is invoked when cursor enters the cell's collider. 
    /// It requires a collider on the cell game object to work.
    /// </summary>
    public event EventHandler CellHighlighted;
    /// <summary>
    /// CellDehighlighted event is invoked when cursor exits the cell's collider. 
    /// It requires a collider on the cell game object to work.
    /// </summary>
    public event EventHandler CellDehighlighted;
}