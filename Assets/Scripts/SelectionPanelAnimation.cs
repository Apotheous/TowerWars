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
    [SerializeField] Button soldierButton; // Soldier button referansý
    [SerializeField] Button turretButton;  // Turret button referansý
    [SerializeField] float slideDistance = 200f; // Kayma mesafesi
    [SerializeField] float animationDuration = 0.5f; // Animasyon süresi

    private Vector3 originalPanelPosition; // Panel'in orijinal pozisyonu
    private Vector3 originalBtnPosition; // Button'ýn orijinal pozisyonu
    private CanvasGroup panelCanvasGroup; // Transparency için
    private bool isPanelDown = false; // Panel aþaðýda mý?

    // Unit states - enum deðiþkenlerini tanýmla
    private UnitType currentSelectedUnit = UnitType.initial;
    private SoldierState soldierState = SoldierState.Closed;
    private TurretState turretState = TurretState.Closed;

    void Start()
    {
        // Orijinal pozisyonlarý kaydet
        originalPanelPosition = unitSelectionPanel.transform.localPosition;
        originalBtnPosition = superPowerBtn.transform.localPosition;

        // Canvas Group ekle (transparency için)
        panelCanvasGroup = unitSelectionPanel.GetComponent<CanvasGroup>();
        if (panelCanvasGroup == null)
            panelCanvasGroup = unitSelectionPanel.AddComponent<CanvasGroup>();

        // Panel baþlangýçta kapalý
        unitSelectionPanel.SetActive(false);
        panelCanvasGroup.alpha = 0f;
    }

    // Bu methodu button'a baðla
    public void TogglePanel()
    {
        if (!isPanelDown)
        {
            // Panel'i aç ve aþaðýya kaydýr
            OpenPanelWithAnimation();
        }
        else
        {
            // Panel'i kapat ve yukarýya kaydýr
            ClosePanelWithAnimation();
        }
    }

    // Panel'i açma animasyonu
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

        // Transparency animasyonu - 100 birim sonra görünür olsun
        DOVirtual.DelayedCall(animationDuration * 0.5f, () => {
            panelCanvasGroup.DOFade(1f, animationDuration * 0.5f)
                .SetEase(Ease.InQuart);
        });

        Debug.Log("Panel açýldý ve aþaðýya kaydý");
    }

    // Panel'i kapatma animasyonu
    void ClosePanelWithAnimation()
    {
        isPanelDown = false;

        // Önce transparency'yi gizle
        panelCanvasGroup.DOFade(0f, animationDuration * 0.3f)
            .SetEase(Ease.OutQuart)
            .OnComplete(() => {
                // Sonra pozisyonlarý orijinale döndür
                unitSelectionPanel.transform.DOLocalMove(originalPanelPosition, animationDuration * 0.7f)
                    .SetEase(Ease.OutQuart)
                    .OnComplete(() => {
                        // Animasyon bitince panel'i deaktif et
                        unitSelectionPanel.SetActive(false);
                    });

                superPowerBtn.transform.DOLocalMove(originalBtnPosition, animationDuration * 0.7f)
                    .SetEase(Ease.OutQuart);
            });

        Debug.Log("Panel kapatýldý ve yukarýya kaydý");
    }

    // Soldier button'a týklandýðýnda
    public void OnSoldierButtonClicked()
    {
        if (soldierState == SoldierState.Closed)
        {
            // Soldier kapalýysa aç
            soldierState = SoldierState.Open;
            turretState = TurretState.Closed; // Diðerini kapat
            currentSelectedUnit = UnitType.Soldier;

            if (!isPanelDown)
            {
                // Panel kapalýysa aç
                OpenPanelWithAnimation();
            }
            else
            {
                // Panel açýksa içeriði deðiþtir (restart animasyon)
                RestartPanelAnimation();
            }
        }
        else
        {
            // Soldier zaten açýksa kapat
            soldierState = SoldierState.Closed;
            currentSelectedUnit = UnitType.initial;
            ClosePanelWithAnimation();
        }

        UpdateButtonVisuals();
        Debug.Log($"Soldier: {soldierState}, Turret: {turretState}");
    }

    // Turret button'a týklandýðýnda
    public void OnTurretButtonClicked()
    {
        if (turretState == TurretState.Closed)
        {
            // Turret kapalýysa aç
            turretState = TurretState.Open;
            soldierState = SoldierState.Closed; // Diðerini kapat
            currentSelectedUnit = UnitType.Turret;

            if (!isPanelDown)
            {
                // Panel kapalýysa aç
                OpenPanelWithAnimation();
            }
            else
            {
                // Panel açýksa içeriði deðiþtir (restart animasyon)
                RestartPanelAnimation();
            }
        }
        else
        {
            // Turret zaten açýksa kapat
            turretState = TurretState.Closed;
            currentSelectedUnit = UnitType.initial;
            ClosePanelWithAnimation();
        }

        UpdateButtonVisuals();
        Debug.Log($"Soldier: {soldierState}, Turret: {turretState}");
    }

    // Panel içeriði deðiþtiðinde restart animasyonu
    void RestartPanelAnimation()
    {
        // Hýzlýca fade out yap
        panelCanvasGroup.DOFade(0f, 0.1f)
            .OnComplete(() => {
                // Panel içeriðini güncelle (buraya unit-specific içerik kodlarý gelecek)
                UpdatePanelContent();

                // Tekrar fade in yap
                panelCanvasGroup.DOFade(1f, 0.2f);
            });
    }

    // Panel içeriðini güncelle
    void UpdatePanelContent()
    {
        switch (currentSelectedUnit)
        {
            case UnitType.Soldier:
                Debug.Log("Panel içeriði Soldier'a ayarlandý");
                // Soldier specific UI updates
                UpdateButtonVisuals();
                break;
            case UnitType.Turret:
                Debug.Log("Panel içeriði Turret'e ayarlandý");
                // Turret specific UI updates
                UpdateButtonVisuals();
                break;
        }
    }

    // Button görsellerini güncelle (seçili/seçili deðil)
    void UpdateButtonVisuals()
    {
        // Soldier button görünümü
        if (soldierState == SoldierState.Open)
        {
            soldierButton.GetComponent<Image>().color = Color.gray; // Seçili
        }
        else
        {
            soldierButton.GetComponent<Image>().color = Color.white; // Normal
        }

        // Turret button görünümü
        if (turretState == TurretState.Open)
        {
            turretButton.GetComponent<Image>().color = Color.gray; // Seçili
        }
        else
        {
            turretButton.GetComponent<Image>().color = Color.white; // Normal
        }
    }
}
