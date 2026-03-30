using UnityEngine;
using UnityEngine.UI;
using Mirror;
using TMPro;
using UnityEngine.SceneManagement;

public class LobbyUI : MonoBehaviour
{
    [Header("UI References")]
    public Button hostButton;
    public Button joinButton;
    public TMP_InputField ipInputField;
    public TMP_Text waitingForPlayersText;
    public TMP_Text gameModeText;
    public TMP_Text errorText;

    private const ushort PORT_CLASSIC         = 7777;
    private const ushort PORT_BATTLE_CHESS    = 7778;
    private const ushort PORT_HIDE_THE_KING   = 7779;
    private const ushort PORT_CROWN_CONFUSION = 7780;

    private static readonly string[] CLASSIC_SCENES = {
        "ClassicGameModeCyberPunk",
        "ClassicGameModeMedivalBattle",
        "ClassicGameModePirates",
        "ClassicGameModeSpaceOdyseey",
        "ClassicGameModeTimeTravel",
        "ClassicGameModeWildWest"
    };

    private static readonly string[] BATTLE_SCENES = {
        "BattleChessGameModeCyberPunk",
        "BattleChessGameModeMedivalBattle",
        "BattleChessGameModePirates",
        "BattleChessGameModeSpaceOdyseey",
        "BattleChessGameModeTimeTravel",
        "BattleChessGameModeWildWest"
    };

     private static readonly string[] HIDE_THE_KING_SCENES = {
        "HideTheKingGameModeCyberPunk",
        "HideTheKingGameModeMedivalBattle",
        "HideTheKingGameModePirates",
        "HideTheKingGameModeSpaceOdyseey",
        "HideTheKingGameModeTimeTravel",
        "HideTheKingGameModeWildWest"
    };
 
    private static readonly string[] CROWN_CONFUSION_SCENES = {
        "CrownOfConfussionsGameModeCyberPunk",
        "CrownOfConfussionsGameModeMedivalBattle",
        "CrownOfConfussionsGameModePirates",
        "CrownOfConfussionsGameModeSpaceOdyseey",
        "CrownOfConfussionsGameModeTimeTravel",
        "CrownOfConfussionsGameModeWildWest"
    };

    private ushort   _currentPort;
    private string   _currentModeName;
    private string[] _scenePool;

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

        if (gameModeText != null)
            gameModeText.text = $"Mode: {_currentModeName}";

        if (errorText != null)
            errorText.gameObject.SetActive(false);

        if (waitingForPlayersText != null)
            waitingForPlayersText.gameObject.SetActive(false);

        hostButton.onClick.AddListener(OnHostClicked);
        joinButton.onClick.AddListener(OnJoinClicked);
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
        string randomScene = _scenePool[Random.Range(0, _scenePool.Length)];
        NetworkManager.singleton.onlineScene = randomScene;

        Debug.Log($"[LobbyUI] Host starts {_currentModeName} with Scene: {randomScene}");

        SetTransportPort(_currentPort);
        NetworkManager.singleton.StartHost();

        hostButton.gameObject.SetActive(false);
        joinButton.gameObject.SetActive(false);
        ipInputField.gameObject.SetActive(false);

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

        // Auf Verbindung warten — nach 5 Sekunden prüfen ob erfolgreich
        hostButton.interactable = false;
        joinButton.interactable = false;

        if (errorText != null)
            errorText.gameObject.SetActive(false);

        Debug.Log($"[LobbyUI] Client connects {ip}:{_currentPort} ({_currentModeName})");

        StartCoroutine(CheckConnectionTimeout(ip));
    }

    private System.Collections.IEnumerator CheckConnectionTimeout(string ip)
    {
        float timeout = 5f;
        float elapsed = 0f;

        while (elapsed < timeout)
        {
            // Verbindung erfolgreich
            if (Mirror.NetworkClient.isConnected)
            {
                Debug.Log("[LobbyUI] Connection successful");
                yield break;
            }
            elapsed += UnityEngine.Time.deltaTime;
            yield return null;
        }

        // Timeout — kein Server gefunden
        if (!Mirror.NetworkClient.isConnected)
        {
            Debug.Log("[LobbyUI] No Server found");
            Mirror.NetworkManager.singleton.StopClient();

            hostButton.interactable = true;
            joinButton.interactable = true;

            if (errorText != null)
            {
                errorText.text = $"No Server Found On {ip}";
                errorText.gameObject.SetActive(true);
            }
        }
    }
}