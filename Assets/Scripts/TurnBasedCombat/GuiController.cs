using System;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class GuiController : MonoBehaviour
{
    public CellGrid CellGrid;

    public Image UnitImage;
    public Text InfoText;
    public Text StatsText;
    public GameObject StyleMeter;

    void Awake()
    {
        UnitImage.color = Color.gray;

        CellGrid.GameStarted += OnGameStarted;
        CellGrid.TurnEnded += OnTurnEnded;
        CellGrid.GameEnded += OnGameEnded;
        CellGrid.UnitAdded += OnUnitAdded;
        CellGrid.StyleModified += OnStyleModified;
    }

    private void OnGameStarted(object sender, EventArgs e)
    {
        foreach (Transform cell in CellGrid.transform)
        {
            cell.GetComponent<Cell>().CellHighlighted += OnCellHighlighted;
            cell.GetComponent<Cell>().CellDehighlighted += OnCellDehighlighted;
        }

        UpdateStyleMeter();

        OnTurnEnded(sender, e);
    }

    private void OnGameEnded(object sender, EventArgs e)
    {
        SceneManager.LoadScene(0);
    }
    private void OnTurnEnded(object sender, EventArgs e)
    {
        InfoText.text = "Player " + ((sender as CellGrid).CurrentPlayerNumber + 1);
    }
    private void OnCellDehighlighted(object sender, EventArgs e)
    {
        UnitImage.color = Color.gray;
        StatsText.text = "";
    }
    private void OnCellHighlighted(object sender, EventArgs e)
    {
        UnitImage.color = Color.gray;
        StatsText.text = "Movement Cost: " + (sender as Cell).MovementCost;
    }
    private void OnUnitAttacked(object sender, AttackEventArgs e)
    {
        if (!(CellGrid.CurrentPlayer is HumanPlayer)) return;
        OnUnitDehighlighted(sender, new EventArgs());

        if ((sender as Unit).HitPoints <= 0) return;

        OnUnitHighlighted(sender, e);
    }
    private void OnUnitDehighlighted(object sender, EventArgs e)
    {
        StatsText.text = "";
        UnitImage.color = Color.gray;
    }
    private void OnUnitHighlighted(object sender, EventArgs e)
    {
        var unit = sender as MyUnit;
        StatsText.text = unit.UnitName + "\nHit Points: " + unit.HitPoints + "/" + unit.TotalHitPoints + "\nStrength: " + unit.StrengthPoints + "\nSpeed: " + unit.SpeedPoints + "\nCunning: " + unit.CunningPoints;
        UnitImage.color = unit.PlayerColor;

    }
    private void OnUnitAdded(object sender, UnitCreatedEventArgs e)
    {
        RegisterUnit(e.unit);
    }

    private void RegisterUnit(Transform unit)
    {
        unit.GetComponent<Unit>().UnitHighlighted += OnUnitHighlighted;
        unit.GetComponent<Unit>().UnitDehighlighted += OnUnitDehighlighted;
        unit.GetComponent<Unit>().UnitAttacked += OnUnitAttacked;
    }
    public void RestartLevel()
    {
        SceneManager.LoadScene(Application.loadedLevel);
    }

    private void OnStyleModified(object sender, StyleIncreaseEventArguments e)
    {
        CellGrid.StylePoints += e.StylePointIncrease;

        UpdateStyleMeter();
    }

    private void UpdateStyleMeter()
    {
        var styleGraphic = StyleMeter?.GetComponent<Image>();

        if (styleGraphic != null)
        {
            styleGraphic.transform.localScale = new Vector3((float)((float)CellGrid.StylePoints / (float)CellGrid.MaxStylePoints), 1, 1);
            styleGraphic.color = Color.Lerp(Color.red, Color.green,
                (float)((float)CellGrid.StylePoints / (float)CellGrid.MaxStylePoints));
        }
    }
}