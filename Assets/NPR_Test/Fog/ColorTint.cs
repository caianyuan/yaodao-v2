using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

public class ColorTint : VolumeComponent
{
    public ColorParameter colorChange = new ColorParameter(Color.white, true);
}
