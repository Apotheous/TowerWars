using System.Collections;
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
    [SerializeField] private GameObject unitBtnPrefab; // Slot prefab

    [SerializeField] private TextMeshProUGUI playerCurrenthHealth;
    [SerializeField] private TextMeshProUGUI myScrapTexter;
    [SerializeField] private TextMeshProUGUI myExpTexter;
    [SerializeField] private TextMeshProUGUI myTempPointTexter;

    [SerializeField] private GameObject WinPanel;
    [SerializeField] private GameObject LosePanel;
    //variables for unit selection
    private UnitData[] allSoldiers;
    private TurretData[] allTurrets;


    //Player Comps
    private GameObject myPlayerLocalObject;

    private Player_Game_Mode_Manager myPlayerAge;

    private PlayerProductionManagement myBarracks;


    private int selecetedTurretPos = -1;
    private bool selectingTurretPos = false;
    private string pendingTurretUnitId; // geçici olarak tutulacak unit id
    //private string myTag; // geçici olarak tutulacak unit id

    // YENÝ EKLENEN DEÐÝÞKEN: Oyuncunun kendi controller'ýna referans
    private PlayerController myPlayerController;
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
        allTurrets = Resources.LoadAll<TurretData>("TurretData");
        Debug.Log($"Found {allTurrets.Length} Turrets");
        // Kullanýmý:
    
    }

    private void Update()
    {
        if (selectingTurretPos)
        {
            if (Input.GetMouseButtonDown(0)) // sadece sol týk ile seç
            {
                Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
                RaycastHit hit;

                if (Physics.Raycast(ray, out hit))
                {
                    if (hit.collider.tag == myPlayerController.gameObject.tag)
                    {
                        if (hit.collider.gameObject.name == "TurretPos1")
                            selecetedTurretPos = 1;
                        else if (hit.collider.gameObject.name == "TurretPos2")
                            selecetedTurretPos = 2;
                        else if (hit.collider.gameObject.name == "TurretPos3")
                            selecetedTurretPos = 3;
                    }
                 

                    if (selecetedTurretPos != -1)
                    {
                        Debug.Log($"Seçilen Pos: {selecetedTurretPos}, Unit: {pendingTurretUnitId}");

                    
                        if (myPlayerController != null)
                        {
                            myPlayerController.RequestTurretProduction(pendingTurretUnitId, selecetedTurretPos);
                        }
                        // resetle
                        selectingTurretPos = false;
                        pendingTurretUnitId = null;
                        selecetedTurretPos = -1;
                    }
                }
            }
            if (Input.GetMouseButtonDown(1)) // sað týk = iptal
            {
                Debug.Log("Turret pozisyon seçme iþlemi iptal edildi.");

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

        myPlayerLocalObject = NetworkManager.Singleton.LocalClient.PlayerObject.gameObject;
        myBarracks = myPlayerLocalObject.GetComponentInChildren<PlayerProductionManagement>();
        myPlayerAge = myPlayerLocalObject.GetComponent<Player_Game_Mode_Manager>();

        // YENÝ EKLENEN SATIR: PlayerController referansýný alýyoruz.
        myPlayerController = myPlayerLocalObject.GetComponent<PlayerController>();

        // Hata kontrolü
        if (myPlayerController == null)
        {
            Debug.LogError("Oyuncu objesinde PlayerController script'i bulunamadý!");
        }

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
        // Önce paneli temizle
        foreach (Transform child in unitMenuContent)
            Destroy(child.gameObject);

        // Çað bilgisi
        var age = myPlayerAge.CurrentAge;

   
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
        // Önce paneli temizle
        foreach (Transform child in unitMenuContent)
            Destroy(child.gameObject);

        // Çað bilgisi
        var age = myPlayerAge.CurrentAge;

   
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
    /// Soldierlerin üretilmesi amacýyla basýlan buton
    /// </summary>
    /// <param name="sPB"></param>

    private void SoldiersProductionBtnCliecked(string unitId)
    {
        Debug.Log("Asker Üretim Btn " + unitId);
        // YENÝ HALÝ: Komutu kendi PlayerController'ýmýza gönderiyoruz.
        if (myPlayerController != null)
        {
            myPlayerController.RequestUnitProduction(unitId);
        }
    }
    private void TurretsProductionBtnCliecked(string unitId)
    {
        Debug.Log("Turret üretim butonuna bastýn: " + unitId);
        selectingTurretPos = true;
        pendingTurretUnitId = unitId; // geçici kaydet



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
    public void PlayerTempporaryPointWrite(int exp)
    {
        myTempPointTexter.text = exp.ToString();
    }

    public void ShowGameOver(string message, bool iWon)
    {
        // Menüleri kapat
        unitMenuPanel.SetActive(false);

        // Her ihtimale karþý ikisini de kapat
        WinPanel.SetActive(false);
        LosePanel.SetActive(false);

        if (iWon)
        {
            WinPanel.SetActive(true);
            Debug.Log("GAME OVER - KAZANDIN: " + message);
            
        }
        else
        {
            LosePanel.SetActive(true);
            Debug.Log("GAME OVER - KAYBETTÝN: " + message);
            
        }
        // Skoru Cloud Save'e gönder
        var playerSc = myPlayerLocalObject.GetComponent<PlayerSC>();
        if (playerSc != null)
        {
            CloudSaveAccountManagerGameScene.Instance.UpdateScore(playerSc.myTempPoint.Value);
            Debug.Log($"[CloudSave] Oyuncu skoru güncellendi: {playerSc.myTempPoint.Value}");
        }
        else
        {
            Debug.LogWarning("[ShowGameOver] PlayerSC referansý bulunamadý, skor kaydedilemedi!");
        }
    }

}
