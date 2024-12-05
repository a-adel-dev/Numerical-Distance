using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using System;
using System.Diagnostics;
using System.Linq;
using System.IO;
using System.Text;
using TMPro;
using Debug = UnityEngine.Debug;

public class NumericalDistance : MonoBehaviour
{
    [Header("UI Components")]
    [SerializeField] TextMeshProUGUI numberDisplayText;
    [SerializeField] Button greaterThanButton;
    [SerializeField] Button lessThanButton;
    [SerializeField] Button startButton;
    [SerializeField] TextMeshProUGUI timerText;
    [SerializeField] TextMeshProUGUI resultText;
    [SerializeField] TextMeshProUGUI promptText;
    [SerializeField] int trialNumber;
    public GameObject startPanel;
    public GameObject testPanel;

    [Header("Test Configuration")]
    public int totalTrials = 10;
    public float easyNumberThreshold = 2f;
    public float hardNumberThreshold = 4f;
    
    [Header("CSV Export")]
    public string fileName = "NumberComparisonResults";

    private List<TrialResult> trialResults = new List<TrialResult>();
    private int currentTrial = 0;
    private float trialStartTime;
    private int currentNumber;
    private bool responseGiven;
    private Stopwatch testStopwatch;

    [Serializable]
    private class TrialResult
    {
        public int TrialNumber { get; set; }
        public int Number { get; set; }
        public bool IsEasyTrial { get; set; }
        public float ResponseTime { get; set; }
        public bool WasCorrect { get; set; }
        public bool UserChoseGreater { get; set; }
        public long ElapsedMilliseconds { get; set; } 
    }

    void Start()
    {
        startButton.onClick.AddListener(StartTest);
        greaterThanButton.onClick.AddListener(() => HandleResponse(true));
        lessThanButton.onClick.AddListener(() => HandleResponse(false));
        
        startPanel.SetActive(true);
        testPanel.SetActive(false);
        
        StartTest();
    }

    void StartTest()
    {
        
        startPanel.SetActive(false);
        testPanel.SetActive(true);
        
        greaterThanButton.interactable = true;
        lessThanButton.interactable = true;
        
        trialResults.Clear();
        currentTrial = 0;
        
        testStopwatch = new Stopwatch();
        testStopwatch.Start();
        
        NextTrial();
    }

    void NextTrial()
    {
        if (currentTrial >= totalTrials)
        {
            GenerateReport();
            ExportToCSV();
            return;
        }

        // Generate number with even distribution of easy and hard trials
        currentNumber = GenerateNumber();
        numberDisplayText.text = currentNumber.ToString();

        responseGiven = false;
        trialStartTime = Time.time;
        currentTrial++;
    }

    int GenerateNumber()
    {
        // Evenly distribute easy and hard numbers
        if (currentTrial % 2 == 0)
        {
            // Easy numbers: 1 or 9
            return UnityEngine.Random.value > 0.5f ? 1 : 9;
        }
        else
        {
            // Hard numbers: 4 or 6
            return UnityEngine.Random.value > 0.5f ? 4 : 6;
        }
    }

    void HandleResponse(bool choseGreater)
    {
        if (responseGiven) return;

        responseGiven = true;
        float responseTime = (Time.time - trialStartTime) * 1000; // Convert to milliseconds

        bool isCorrect = (choseGreater && currentNumber > 5) || 
                         (!choseGreater && currentNumber < 5);

        trialResults.Add(new TrialResult
        {
            Number = currentNumber,
            IsEasyTrial = (currentNumber == 1 || currentNumber == 9),
            ResponseTime = responseTime,
            WasCorrect = isCorrect,
            UserChoseGreater = choseGreater,
            ElapsedMilliseconds = testStopwatch.ElapsedMilliseconds
        });

        NextTrial();
    }

    void GenerateReport()
    {
        // Disable buttons
        greaterThanButton.interactable = false;
        lessThanButton.interactable = false;

        // Calculate statistics
        var easyTrials = trialResults.Where(t => t.IsEasyTrial).ToList();
        var hardTrials = trialResults.Where(t => !t.IsEasyTrial).ToList();
        promptText.text = "";
        string report = "Test Report:\n\n" +
            $"Total Trials: {totalTrials}\n" +
            $"Easy Trials Correct: {easyTrials.Count(t => t.WasCorrect)} / {easyTrials.Count}\n" +
            $"Hard Trials Correct: {hardTrials.Count(t => t.WasCorrect)} / {hardTrials.Count}\n\n" +
            $"Average Easy Trial Time: {easyTrials.Average(t => t.ResponseTime):F2} ms\n" +
            $"Average Hard Trial Time: {hardTrials.Average(t => t.ResponseTime):F2} ms\n\n" +
            $"Total Accuracy: {trialResults.Count(t => t.WasCorrect)} / {totalTrials}";

        resultText.text = report;
        numberDisplayText.text = "";
        // numberDisplayText.text = "Test Complete";
    }

    private void Update()
    {
        
    }
    
    void ExportToCSV()
    {
        // Determine the path to save the CSV file
        string path = GetUniquePath();

        try
        {
            using (StreamWriter writer = new StreamWriter(path, false, Encoding.UTF8))
            {
                // Write header
                writer.WriteLine("Trial Number,Number,Is Easy Trial,Response Time (ms),Was Correct,User Chose Greater,Timestamp");

                // Write data rows
                foreach (var result in trialResults)
                {
                    writer.WriteLine(
                        $"{result.TrialNumber}," +
                        $"{result.Number}," +
                        $"{result.IsEasyTrial}," +
                        $"{result.ResponseTime:F2}," +
                        $"{result.WasCorrect}," +
                        $"{result.UserChoseGreater}," +
                        $"{result.ElapsedMilliseconds}"
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
        // Ensure the Exports folder exists
        string exportFolder = Path.Combine(Application.persistentDataPath, "Exports");
        Directory.CreateDirectory(exportFolder);

        // Create a filename with timestamp to avoid overwriting
        string timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
        string filename = $"{fileName}_{trialNumberText}_{timestamp}.csv";
        
        return Path.Combine(exportFolder, filename);
    }
}