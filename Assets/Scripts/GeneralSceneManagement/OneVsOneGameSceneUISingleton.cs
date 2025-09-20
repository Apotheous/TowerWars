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

    [SerializeField] private Barracks myBarracks;
    [SerializeField] private Button soldierMenuBtn;
    [SerializeField] private Button turretMenuBtn;

    [SerializeField] private GameObject unitMenuPanel;

    [SerializeField] private Transform unitMenuContent; // Panel alt�ndaki container
    [SerializeField] private GameObject unitSlotPrefab; // Slot prefab

    [SerializeField] private TextMeshProUGUI playerCurrenthHealth;
    [SerializeField] private TextMeshProUGUI myScrapTexter;
    [SerializeField] private TextMeshProUGUI myExpTexter;

    //variables for unit selection
    [SerializeField] private UnitData[] allSoldiers;
    [SerializeField] private GameObject myPlayerLocalObject;

    [SerializeField] private Player_Game_Mode_Manager myPlayerAge;
    

    private void Awake()
    {
        // E�er zaten bir Instance varsa ve bu o de�ilse yok et
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        
    }



    private void Start()
    {
        StartCoroutine(InitializeWhenReady());
        allSoldiers = Resources.LoadAll<UnitData>("UnitData");
        Debug.Log($"Found {allSoldiers.Length} units");
    }

    private IEnumerator InitializeWhenReady()
    {
        yield return new WaitUntil(() =>
            NetworkManager.Singleton != null &&
            NetworkManager.Singleton.LocalClient?.PlayerObject != null);

        // Local player objesini kaydet
        myPlayerLocalObject = NetworkManager.Singleton.LocalClient.PlayerObject.gameObject;

        // Barracks referans�
        myBarracks = myPlayerLocalObject.GetComponentInChildren<Barracks>();

        // Age referans�
        myPlayerAge = myPlayerLocalObject.GetComponent<Player_Game_Mode_Manager>();

        // Art�k UI men�s�n� a�abilirsin
        CalculateUniteMenu();
    }


    public void OnSoldierMenuBtnClicked()
    {
        

        CalculateUniteMenu();
    }

    private void CalculateUniteMenu()
    {
        // �nce paneli temizle
        foreach (Transform child in unitMenuContent)
            Destroy(child.gameObject);

        // �a� bilgisi
        var age = myPlayerAge.age;

   
        foreach (var unit in allSoldiers)
        {
            if (unit.age == age)
            {
                GameObject unitBtnPrfGo = Instantiate(unitSlotPrefab, unitMenuContent);
                unitBtnPrfGo.name = unit.id;

                Button unitBtnPrfBtn = unitBtnPrfGo.GetComponent<Button>();

                string id = unit.id; // closure
                unitBtnPrfBtn.onClick.AddListener(() => SoldiersProductionBtnCliecked(id));

                Image iconImage = unitBtnPrfGo.GetComponentInChildren<Image>();
                if (iconImage != null)
                {
                    iconImage.sprite = unit.icon;
                }
            }
        }
    }

    /// <summary>
    /// Soldierlerin �retilmesi amac�yla bas�lan buton
    /// </summary>
    /// <param name="sPB"></param>

    private void SoldiersProductionBtnCliecked(string unitId)
    {
        Debug.Log("Asker �retim Btn " + unitId);
        myBarracks.QueueUnitServerRpc(unitId);
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
