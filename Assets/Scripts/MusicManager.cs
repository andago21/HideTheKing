using UnityEngine;
using UnityEngine.SceneManagement;
using Mirror;

public class MusicManager : MonoBehaviour
{
    public static MusicManager Instance;

    [Header("Main Menu Music (Start Menu + Lobbies)")]
    public AudioClip mainMenuMusic;

    [Header("Theme Music")]
    public AudioClip cyberpunkMusic;
    public AudioClip medievalMusic;
    public AudioClip piratesMusic;
    public AudioClip spaceMusic;
    public AudioClip timetravelMusic;
    public AudioClip wildwestMusic;

    [Header("Win / Loss")]
    public AudioClip victorySound;
    public AudioClip defeatSound;

    private AudioSource _musicSource;
    private AudioSource _sfxSource;
    private string      _currentClipName = "";

    private void Awake()
    {
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        _musicSource             = gameObject.AddComponent<AudioSource>();
        _musicSource.loop        = true;
        _musicSource.playOnAwake = false;

        _sfxSource               = gameObject.AddComponent<AudioSource>();
        _sfxSource.loop          = false;
        _sfxSource.playOnAwake   = false;

        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void Start()
    {
        OnSceneLoaded(SceneManager.GetActiveScene(), LoadSceneMode.Single);
    }

    private void OnDestroy()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        string name = scene.name;

        // Start Menu, ChooseGameMode, oder Lobby → Main Menu Musik weiterlaufen lassen
        bool isMenuScene = name == "StartScene" || name == "StartMenu" 
                        || name.Contains("Lobby") || name.Contains("ChooseGameMode")
                        || name.Contains("Choose") || name.Contains("Menu");
        if (isMenuScene)
        {
            PlayMusic(mainMenuMusic);
            return;
        }

        // Spielszene — Main Menu Musik stoppen
        StopMusic();

        // Singleplayer → sofort Theme Musik
        if (!NetworkClient.active && !NetworkServer.active)
        {
            PlayThemeForScene(name);
        }
        // Multiplayer → warten auf RpcStartGame (StartThemeMusic wird aufgerufen)
    }

    // Wird von ChessNetworkManager.RpcStartGame aufgerufen
    public void StartThemeMusic()
    {
        PlayThemeForScene(SceneManager.GetActiveScene().name);
    }

    private void PlayThemeForScene(string sceneName)
    {
        if      (sceneName.Contains("CyberPunk"))                                        PlayMusic(cyberpunkMusic);
        else if (sceneName.Contains("MedivalBattle") || sceneName.Contains("Medieval"))  PlayMusic(medievalMusic);
        else if (sceneName.Contains("Pirates"))                                           PlayMusic(piratesMusic);
        else if (sceneName.Contains("SpaceOdyseey") || sceneName.Contains("Space"))      PlayMusic(spaceMusic);
        else if (sceneName.Contains("TimeTravel"))                                        PlayMusic(timetravelMusic);
        else if (sceneName.Contains("WildWest"))                                          PlayMusic(wildwestMusic);
    }

    private void PlayMusic(AudioClip clip)
    {
        if (clip == null) return;
        if (_currentClipName == clip.name && _musicSource.isPlaying) return;

        _currentClipName      = clip.name;
        _musicSource.clip     = clip;
        _musicSource.loop     = true;
        _musicSource.volume   = 1f;
        _musicSource.Play();
        Debug.Log($"[Music] Playing: {clip.name}");
    }

    // Win/Loss — Musik stoppen, Sound einmal abspielen
    public void PlayVictory()
    {
        StopMusic();
        if (victorySound != null)
            _sfxSource.PlayOneShot(victorySound);
    }

    public void PlayDefeat()
    {
        StopMusic();
        if (defeatSound != null)
            _sfxSource.PlayOneShot(defeatSound);
    }

    public void StopMusic()
    {
        _musicSource.Stop();
        _currentClipName = "";
    }
}