using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using TMPro;

public class FingerTapping : MonoBehaviour
{
    [Header("UI Components")]
    public TextMeshProUGUI instructionText;
    public TextMeshProUGUI sequenceText;
    public TextMeshProUGUI keyPressCountText;
    public Button startButton;
    public GameObject startPanel;
    public GameObject testPanel;

    [Header("Test Configuration")]
    public int totalTrials = 10;
    public float timeBetweenTrials = 1f;
    public int sequenceLength = 2;

    private const string EASY_LETTERS = "SK";
    private const string HARD_LETTERS = "EYDGLVN";

    private List<TrialResult> trialResults = new List<TrialResult>();
    private int currentTrial = 0;
    private string currentSequence;
    private string currentLettersTyped = "";
    private bool isTestRunning = false;
    private int keyPressCount = 0;
    private bool isEasyTrial;

    [Serializable]
    private class TrialResult
    {
        public int TrialNumber { get; set; }
        public string Sequence { get; set; }
        public bool IsEasyTrial { get; set; }
        public int KeyPressCount { get; set; }
        public bool WasSuccessful { get; set; }
        public long ElapsedMilliseconds { get; set; }
    }

    void Start()
    {
        // Set up input system
        var inputActions = new PlayerInput();
        inputActions.keyboardActions.Enable();
        inputActions.keyboardActions.AnyKey.performed += HandleKeyPress;

        // Set up UI
        startButton.onClick.AddListener(StartTest);
        InitializeUI();
    }

    void InitializeUI()
    {
        startPanel.SetActive(true);
        testPanel.SetActive(false);
        instructionText.text = "Press Start to Begin Motor Response Test";
        sequenceText.text = "";
        keyPressCountText.text = "";
    }

    public void StartTest()
    {
        // Switch UI panels
        startPanel.SetActive(false);
        testPanel.SetActive(true);

        // Reset test data
        trialResults.Clear();
        currentTrial = 0;
        isTestRunning = true;

        // Start first trial
        NextTrial();
    }

    void NextTrial()
    {
        if (currentTrial >= totalTrials)
        {
            CompleteTest();
            return;
        }

        // Alternate between easy and hard trials
        isEasyTrial = (currentTrial % 2 == 0);
        currentSequence = GenerateUniqueSequence(isEasyTrial, sequenceLength);
        
        instructionText.text = $"Type the sequence: {currentSequence}";
        sequenceText.text = "";
        currentLettersTyped = "";
        keyPressCount = 0;
        keyPressCountText.text = $"Key Presses: {keyPressCount}";

        currentTrial++;
    }

    string GenerateUniqueSequence(bool isEasy, int length)
    {
        string letters = isEasy ? EASY_LETTERS : HARD_LETTERS;
        
        // Use Fisher-Yates shuffle to randomize letter selection
        List<char> letterList = letters.ToList();
        int n = letterList.Count;
        while (n > 1) 
        {
            n--;
            int k = UnityEngine.Random.Range(0, n + 1);
            char value = letterList[k];
            letterList[k] = letterList[n];
            letterList[n] = value;
        }

        // Take the first 'length' unique letters
        HashSet<char> usedLetters = new HashSet<char>();
        string sequence = "";

        foreach (char letter in letterList)
        {
            if (sequence.Length < length)
            {
                sequence += letter;
            }
            else
            {
                break;
            }
        }

        return sequence;
    }


    void HandleKeyPress(InputAction.CallbackContext context)
    {
        if (!isTestRunning) return;

        // Get the key that was pressed
        // var pressedKey = Keyboard.current.onTextInput += GetKeyinput;
        // char keyPressed;
        // Keyboard.current.onTextInput += cha => keyPressed = cha;
        // // Debug.Log(pressedKey.ReadValue());
        // string keyPressed = pressedKey.ToString();

        // Increment key press count
        keyPressCount++;
        keyPressCountText.text = $"Key Presses: {keyPressCount}";

        // Check if the pressed key matches the next required key
        if (currentLettersTyped.Length < currentSequence.Length)
        {
            if (keyPressed == currentSequence[currentLettersTyped.Length].ToString())
            {
                currentLettersTyped += keyPressed;
                sequenceText.text = currentLettersTyped;
        
                // Check if sequence is complete
                if (currentLettersTyped == currentSequence)
                {
                    // Record trial result
                    trialResults.Add(new TrialResult
                    {
                        TrialNumber = currentTrial,
                        Sequence = currentSequence,
                        IsEasyTrial = isEasyTrial,
                        KeyPressCount = keyPressCount,
                        WasSuccessful = true
                    });
        
                    // Move to next trial after a short delay
                    Invoke(nameof(NextTrial), timeBetweenTrials);
                }
            }
            else
            {
                // Reset if incorrect key
                currentLettersTyped = "";
                sequenceText.text = "";
            }
        }
    }

    void CompleteTest()
    {
        isTestRunning = false;

        // Generate report
        var easyTrials = trialResults.Where(t => t.IsEasyTrial).ToList();
        var hardTrials = trialResults.Where(t => !t.IsEasyTrial).ToList();

        string report = "Test Report:\n\n" +
            $"Total Trials: {totalTrials}\n" +
            $"Easy Trials Avg Key Presses: {(easyTrials.Any() ? easyTrials.Average(t => t.KeyPressCount).ToString("F2") : "N/A")}\n" +
            $"Hard Trials Avg Key Presses: {(hardTrials.Any() ? hardTrials.Average(t => t.KeyPressCount).ToString("F2") : "N/A")}\n";

        instructionText.text = report;

        // Export results
        ExportToCSV();

        // Return to start panel
        Invoke(nameof(ReturnToStart), 3f);
    }

    void ReturnToStart()
    {
        startPanel.SetActive(true);
        testPanel.SetActive(false);
    }

    void ExportToCSV()
    {
        string path = GetUniquePath();

        try
        {
            using (StreamWriter writer = new StreamWriter(path, false))
            {
                // Write header
                writer.WriteLine("Trial Number,Sequence,Is Easy Trial,Key Press Count,Was Successful");

                // Write data rows
                foreach (var result in trialResults)
                {
                    writer.WriteLine(
                        $"{result.TrialNumber}," +
                        $"{result.Sequence}," +
                        $"{result.IsEasyTrial}," +
                        $"{result.KeyPressCount}," +
                        $"{result.WasSuccessful}"
                    );
                }
            }

            Debug.Log($"CSV file exported successfully to {path}");
        }
        catch (Exception e)
        {
            Debug.LogError($"Failed to export CSV: {e.Message}");
        }
    }

    string GetUniquePath()
    {
        string exportFolder = Path.Combine(Application.persistentDataPath, "Exports");
        Directory.CreateDirectory(exportFolder);

        string timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
        string filename = $"MotorResponseTest_{timestamp}.csv";
        
        return Path.Combine(exportFolder, filename);
    }
}
