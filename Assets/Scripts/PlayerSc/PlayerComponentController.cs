using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerComponentController : MonoBehaviour
{
    // Kontrol edeceðimiz componentleri buraya ekliyoruz
    [SerializeField] private List<Behaviour> componentsToControl = new List<Behaviour>();

    /// <summary>
    /// Tüm componentleri aktif/pasif yapar
    /// </summary>
    /// <param name="state">true = aktif, false = pasif</param>
    public void SetComponentsActive(bool state)
    {
        foreach (var component in componentsToControl)
        {
            if (component != null)
                component.enabled = state;
        }
    }

    /// <summary>
    /// Belirli bir componenti açýp kapatýr
    /// </summary>
    /// <param name="componentType">Component türü</param>
    /// <param name="state">true = aktif, false = pasif</param>
    public void SetComponentActive<T>(bool state) where T : Behaviour
    {
        foreach (var component in componentsToControl)
        {
            if (component is T)
                component.enabled = state;
        }
    }

    /// <summary>
    /// Component listesini otomatik olarak doldurur
    /// </summary>
    public void AutoPopulateComponents()
    {
        componentsToControl.Clear();
        componentsToControl.AddRange(GetComponentsInChildren<Behaviour>());
    }
}
