using UnityEngine;
using UnityEngine.UI;
using Mirror;
using TMPro;
using System;

public class LobbyUI : MonoBehaviour
{
    [Header("UI References")]
    public Button hostButton;
    public Button joinButton;
    public TMP_InputField ipInputField;
    public TMP_Text waitingForPlayersText;  // "State: Waiting for Opponent..."

    private void Start()
    {
        // Hide waiting text at start
        if (waitingForPlayersText != null)
            waitingForPlayersText.gameObject.SetActive(false);

        // Wire up buttons
        hostButton.onClick.AddListener(OnHostClicked);
        joinButton.onClick.AddListener(OnJoinClicked);
    }

    private void OnHostClicked()
    {
        NetworkManager.singleton.StartHost();

        // Show waiting text, hide buttons
        hostButton.gameObject.SetActive(false);
        joinButton.gameObject.SetActive(false);
        ipInputField.gameObject.SetActive(false);

        if (waitingForPlayersText != null)
            waitingForPlayersText.gameObject.SetActive(true);

        Debug.Log("Hosting... waiting for opponent");
    }

    private void OnJoinClicked()
    {
        string ip = ipInputField.text?.Trim() ?? "localhost";

        if (string.IsNullOrEmpty(ip))
        {
            Debug.LogWarning("No IP entered!");
            return;
        }

        NetworkManager.singleton.networkAddress = ip;
        NetworkManager.singleton.StartClient();

        // Disable buttons while connecting
        hostButton.interactable = false;
        joinButton.interactable = false;

        Debug.Log("Connecting to: " + ip);
    }
}