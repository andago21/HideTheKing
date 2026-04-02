using UnityEngine;
using UnityEngine.UI;
using Mirror;
using TMPro;
using UnityEngine.SceneManagement;

public class LobbyUI : MonoBehaviour
{
    [Header("Connection UI")]
    public Button hostButton;
    public Button joinButton;
    public TMP_InputField ipInputField;
    public TMP_Text waitingForPlayersText;
    public TMP_Text errorText;

    [Header("Theme Buttons (Host only)")]
    public Button piratesButton;
    public Button medievalButton;
    public Button cyberpunkButton;
    public Button spaceButton;
    public Button wildButton;
    public Button timeButton;

    [Header("Timer Buttons (Host only)")]
    public Button fiveMinButton;
    public Button tenMinButton;
    public Button fifteenMinButton;

    [Header("Arrow Followers")]
    public GameObject themeArrow;
    public GameObject timerArrow;

    private const ushort PORT_CLASSIC         = 7777;
    private const ushort PORT_BATTLE_CHESS    = 7778;
    private const ushort PORT_HIDE_THE_KING   = 7779;
    private const ushort PORT_CROWN_CONFUSION = 7780;

    private static readonly string[] CLASSIC_SCENES = {
        "ClassicGameModePirates",
        "ClassicGameModeMedivalBattle",
        "ClassicGameModeCyberPunk",
        "ClassicGameModeSpaceOdyseey",
        "ClassicGameModeWildWest",
        "ClassicGameModeTimeTravel"
    };

    private static readonly string[] BATTLE_SCENES = {
        "BattleChessGameModePirates",
        "BattleChessGameModeMedivalBattle",
        "BattleChessGameModeCyberPunk",
        "BattleChessGameModeSpaceOdyseey",
        "BattleChessGameModeWildWest",
        "BattleChessGameModeTimeTravel"
    };

    private static readonly string[] HIDE_THE_KING_SCENES = {
        "HideTheKingGameModePirates",
        "HideTheKingGameModeMedivalBattle",
        "HideTheKingGameModeCyberPunk",
        "HideTheKingGameModeSpaceOdyseey",
        "HideTheKingGameModeWildWest",
        "HideTheKingGameModeTimeTravel"
    };

    private static readonly string[] CROWN_CONFUSION_SCENES = {
        "CrownOfConfussionsGameModePirates",
        "CrownOfConfussionsGameModeMedivalBattle",
        "CrownOfConfussionsGameModeCyberPunk",
        "CrownOfConfussionsGameModeSpaceOdyseey",
        "CrownOfConfussionsGameModeWildWest",
        "CrownOfConfussionsGameModeTimeTravel"
    };

    private ushort   _currentPort;
    private string   _currentModeName;
    private string[] _scenePool;

    // Selected by host
    private int _selectedThemeIndex = 0; // 0=Pirates, 1=Medieval, 2=Cyberpunk, 3=Space, 4=Wild, 5=Time
    private int _selectedTimerMinutes = 5;

    private void Start()
    {
        string sceneName = SceneManager.GetActiveScene().name;

        if (sceneName.Contains("BattleChess"))
        {
            _currentPort     = PORT_BATTLE_CHESS;
            _currentModeName = "Battle Chess";
            _scenePool       = BATTLE_SCENES;
        }
        else if (sceneName.Contains("HideTheKing"))
        {
            _currentPort     = PORT_HIDE_THE_KING;
            _currentModeName = "Hide The King";
            _scenePool       = HIDE_THE_KING_SCENES;
        }
        else if (sceneName.Contains("CrownOfConfussions"))
        {
            _currentPort     = PORT_CROWN_CONFUSION;
            _currentModeName = "Crown Of Confusion";
            _scenePool       = CROWN_CONFUSION_SCENES;
        }
        else
        {
            _currentPort     = PORT_CLASSIC;
            _currentModeName = "Classic Chess";
            _scenePool       = CLASSIC_SCENES;
        }

        SetTransportPort(_currentPort);

        if (errorText != null)         errorText.gameObject.SetActive(false);
        if (waitingForPlayersText != null) waitingForPlayersText.gameObject.SetActive(false);

        // Theme buttons
        if (piratesButton  != null) piratesButton.onClick.AddListener(()  => SelectTheme(0, piratesButton));
        if (medievalButton != null) medievalButton.onClick.AddListener(() => SelectTheme(1, medievalButton));
        if (cyberpunkButton!= null) cyberpunkButton.onClick.AddListener(()=> SelectTheme(2, cyberpunkButton));
        if (spaceButton    != null) spaceButton.onClick.AddListener(()    => SelectTheme(3, spaceButton));
        if (wildButton     != null) wildButton.onClick.AddListener(()     => SelectTheme(4, wildButton));
        if (timeButton     != null) timeButton.onClick.AddListener(()     => SelectTheme(5, timeButton));

        // Timer buttons
        if (fiveMinButton   != null) fiveMinButton.onClick.AddListener(()   => SelectTimer(5,  fiveMinButton));
        if (tenMinButton    != null) tenMinButton.onClick.AddListener(()    => SelectTimer(10, tenMinButton));
        if (fifteenMinButton!= null) fifteenMinButton.onClick.AddListener(()=> SelectTimer(15, fifteenMinButton));

        // Connection buttons
        hostButton.onClick.AddListener(OnHostClicked);
        joinButton.onClick.AddListener(OnJoinClicked);

        // Default selection
        SelectTheme(0, piratesButton);
        SelectTimer(5, fiveMinButton);
    }

    private Button _selectedThemeButton;
    private Button _selectedTimerButton;

    private void SelectTheme(int index, Button btn)
    {
        _selectedThemeIndex = index;
        Debug.Log($"[LobbyUI] Theme selected: index={index} scene={_scenePool[index]}");
        if (themeArrow != null && btn != null)
            themeArrow.transform.position = new Vector3(
                themeArrow.transform.position.x,
                btn.transform.position.y,
                themeArrow.transform.position.z);
        _selectedThemeButton = btn;
    }

    private void SelectTimer(int minutes, Button btn)
    {
        _selectedTimerMinutes = minutes;
        Debug.Log($"[LobbyUI] Timer selected: {minutes} minutes");
        if (timerArrow != null && btn != null)
            timerArrow.transform.position = new Vector3(
                timerArrow.transform.position.x,
                btn.transform.position.y,
                timerArrow.transform.position.z);
        _selectedTimerButton = btn;
    }

    private void SetTransportPort(ushort port)
    {
        var kcp = Transport.active as kcp2k.KcpTransport;
        if (kcp != null)
        {
            kcp.Port = port;
            Debug.Log($"[LobbyUI] Port: {port} ({_currentModeName})");
        }
    }

    private void OnHostClicked()
    {
        // Use selected theme
        string selectedScene = _scenePool[_selectedThemeIndex];
        NetworkManager.singleton.onlineScene = selectedScene;

        // Save timer setting
        PlayerPrefs.SetInt("SelectedTimerMinutes", _selectedTimerMinutes);
        PlayerPrefs.Save();

        Debug.Log($"[LobbyUI] Host starts {_currentModeName}");
        Debug.Log($"[LobbyUI] Selected theme index: {_selectedThemeIndex}");
        Debug.Log($"[LobbyUI] Selected scene: {selectedScene}");
        Debug.Log($"[LobbyUI] Selected timer: {_selectedTimerMinutes} minutes");

        SetTransportPort(_currentPort);
        NetworkManager.singleton.StartHost();

        hostButton.gameObject.SetActive(false);
        joinButton.gameObject.SetActive(false);
        ipInputField.gameObject.SetActive(false);

        // Hide theme/timer options
        SetThemeTimerVisible(false);

        if (waitingForPlayersText != null)
            waitingForPlayersText.gameObject.SetActive(true);

        if (errorText != null)
            errorText.gameObject.SetActive(false);
    }

    private void OnJoinClicked()
    {
        string ip = string.IsNullOrWhiteSpace(ipInputField.text) ? "localhost" : ipInputField.text.Trim();

        SetTransportPort(_currentPort);
        NetworkManager.singleton.networkAddress = ip;
        NetworkManager.singleton.StartClient();

        hostButton.interactable = false;
        joinButton.interactable = false;

        // Hide theme/timer — client doesn't choose
        SetThemeTimerVisible(false);

        if (errorText != null)
            errorText.gameObject.SetActive(false);

        Debug.Log($"[LobbyUI] Client connects {ip}:{_currentPort} ({_currentModeName})");

        StartCoroutine(CheckConnectionTimeout(ip));
    }

    private void SetThemeTimerVisible(bool visible)
    {
        Button[] themeButtons = { piratesButton, medievalButton, cyberpunkButton, spaceButton, wildButton, timeButton };
        Button[] timerButtons = { fiveMinButton, tenMinButton, fifteenMinButton };

        foreach (var b in themeButtons) if (b != null) b.gameObject.SetActive(visible);
        foreach (var b in timerButtons) if (b != null) b.gameObject.SetActive(visible);

        if (themeArrow != null) themeArrow.SetActive(visible);
        if (timerArrow != null) timerArrow.SetActive(visible);
    }

    private System.Collections.IEnumerator CheckConnectionTimeout(string ip)
    {
        float timeout = 5f;
        float elapsed = 0f;

        while (elapsed < timeout)
        {
            if (Mirror.NetworkClient.isConnected)
            {
                Debug.Log("[LobbyUI] Connection successful");
                yield break;
            }
            elapsed += UnityEngine.Time.deltaTime;
            yield return null;
        }

        if (!Mirror.NetworkClient.isConnected)
        {
            Debug.Log("[LobbyUI] No Server found");
            Mirror.NetworkManager.singleton.StopClient();

            hostButton.interactable = true;
            joinButton.interactable = true;

            // Show theme/timer again
            SetThemeTimerVisible(true);

            if (errorText != null)
            {
                errorText.text = $"No Server Found On {ip}";
                errorText.gameObject.SetActive(true);
            }
        }
    }
}