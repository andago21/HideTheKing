using UnityEngine;
using System.IO;

[System.Serializable]
public class EloData
{
    public int elo   = 1200;
    public int wins  = 0;
    public int losses = 0;
    public int draws = 0;
}

public class EloManager : MonoBehaviour
{
    public static EloManager Instance;

    private const int   K_FACTOR    = 32;
    private const int   DEFAULT_ELO = 1200;
    private const string FILE_NAME  = "elo.json";

    private EloData _data;
    private string  FilePath => Path.Combine(Application.persistentDataPath, FILE_NAME);

    private void Awake()
    {
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
        Load();
    }

    // ── Laden ──
    private void Load()
    {
        if (File.Exists(FilePath))
        {
            string json = File.ReadAllText(FilePath);
            _data = JsonUtility.FromJson<EloData>(json);
            Debug.Log($"[ELO] Geladen: {_data.elo}");
        }
        else
        {
            _data = new EloData();
            Save();
            Debug.Log("[ELO] Neue ELO-Datei erstellt (1200)");
        }
    }

    // ── Speichern ──
    private void Save()
    {
        string json = JsonUtility.ToJson(_data, true);
        File.WriteAllText(FilePath, json);
        Debug.Log($"[ELO] Gespeichert: {_data.elo}");
    }

    // ── ELO berechnen und speichern ──
    // result: 1 = Gewonnen, 0 = Verloren, 0.5 = Unentschieden
    // opponentElo: ELO des Gegners (vom Netzwerk empfangen)
    public void UpdateElo(float result, int opponentElo)
    {
        float expected = 1f / (1f + Mathf.Pow(10f, (opponentElo - _data.elo) / 400f));
        int change     = Mathf.RoundToInt(K_FACTOR * (result - expected));
        _data.elo     += change;

        if (_data.elo < 0) _data.elo = 0;

        if      (result == 1f)   _data.wins++;
        else if (result == 0f)   _data.losses++;
        else                     _data.draws++;

        Debug.Log($"[ELO] Ergebnis: {result} | Gegner: {opponentElo} | Änderung: {change:+#;-#;0} | Neu: {_data.elo}");
        Save();
    }

    // ── Getter ──
    public int    GetElo()    => _data.elo;
    public int    GetWins()   => _data.wins;
    public int    GetLosses() => _data.losses;
    public int    GetDraws()  => _data.draws;

    public string GetRank()
    {
        int elo = _data.elo;
        if      (elo < 1000) return "Beginner";
        else if (elo < 1200) return "Bronze";
        else if (elo < 1400) return "Silver";
        else if (elo < 1600) return "Gold";
        else if (elo < 1800) return "Platinum";
        else                 return "Diamond";
    }
}