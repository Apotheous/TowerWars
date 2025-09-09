using DG.Tweening;
using System.Collections;
using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.UI;
using static SelectionPanelAnimation;

public class SelectionPanelAnimation : MonoBehaviour
{

    public enum UnitType
    {
        initial = -1,
        Soldier = 0,
        Turret = 1
    }

    public enum SoldierState
    {
        Closed = 0,
        Open = 1
    }

    public enum TurretState
    {
        Closed = 0,
        Open = 1
    }

    [SerializeField] GameObject unitSelectionPanel;
    [SerializeField] GameObject superPowerBtn;
    [SerializeField] Button soldierButton; // Soldier button referans�
    [SerializeField] Button turretButton;  // Turret button referans�
    [SerializeField] float slideDistance = 200f; // Kayma mesafesi
    [SerializeField] float animationDuration = 0.5f; // Animasyon s�resi

    private Vector3 originalPanelPosition; // Panel'in orijinal pozisyonu
    private Vector3 originalBtnPosition; // Button'�n orijinal pozisyonu
    private CanvasGroup panelCanvasGroup; // Transparency i�in
    private bool isPanelDown = false; // Panel a�a��da m�?

    // Unit states - enum de�i�kenlerini tan�mla
    private UnitType currentSelectedUnit = UnitType.initial;
    private SoldierState soldierState = SoldierState.Closed;
    private TurretState turretState = TurretState.Closed;

    void Start()
    {
        // Orijinal pozisyonlar� kaydet
        originalPanelPosition = unitSelectionPanel.transform.localPosition;
        originalBtnPosition = superPowerBtn.transform.localPosition;

        // Canvas Group ekle (transparency i�in)
        panelCanvasGroup = unitSelectionPanel.GetComponent<CanvasGroup>();
        if (panelCanvasGroup == null)
            panelCanvasGroup = unitSelectionPanel.AddComponent<CanvasGroup>();

        // Panel ba�lang��ta kapal�
        unitSelectionPanel.SetActive(false);
        panelCanvasGroup.alpha = 0f;
    }

    // Bu methodu button'a ba�la
    public void TogglePanel()
    {
        if (!isPanelDown)
        {
            // Panel'i a� ve a�a��ya kayd�r
            OpenPanelWithAnimation();
        }
        else
        {
            // Panel'i kapat ve yukar�ya kayd�r
            ClosePanelWithAnimation();
        }
    }

    // Panel'i a�ma animasyonu
    void OpenPanelWithAnimation()
    {
        // Panel'i aktif et
        unitSelectionPanel.SetActive(true);
        isPanelDown = true;

        // Hedef pozisyonlar
        Vector3 panelTargetPos = originalPanelPosition + new Vector3(0, -slideDistance, 0);
        Vector3 btnTargetPos = originalBtnPosition + new Vector3(0, -slideDistance, 0);

        // Panel hareket animasyonu
        unitSelectionPanel.transform.DOLocalMove(panelTargetPos, animationDuration)
            .SetEase(Ease.OutQuart);

        // Button hareket animasyonu
        superPowerBtn.transform.DOLocalMove(btnTargetPos, animationDuration)
            .SetEase(Ease.OutQuart);

        // Transparency animasyonu - 100 birim sonra g�r�n�r olsun
        DOVirtual.DelayedCall(animationDuration * 0.5f, () => {
            panelCanvasGroup.DOFade(1f, animationDuration * 0.5f)
                .SetEase(Ease.InQuart);
        });

        Debug.Log("Panel a��ld� ve a�a��ya kayd�");
    }

    // Panel'i kapatma animasyonu
    void ClosePanelWithAnimation()
    {
        isPanelDown = false;

        // �nce transparency'yi gizle
        panelCanvasGroup.DOFade(0f, animationDuration * 0.3f)
            .SetEase(Ease.OutQuart)
            .OnComplete(() => {
                // Sonra pozisyonlar� orijinale d�nd�r
                unitSelectionPanel.transform.DOLocalMove(originalPanelPosition, animationDuration * 0.7f)
                    .SetEase(Ease.OutQuart)
                    .OnComplete(() => {
                        // Animasyon bitince panel'i deaktif et
                        unitSelectionPanel.SetActive(false);
                    });

                superPowerBtn.transform.DOLocalMove(originalBtnPosition, animationDuration * 0.7f)
                    .SetEase(Ease.OutQuart);
            });

        Debug.Log("Panel kapat�ld� ve yukar�ya kayd�");
    }

    // Soldier button'a t�kland���nda
    public void OnSoldierButtonClicked()
    {
        if (soldierState == SoldierState.Closed)
        {
            // Soldier kapal�ysa a�
            soldierState = SoldierState.Open;
            turretState = TurretState.Closed; // Di�erini kapat
            currentSelectedUnit = UnitType.Soldier;

            if (!isPanelDown)
            {
                // Panel kapal�ysa a�
                OpenPanelWithAnimation();
            }
            else
            {
                // Panel a��ksa i�eri�i de�i�tir (restart animasyon)
                RestartPanelAnimation();
            }
        }
        else
        {
            // Soldier zaten a��ksa kapat
            soldierState = SoldierState.Closed;
            currentSelectedUnit = UnitType.initial;
            ClosePanelWithAnimation();
        }

        UpdateButtonVisuals();
        Debug.Log($"Soldier: {soldierState}, Turret: {turretState}");
    }

    // Turret button'a t�kland���nda
    public void OnTurretButtonClicked()
    {
        if (turretState == TurretState.Closed)
        {
            // Turret kapal�ysa a�
            turretState = TurretState.Open;
            soldierState = SoldierState.Closed; // Di�erini kapat
            currentSelectedUnit = UnitType.Turret;

            if (!isPanelDown)
            {
                // Panel kapal�ysa a�
                OpenPanelWithAnimation();
            }
            else
            {
                // Panel a��ksa i�eri�i de�i�tir (restart animasyon)
                RestartPanelAnimation();
            }
        }
        else
        {
            // Turret zaten a��ksa kapat
            turretState = TurretState.Closed;
            currentSelectedUnit = UnitType.initial;
            ClosePanelWithAnimation();
        }

        UpdateButtonVisuals();
        Debug.Log($"Soldier: {soldierState}, Turret: {turretState}");
    }

    // Panel i�eri�i de�i�ti�inde restart animasyonu
    void RestartPanelAnimation()
    {
        // H�zl�ca fade out yap
        panelCanvasGroup.DOFade(0f, 0.1f)
            .OnComplete(() => {
                // Panel i�eri�ini g�ncelle (buraya unit-specific i�erik kodlar� gelecek)
                UpdatePanelContent();

                // Tekrar fade in yap
                panelCanvasGroup.DOFade(1f, 0.2f);
            });
    }

    // Panel i�eri�ini g�ncelle
    void UpdatePanelContent()
    {
        switch (currentSelectedUnit)
        {
            case UnitType.Soldier:
                Debug.Log("Panel i�eri�i Soldier'a ayarland�");
                // Soldier specific UI updates
                UpdateButtonVisuals();
                break;
            case UnitType.Turret:
                Debug.Log("Panel i�eri�i Turret'e ayarland�");
                // Turret specific UI updates
                UpdateButtonVisuals();
                break;
        }
    }

    // Button g�rsellerini g�ncelle (se�ili/se�ili de�il)
    void UpdateButtonVisuals()
    {
        // Soldier button g�r�n�m�
        if (soldierState == SoldierState.Open)
        {
            soldierButton.GetComponent<Image>().color = Color.gray; // Se�ili
        }
        else
        {
            soldierButton.GetComponent<Image>().color = Color.white; // Normal
        }

        // Turret button g�r�n�m�
        if (turretState == TurretState.Open)
        {
            turretButton.GetComponent<Image>().color = Color.gray; // Se�ili
        }
        else
        {
            turretButton.GetComponent<Image>().color = Color.white; // Normal
        }
    }
}
