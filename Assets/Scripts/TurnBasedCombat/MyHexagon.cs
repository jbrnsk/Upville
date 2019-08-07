using UnityEngine;
using System.Collections;
using System.Collections.Generic;

class MyHexagon : Hexagon
{
    private Renderer hexagonRenderer;
    private Renderer outlineRenderer;
    private bool pulsing;
    public GameObject CameraToken;
    private Color strengthColor = new Color(0.25f, 0.9f, 0.25f);
    private Color speedColor = new Color(0.9f, 0.25f, 0.9f);
    private Color cunningColor = new Color(0.9f, 0.9f, 0.25f);

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
        Color standardColor;
        Color highlightColor;

        switch (AbilityPointType)
        {
            case "Strength":
                standardColor = strengthColor;
                highlightColor = new Color(0f, 0.5f, 0f);
                break;
            case "Speed":
                standardColor = speedColor;
                highlightColor = new Color(0.5f, 0f, 0.5f);
                break;
            case "Cunning":
                standardColor = cunningColor;
                highlightColor = new Color(0.5f, 0.5f, 0f);
                break;
            case "Camera":
            default:
                standardColor = new Color(0.25f, 0.25f, 0.25f);
                highlightColor = new Color(0f, 0f, 0f);
                break;
        }

        StartCoroutine(Pulse(standardColor, highlightColor));
    }

    /// <summary>
    /// A coroutine method that executes all commands in the Block. Only one running instance of each Block is permitted.
    /// </summary>
    /// <param name="commandIndex">Index of command to start execution at</param>
    /// <param name="onComplete">Delegate function to call when execution completes</param>
    public virtual IEnumerator Pulse(Color standardColor, Color highlightColor)
    {
        float size = 0.0f;
        pulsing = true;

        while (pulsing == true)
        {
            size += Time.deltaTime;
            hexagonRenderer.material.color = Color.Lerp(standardColor, highlightColor, Mathf.PingPong(size, 0.75f));

            yield return null;
        }

        SetColor(hexagonRenderer, standardColor);

        yield break;
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
        switch (Random.Range(0, 4))
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
        SetColor(hexagonRenderer, strengthColor);
    }
    public override void MarkAsSpeed()
    {
        AbilityPointType = "Speed";
        SetColor(hexagonRenderer, speedColor);
    }
    public override void MarkAsCunning()
    {
        AbilityPointType = "Cunning";
        SetColor(hexagonRenderer, cunningColor);
    }
    public override void MarkAsCamera()
    {
        AbilityPointType = "Camera";
        SetColor(hexagonRenderer, new Color(0.25f, 0.25f, 0.25f));

        ColorTokenFactory();
    }

    private void ColorTokenFactory()
    {
        List<int> colorList = new List<int> { Random.Range(0, 3), Random.Range(0, 3), Random.Range(0, 3) };

        for (int i = 0; i < colorList.Count; i++)
        {
            var colorInt = colorList[i];
            GameObject token = NewToken(i, colorInt);
        }
    }

    private GameObject NewToken(int index, int colorInt)
    {
        List<Color> colorMap = new List<Color> { strengthColor, speedColor, cunningColor };
        GameObject newToken = Instantiate(CameraToken, this.transform.position, Quaternion.identity, this.transform);
        Renderer renderer = newToken.GetComponent<Renderer>();

        SetColor(renderer, colorMap[colorInt]);

        switch (index)
        {
            case 0:
                newToken.transform.position = this.gameObject.transform.position + new Vector3(-0.1f, this.GetCellDimensions().y - 0.1f, 0.15f);
                break;
            case 1:
                newToken.transform.position = this.gameObject.transform.position + new Vector3(0.15f, this.GetCellDimensions().y - 0.1f, 0f);
                break;
            case 2:
            default:
                newToken.transform.position = this.gameObject.transform.position + new Vector3(-0.1f, this.GetCellDimensions().y - 0.1f, -0.15f);
                break;
        }

        return newToken;
    }
    public override void MarkAsMovementCell()
    {
        AbilityPointType = "Camera";
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
            case "Camera":
                MarkAsCamera();
                break;
            default:
                SetColor(hexagonRenderer, Color.white);
                break;
        }

        SetColor(outlineRenderer, Color.black);
        pulsing = false;
    }

    private void SetColor(Renderer renderer, Color color)
    {
        renderer.material.color = color;
    }
}

