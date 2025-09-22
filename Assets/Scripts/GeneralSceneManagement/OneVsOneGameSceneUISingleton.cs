using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Net;
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

    [SerializeField] private Transform unitMenuContent; // Panel alt�ndaki container
    [SerializeField] private GameObject unitBtnPrefab; // Slot prefab

    [SerializeField] private TextMeshProUGUI playerCurrenthHealth;
    [SerializeField] private TextMeshProUGUI myScrapTexter;
    [SerializeField] private TextMeshProUGUI myExpTexter;

    //variables for unit selection
    [SerializeField] private UnitData[] allSoldiers;
    [SerializeField] private TurretData[] allTurrets;


    //Player Comps
    private GameObject myPlayerLocalObject;

    private Player_Game_Mode_Manager myPlayerAge;

    private PlayerProductionManagement myBarracks;


    [SerializeField] private int selecetedTurretPos = -1;
    [SerializeField] private bool selectingTurretPos = false;
    private string pendingTurretUnitId; // ge�ici olarak tutulacak unit id
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
        allTurrets = Resources.LoadAll<TurretData>("TurretData");
        Debug.Log($"Found {allTurrets.Length} Turrets");
    }

    private void Update()
    {
        if (selectingTurretPos)
        {
            if (Input.GetMouseButtonDown(0)) // sadece sol t�k ile se�
            {
                Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
                RaycastHit hit;

                if (Physics.Raycast(ray, out hit))
                {
                    if (hit.collider.gameObject.name == "TurretPos1")
                        selecetedTurretPos = 1;
                    else if (hit.collider.gameObject.name == "TurretPos2")
                        selecetedTurretPos = 2;
                    else if (hit.collider.gameObject.name == "TurretPos3")
                        selecetedTurretPos = 3;

                    if (selecetedTurretPos != -1)
                    {
                        Debug.Log($"Se�ilen Pos: {selecetedTurretPos}, Unit: {pendingTurretUnitId}");

                        // �retimi ba�lat
                        myBarracks.QueueTurretServerRpc(pendingTurretUnitId, selecetedTurretPos);

                        // resetle
                        selectingTurretPos = false;
                        pendingTurretUnitId = null;
                        selecetedTurretPos = -1;
                    }
                }
            }
            if (Input.GetMouseButtonDown(1)) // sa� t�k = iptal
            {
                Debug.Log("Turret pozisyon se�me i�lemi iptal edildi.");

                selectingTurretPos = false;
                pendingTurretUnitId = null;
                selecetedTurretPos = -1;
            }
        }

    }
    private IEnumerator InitializeWhenReady()
    {
        yield return new WaitUntil(() =>
            NetworkManager.Singleton != null &&
            NetworkManager.Singleton.LocalClient?.PlayerObject != null);

        // Local player objesini kaydet
        myPlayerLocalObject = NetworkManager.Singleton.LocalClient.PlayerObject.gameObject;

        // Barracks referans�
        myBarracks = myPlayerLocalObject.GetComponentInChildren<PlayerProductionManagement>();

        // Age referans�
        myPlayerAge = myPlayerLocalObject.GetComponent<Player_Game_Mode_Manager>();


    }


    public void OnSoldierMenuBtnClicked()
    {
        

        CalculateUniteMenu();
    }

    public void OnTurretMenuBtnClicked()
    {
        

        CalculateTurretMenu();
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
                GameObject unitBtnPrfGo = Instantiate(unitBtnPrefab, unitMenuContent);
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
    private void CalculateTurretMenu()
    {
        // �nce paneli temizle
        foreach (Transform child in unitMenuContent)
            Destroy(child.gameObject);

        // �a� bilgisi
        var age = myPlayerAge.age;

   
        foreach (var unit in allTurrets)
        {
            if (unit.age == age)
            {
                GameObject unitBtnPrfGo = Instantiate(unitBtnPrefab, unitMenuContent);
                unitBtnPrfGo.name = unit.id;

                Button unitBtnPrfBtn = unitBtnPrfGo.GetComponent<Button>();

                string id = unit.id; // closure
                unitBtnPrfBtn.onClick.AddListener(() => TurretsProductionBtnCliecked(id));

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
    private void TurretsProductionBtnCliecked(string unitId)
    {
        Debug.Log("Turret �retim butonuna bast�n: " + unitId);
        selectingTurretPos = true;
        pendingTurretUnitId = unitId; // ge�ici kaydet



    }
    private void TurretsProductionBtnCliecked2(string unitId)
    {


            Debug.Log("Asker �retim Btn " + unitId + " | Pos: " + selecetedTurretPos);
        if (selecetedTurretPos != -1) // yani ge�erli bir pozisyon se�ildi
            myBarracks.QueueTurretServerRpc(unitId, selecetedTurretPos);


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
