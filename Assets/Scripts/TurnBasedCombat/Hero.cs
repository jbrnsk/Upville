﻿using UnityEngine.UI;

public class Hero : MyUnit
{
    private BuffSpawner _buffSpawner;
    private Button _specialAbilityButton;
    private bool _abilityUsed;

    public override void Initialize()
    {
        base.Initialize();
        _buffSpawner = new BuffSpawner();
        _specialAbilityButton = GetComponentInChildren<Button>();
        _specialAbilityButton.gameObject.SetActive(false);
        _specialAbilityButton.onClick.AddListener(TriggerSpecialAbility);
    }

    public override void OnTurnEnd()
    {
        _buffSpawner.SpawnBuff(new HealingBuff(1, 1), Cell, this, 1, false);
        _buffSpawner.SpawnBuff(new DefenceBuff(1, 1), Cell, this, 1, false);//Hero has the ability to heal and raise defence od adjacent units.
        base.OnTurnEnd();
    }
    public override void OnUnitSelected()
    {
        if (!_abilityUsed)
        {
            Invoke("EnableSpecialAbilityButton",0.1f);
        }       

        ActionMenu.gameObject.SetActive(true);
        CellGrid.IsPaused = true;
    }
    public override void OnUnitDeselected()
    {
        _specialAbilityButton.gameObject.SetActive(false);

        ActionMenu.gameObject.SetActive(false);
        CellGrid.IsPaused = true;

    }

    private void EnableSpecialAbilityButton() 
    {
        _specialAbilityButton.gameObject.SetActive(true);
        _specialAbilityButton.interactable = true;
    }
    private void TriggerSpecialAbility()
    {
        //Hero has specail ability that allows him to raise his attack by 2 for duration of 3 turns.
        //This ability can be triggered once a game.
        if (!_abilityUsed)
        {
            _abilityUsed = true;
            var buff = new AttackBuff(3, 2);
            buff.Apply(this);
            Buffs.Add(buff);

            _specialAbilityButton.gameObject.SetActive(false);
        }  
    }
}
