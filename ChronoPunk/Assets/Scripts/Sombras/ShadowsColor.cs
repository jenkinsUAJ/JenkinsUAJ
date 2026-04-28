using UnityEngine;

[CreateAssetMenu(fileName = "ShadowsColor", menuName = "Scriptable Objects/ShadowsColor")]
public class ShadowsColor : ScriptableObject
{
    public Color color1;
    public Color color2;
    public Color color3;
    public Color color4;
    public Color color5;
    public Color color6;


    public Color GetColor(int indx)
    {
        switch (indx)
        {
            case 0:
                return color1;
            case 1:
                return color2;
            case 2:
                return color3;
            case 3:
                return color4;
            case 4:
                return color5;
            case 5:
                return color6;
            default:
                return new Color(0,0,0,1);
        }
    }
}
