using System;

/// <summary>
/// Class representing the costs associated with an ability.
/// </summary>
public class StyleIncreaseEventArguments : EventArgs
{
    /// <summary>
    /// Strength cost of ability
    /// </summary>
    public int StylePointIncrease { get; set; }
}