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
    public TMP_Text gameModeText;        // Zeigt aktuellen Game Mode
    public TMP_Text errorText;

    // Port pro Game Mode
    private const ushort PORT_CLASSIC     = 7777;
    private const ushort PORT_BATTLE_CHESS = 7778;

    private ushort _currentPort;
    private string _currentModeName;

    private void Start()
    {
        // Game Mode anhand der aktuellen Szene bestimmen
        string sceneName = SceneManager.GetActiveScene().name;

        if (sceneName == "BattleChessLobby")
        {
            _currentPort     = PORT_BATTLE_CHESS;
            _currentModeName = "Battle Chess";
        }
        else // ClassicChessGameMode oder andere
        {
            _currentPort     = PORT_CLASSIC;
            _currentModeName = "Classic Chess";
        }

        // Port im NetworkManager setzen
        if (NetworkManager.singleton is kcp2k.KcpTransport || Transport.active != null)
        {
            SetTransportPort(_currentPort);
        }

        // UI aktualisieren
        if (gameModeText != null)
            gameModeText.text = $"Mode: {_currentModeName} (Port {_currentPort})";

        if (errorText != null)
            errorText.gameObject.SetActive(false);

        if (waitingForPlayersText != null)
            waitingForPlayersText.gameObject.SetActive(false);

        hostButton.onClick.AddListener(OnHostClicked);
        joinButton.onClick.AddListener(OnJoinClicked);
    }

    private void SetTransportPort(ushort port)
    {
        // KCP Transport Port setzen
        var kcp = Transport.active as kcp2k.KcpTransport;
        if (kcp != null)
        {
            kcp.Port = port;
            Debug.Log($"[LobbyUI] Port gesetzt auf {port} fuer {_currentModeName}");
        }
    }

    private void OnHostClicked()
    {
        SetTransportPort(_currentPort);
        NetworkManager.singleton.StartHost();

        hostButton.gameObject.SetActive(false);
        joinButton.gameObject.SetActive(false);
        ipInputField.gameObject.SetActive(false);

        if (waitingForPlayersText != null)
            waitingForPlayersText.gameObject.SetActive(true);

        if (errorText != null)
            errorText.gameObject.SetActive(false);

        Debug.Log($"[LobbyUI] Hosting {_currentModeName} auf Port {_currentPort}");
    }

    private void OnJoinClicked()
    {
        string ip = ipInputField.text?.Trim() ?? "localhost";
        if (string.IsNullOrEmpty(ip))
        {
            Debug.LogWarning("No IP entered!");
            return;
        }

        SetTransportPort(_currentPort);
        NetworkManager.singleton.networkAddress = ip;
        NetworkManager.singleton.StartClient();

        hostButton.interactable = false;
        joinButton.interactable = false;

        if (errorText != null)
            errorText.gameObject.SetActive(false);

        Debug.Log($"[LobbyUI] Verbinde mit {ip}:{_currentPort} ({_currentModeName})");
    }
}