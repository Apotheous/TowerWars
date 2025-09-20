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

    [SerializeField] private Transform unitMenuContent; // Panel altýndaki container
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

        // Barracks referansý
        myBarracks = myPlayerLocalObject.GetComponentInChildren<Barracks>();

        // Age referansý
        myPlayerAge = myPlayerLocalObject.GetComponent<Player_Game_Mode_Manager>();

        // Artýk UI menüsünü açabilirsin
        CalculateUniteMenu();
    }


    public void OnSoldierMenuBtnClicked()
    {
        

        CalculateUniteMenu();
    }

    private void CalculateUniteMenu()
    {
        // Önce paneli temizle
        foreach (Transform child in unitMenuContent)
            Destroy(child.gameObject);

        // Çað bilgisi
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
    /// Soldierlerin üretilmesi amacýyla basýlan buton
    /// </summary>
    /// <param name="sPB"></param>

    private void SoldiersProductionBtnCliecked(string unitId)
    {
        Debug.Log("Asker Üretim Btn " + unitId);
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
