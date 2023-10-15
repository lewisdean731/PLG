using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UpdateableData : ScriptableObject
{
    public event System.Action onValuesUpdated;
    public bool autoUpdate;

    protected virtual void OnValidate()
    {
        if (autoUpdate)
        {
            notifyOfUpdatedValues();
        }
    }

    public void notifyOfUpdatedValues()
    {
        if(onValuesUpdated != null)
        {
            onValuesUpdated();
        }
    }
}
