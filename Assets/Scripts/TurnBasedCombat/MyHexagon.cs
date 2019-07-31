using UnityEngine;

class MyHexagon : Hexagon
{
    private Renderer hexagonRenderer;
    private Renderer outlineRenderer;

    public void Awake()
    {
        hexagonRenderer = GetComponent<Renderer>();

        var outline = transform.Find("Outline");
        outlineRenderer = outline.GetComponent<Renderer>();

        RandomizeAbilityPointType();
    }

    public override Vector3 GetCellDimensions()
    {
        var outline = transform.Find("Outline");
        var outlineRenderer = outline.GetComponent<Renderer>();
        return outlineRenderer.bounds.size;
    }

    public override void MarkAsReachable()
    {
        SetColor(hexagonRenderer, Color.yellow);
    }
    public override void MarkAsPath()
    {
        SetColor(hexagonRenderer, Color.green); ;
    }
    public override void MarkAsHighlighted()
    {
        SetColor(outlineRenderer, Color.blue);
    }
    public override void RandomizeAbilityPointType()
    {
        System.Random rnd = new System.Random();
        int randomAbility = rnd.Next(1, 4);

        switch (randomAbility)
        {
            case 1:
                MarkAsStrength();
                break;
            case 2:
                MarkAsSpeed();
                break;
            case 3:
            default:
                MarkAsCunning();
                break;
        }
    }
    public override void MarkAsStrength()
    {
        AbilityPointType = "Strength";
        SetColor(hexagonRenderer, new Color(1.0f, 0.5f, 0.5f));
    }
    public override void MarkAsSpeed()
    {
        AbilityPointType = "Speed";
        SetColor(hexagonRenderer, new Color(1.0f, 1.0f, 0.5f));
    }
    public override void MarkAsCunning()
    {
        AbilityPointType = "Cunning";
        SetColor(hexagonRenderer, new Color(0.66f, 0.66f, 1.0f));
    }
    public override void MarkAsMovementCell()
    {
        SetColor(hexagonRenderer, Color.grey);
    }
    public override void UnMark()
    {
        switch (AbilityPointType)
        {
            case "Strength":
                MarkAsStrength();
                break;
            case "Speed":
                MarkAsSpeed();
                break;
            case "Cunning":
                MarkAsCunning();
                break;
            default:
                SetColor(hexagonRenderer, Color.white);
                break;
        }

        SetColor(outlineRenderer, Color.black);
    }

    private void SetColor(Renderer renderer, Color color)
    {
        renderer.material.color = color;
    }
}

