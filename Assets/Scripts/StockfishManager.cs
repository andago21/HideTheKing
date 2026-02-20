using System;
using System.Diagnostics;
using System.IO;
using UnityEngine;
using System.Collections;
using Debug = UnityEngine.Debug;

public class StockfishManager : MonoBehaviour
{
    public static StockfishManager Instance;

    private Process stockfishProcess;
    private StreamWriter stockfishInput;
    private StreamReader stockfishOutput;

    [Header("Engine Settings")]
    [Tooltip("Difficulty level: 1 (easiest) to 20 (hardest)")]
    [Range(1, 20)]
    public int skillLevel = 10;

    [Tooltip("Time limit in milliseconds for engine to think")]
    public int thinkingTimeMs = 1000;

    private bool isEngineReady = false;
    private string lastBestMove = "";

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void Start()
    {
        InitializeStockfish();
    }

    private void InitializeStockfish()
    {
        string stockfishPath = GetStockfishPath();

        if (!File.Exists(stockfishPath))
        {
            Debug.LogError("Stockfish executable not found at: " + stockfishPath);
            return;
        }

        try
        {
            // Start Stockfish process
            stockfishProcess = new Process();
            stockfishProcess.StartInfo.FileName = stockfishPath;
            stockfishProcess.StartInfo.UseShellExecute = false;
            stockfishProcess.StartInfo.RedirectStandardInput = true;
            stockfishProcess.StartInfo.RedirectStandardOutput = true;
            stockfishProcess.StartInfo.RedirectStandardError = true;
            stockfishProcess.StartInfo.CreateNoWindow = true;

            stockfishProcess.Start();

            stockfishInput = stockfishProcess.StandardInput;
            stockfishOutput = stockfishProcess.StandardOutput;

            // Initialize UCI protocol
            SendCommand("uci");
            
            // Wait for engine to be ready
            StartCoroutine(WaitForEngineReady());

            Debug.Log("Stockfish engine started successfully");
        }
        catch (Exception e)
        {
            Debug.LogError("Failed to start Stockfish: " + e.Message);
        }
    }

    private IEnumerator WaitForEngineReady()
    {
        while (!stockfishOutput.EndOfStream)
        {
            string line = stockfishOutput.ReadLine();
            Debug.Log("Stockfish: " + line);

            if (line == "uciok")
            {
                // Set skill level
                SendCommand($"setoption name Skill Level value {skillLevel}");
                SendCommand("isready");
            }

            if (line == "readyok")
            {
                isEngineReady = true;
                Debug.Log("Stockfish is ready!");
                yield break;
            }

            yield return null;
        }
    }

    private string GetStockfishPath()
    {
        string streamingAssetsPath = Application.streamingAssetsPath;
        string engineFolder = Path.Combine(streamingAssetsPath, "Engine");

        #if UNITY_STANDALONE_WIN || UNITY_EDITOR_WIN
                return Path.Combine(engineFolder, "stockfish.exe");
        #elif UNITY_STANDALONE_LINUX || UNITY_EDITOR_LINUX
                return Path.Combine(engineFolder, "stockfish");
        #elif UNITY_STANDALONE_OSX || UNITY_EDITOR_OSX
                return Path.Combine(engineFolder, "stockfish");
        #else
                return Path.Combine(engineFolder, "stockfish");
        #endif
    }

    private void SendCommand(string command)
    {
        if (stockfishInput != null)
        {
            stockfishInput.WriteLine(command);
            stockfishInput.Flush();
            Debug.Log("Sent to Stockfish: " + command);
        }
    }

    /// <summary>
    /// Get the best move for the current board position
    /// </summary>
    /// <param name="fenPosition">Board position in FEN notation</param>
    public void GetBestMove(string fenPosition, Action<string> onMoveReceived)
    {
        if (!isEngineReady)
        {
            Debug.LogError("Stockfish engine is not ready yet!");
            return;
        }

        StartCoroutine(RequestBestMove(fenPosition, onMoveReceived));
    }

    private IEnumerator RequestBestMove(string fenPosition, Action<string> onMoveReceived)
    {
        // Set position
        SendCommand($"position fen {fenPosition}");
        
        // Request best move with time limit
        SendCommand($"go movetime {thinkingTimeMs}");

        // Wait for response
        while (!stockfishOutput.EndOfStream)
        {
            string line = stockfishOutput.ReadLine();
            Debug.Log("Stockfish: " + line);

            if (line.StartsWith("bestmove"))
            {
                // Parse best move (format: "bestmove e2e4")
                string[] parts = line.Split(' ');
                if (parts.Length >= 2)
                {
                    lastBestMove = parts[1];
                    Debug.Log("Best move: " + lastBestMove);
                    onMoveReceived?.Invoke(lastBestMove);
                }
                yield break;
            }

            yield return null;
        }
    }

    private void OnApplicationQuit()
    {
        CloseStockfish();
    }

    private void OnDestroy()
    {
        CloseStockfish();
    }

    private void CloseStockfish()
    {
        if (stockfishProcess != null && !stockfishProcess.HasExited)
        {
            SendCommand("quit");
            stockfishProcess.WaitForExit(1000);
            stockfishProcess.Close();
            Debug.Log("Stockfish engine closed");
        }
    }

    /// <summary>
    /// Set the difficulty level (1-20)
    /// </summary>
    public void SetSkillLevel(int level)
    {
        skillLevel = Mathf.Clamp(level, 1, 20);
        if (isEngineReady)
        {
            SendCommand($"setoption name Skill Level value {skillLevel}");
        }
    }
}