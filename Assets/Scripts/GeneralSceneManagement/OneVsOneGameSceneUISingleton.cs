using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using TMPro;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;

public class OneVsOneGameSceneUISingleton : MonoBehaviour
{
    public static OneVsOneGameSceneUISingleton Instance { get; private set; }


    [SerializeField] private Button soldierMenuBtn;
    [SerializeField] private Button turretMenuBtn;

    [SerializeField] private GameObject unitMenuPanel;

    [SerializeField] private Transform unitMenuContent; // Panel altýndaki container
    [SerializeField] private GameObject unitSlotPrefab; // Slot prefab

    [SerializeField] private TextMeshProUGUI playerCurrenthHealth;
    [SerializeField] private TextMeshProUGUI myScrapTexter;
    [SerializeField] private TextMeshProUGUI myExpTexter;
    [SerializeField] private UnitData[] allSoldiers;
    private void Awake()
    {
        // Eðer zaten bir Instance varsa ve bu o deðilse yok et
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        
    }


    private void Start()
    {
        allSoldiers = Resources.LoadAll<UnitData>("UnitData");
        Debug.Log($"Found {allSoldiers.Length} units");
    }



    public void OnSoldierMenuBtnClicked()
    {
        unitMenuPanel.SetActive(true);

        PopulateUnitMenu();
    }

    private void PopulateUnitMenu()
    {
        // Önce paneli temizle
        foreach (Transform child in unitMenuContent)
            Destroy(child.gameObject);

        // Local player GameObject
        var localPlayer = NetworkManager.Singleton.LocalClient.PlayerObject;
        if (localPlayer == null) return;

        // Player_Game_Mode_Manager component’i
        var modeManager = localPlayer.GetComponent<Player_Game_Mode_Manager>();
        if (modeManager == null) return;

        // Çað bilgisi
        var age = modeManager.age;

        // Çaða uygun birimleri listele
        foreach (var unit in allSoldiers)
        {
            if (unit.age == age)
            {
                GameObject slot = Instantiate(unitSlotPrefab, unitMenuContent);
                
                //slot.GetComponent<UnitSlotUI>().Setup(unit);
            }
        }
    }

    public void PlayerCurrentHealthWrite(float health)
    {
        playerCurrenthHealth.text = health.ToString();
    }

    public void PlayerScrapWrite(float scrap)
    {
        myScrapTexter.text = scrap.ToString();
    }
    public void PlayerExpWrite(float exp)
    {
        myExpTexter.text = exp.ToString();
    }



}
