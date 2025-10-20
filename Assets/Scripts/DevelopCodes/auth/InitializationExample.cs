using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Services.Core;
using UnityEngine;

public class InitializationExample : MonoBehaviour
{
    async void Awake()
    {
        try
        {
            await UnityServices.InitializeAsync();
        }
        catch (Exception e)
        {
            Debug.LogException(e);
        }
    }
}
