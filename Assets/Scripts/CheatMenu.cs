using System;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class CheatMenu : MonoBehaviour
{
    [Header("References")]
    public ItemDatabase itemDatabase;
    public Bloomery bloom;
    public Smeltery smelt;
    public Controls playerController;
    public CameraMovements CM;

    [Header("UI")]
    [SerializeField] private TMP_InputField inputField;
    [SerializeField] private TextMeshProUGUI text;
    [SerializeField] private ScrollRect scrollRect;
    [SerializeField] private GameObject console;

    [Header("Settings")]
    [SerializeField] private int maxLines = 50;

    private bool consoleUp = false;

    // Terminal log
    private List<string> logLines = new List<string>();

    // Commands
    private Dictionary<string, Action<string[]>> commands;
    private List<string> commandList = new List<string>();

    // History
    private List<string> commandHistory = new List<string>();
    private int historyIndex = -1;

    // Autocomplete
    private List<string> suggestions = new List<string>();
    private int suggestionIndex = -1;

    //Help Dic
    private Dictionary<string, string> commandDescriptions = new Dictionary<string, string>();

    void Awake()
    {
        commands = new Dictionary<string, Action<string[]>>()
        {
            { "spawn", CmdSpawn },
            { "teleport", CmdTeleport },
            { "time", CmdTime },
            { "player", CmdPlayer },
            { "bloom", CmdBloom },
            { "smeltery", CmdSmeltery },
            { "help", CmdHelp }
        };

        commandList.AddRange(commands.Keys);

        commandDescriptions = new Dictionary<string, string>()
        {
            { "spawn", "spawn {itemID} {pos(x,y,z) or player} (optional: {heat=value} {condenced=value})" },
            { "teleport", "teleport {pos(x,y,z) or player}" },
            { "time", "time {set value | pause 0/1}" },
            { "player", "player set {walkSpeed, sprintSpeed, pickupRange, hp, stamina} value" },
            { "bloom", "bloom addHeat value" },
            { "smeltery", "smeltery addHeat value" },
            { "help", "help (lists all commands)" }
        };
    }

    void Start()
    {
        console.SetActive(false);
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.F1))
            ToggleConsole();

        if (!consoleUp) return;

        // Submit command
        if (Input.GetKeyDown(KeyCode.Return))
        {
            string cmd = inputField.text;

            if (!string.IsNullOrWhiteSpace(cmd))
            {
                commandHistory.Add(cmd);
                historyIndex = commandHistory.Count;

                HandleCommand(cmd);
            }

            inputField.text = "";
            inputField.ActivateInputField();
        }

        // History 
        if (Input.GetKeyDown(KeyCode.UpArrow))
        {
            if (commandHistory.Count == 0) return;

            historyIndex--;
            if (historyIndex < 0) historyIndex = 0;

            inputField.text = commandHistory[historyIndex];
            MoveCursorToEnd();
        }

        // History 
        if (Input.GetKeyDown(KeyCode.DownArrow))
        {
            if (commandHistory.Count == 0) return;

            historyIndex++;

            if (historyIndex >= commandHistory.Count)
            {
                historyIndex = commandHistory.Count;
                inputField.text = "";
            }
            else
            {
                inputField.text = commandHistory[historyIndex];
            }

            MoveCursorToEnd();
        }

        // Autocomplete
        if (Input.GetKeyDown(KeyCode.Tab))
        {
            HandleAutocomplete();
        }
    }

    // Console Control

    public void ToggleConsole()
    {
        consoleUp = !consoleUp;
        console.SetActive(consoleUp);

        playerController.SetMovementLocked(consoleUp);

        if (consoleUp)
        {
            inputField.text = "";
            inputField.ActivateInputField();
        }
    }

    // Command Handling

    private void HandleCommand(string input)
    {
        Log(input);

        var argsList = ParseArguments(input);
        if (argsList.Count == 0) return;

        string cmd = argsList[0].ToLower();

        if (commands.TryGetValue(cmd, out var action))
        {
            action.Invoke(argsList.ToArray());
        }
        else
        {
            Log("<color=red>Unknown command</color>");
        }
    }

    // Commands

    private void CmdSpawn(string[] args)
    {
        
        if (args.Length < 3)
        {
            Log("Usage: spawn {itemID} {pos}");
            return;
        }

        if (!int.TryParse(args[1], out int id))
        {
            Log("Invalid itemID");
            return;
        }

        Vector3 pos = ParsePosition(args[2]);

        var item = itemDatabase.GetItemDataById(id);
        if (item == null)
        {
            Log("Item not found");
            return;
        }
        
        GameObject o = itemDatabase.SpawnItem(id, pos, Quaternion.identity);
        
        if(args.Length >= 4)
        {
            bool usedArg = false;
            for (int i=3; i < args.Length; i++)
            {
                usedArg = false;
                string[] stringSplit = args[i].Split('=');
                if(stringSplit.Length != 2)
                {
                    Log("missing = at parameter " + args[i] + " not adding but still spawning");
                    break;
                }
                stringSplit[1] = stringSplit[1].Replace(" ", "");
                stringSplit[0] = stringSplit[0].Replace(" ", "");

                //need float as second switch
                if (float.TryParse(stringSplit[1], out float value))
                {
                    switch (stringSplit[0])
                    {
                        case "heat":
                            o.GetComponent<Items>().heatTimer = value;
                            usedArg = true;
                            break;
                        case "condenced":
                            o.GetComponent<Items>().condensed = value;
                            usedArg = true;
                            break;
                    }
                }
                else
                {
                    Log("value " + stringSplit[1] + " is not a valid number");
                }

                if(!usedArg)
                    Log("parameter " + stringSplit[0] + " is not a parameter");
            } 
        }

        Log($"Spawned {item.itemName}");
    }

    private void CmdTeleport(string[] args)
    {
        if (args.Length < 2) return;

        Vector3 pos = ParsePosition(args[1]);
        playerController.transform.position = pos;

        Log($"Teleported to {pos}");
    }

    private void CmdTime(string[] args)
    {
        if (args.Length < 3) return;

        switch (args[1].ToLower())
        {
            case "set":
                if (float.TryParse(args[2], out float scale))
                {
                    Time.timeScale = scale;
                    Log($"Time scale set to {scale}");
                }
                break;

            case "pause":
                bool pause = args[2] == "1";
                Time.timeScale = pause ? 0f : 1f;
                Log(pause ? "Paused" : "Resumed");
                break;
        }
    }

    private void CmdPlayer(string[] args)
    {
        if (args.Length < 4) return;

        if (args[1].ToLower() != "set") return;

        string param = args[2].ToLower();

        if (!float.TryParse(args[3], out float value))
        {
            Log("Invalid value");
            return;
        }

        switch (param)
        {
            case "walkSpeed": playerController.WalkSpeed = value; break;
            case "sprintSpeed": playerController.SprintSpeed = value; break;
            case "pickupRange": CM.pickupRange = value; break;
            case "hp": playerController.health = value; break;
            case "stamina": playerController.stamina = value; break;
            default:
                Log("Unknown player parameter");
                return;
        }

        Log($"Set {param} to {value}");
    }

    private void CmdBloom(string[] args)
    {
        if (args.Length < 3) return;

        if (args[1].ToLower() == "addheat")
        {
            float val = float.Parse(args[2]);
            bloom.CheatAddCharcoal((int)val);
            Log($"Bloom heat +{val}");
        }
    }

    private void CmdSmeltery(string[] args)
    {
        if (args.Length < 3) return;

        if (args[1].ToLower() == "addheat")
        {
            float val = float.Parse(args[2]);
            smelt.AddCharcoal(val);
            Log($"Smeltery heat +{val}");
        }
    }
    private void CmdHelp(string[] args)
    {
        if (args.Length > 1)
        {
            string cmd = args[1].ToLower();

            if (commandDescriptions.TryGetValue(cmd, out string desc))
            {
                Log($"<color=yellow>{cmd}</color> - {desc}");
            }
            else
            {
                Log("<color=red>Command not found</color>");
            }

            return;
        }

        Log("<color=cyan>=== Commands ===</color>");

        foreach (var cmd in commandDescriptions)
        {
            Log($"<color=yellow>{cmd.Key}</color> - {cmd.Value}");
        }
    }

    // Parsing

    private List<string> ParseArguments(string input)
    {
        List<string> args = new List<string>();
        string current = "";
        bool inQuotes = false;

        foreach (char c in input)
        {
            if (c == '"')
            {
                inQuotes = !inQuotes;
                continue;
            }

            if (c == ' ' && !inQuotes)
            {
                if (!string.IsNullOrEmpty(current))
                {
                    args.Add(current);
                    current = "";
                }
            }
            else
            {
                current += c;
            }
        }

        if (!string.IsNullOrEmpty(current))
            args.Add(current);

        return args;
    }

    private Vector3 ParsePosition(string input)
    {
        if (input.ToLower() == "player")
        {
            return Camera.main.transform.position + Camera.main.transform.forward * 3f;
        }

        input = input.Replace("(", "").Replace(")", "");
        string[] split = input.Split(',');

        if (split.Length != 3) return Vector3.zero;

        if (float.TryParse(split[0], out float x) &&
            float.TryParse(split[1], out float y) &&
            float.TryParse(split[2], out float z))
        {
            return new Vector3(x, y, z);
        }

        return Vector3.zero;
    }

    // Autocomplete

    private void HandleAutocomplete()
    {
        string input = inputField.text.ToLower();

        suggestions = commandList.FindAll(c => c.StartsWith(input));

        if (suggestions.Count == 0) return;

        suggestionIndex++;
        if (suggestionIndex >= suggestions.Count)
            suggestionIndex = 0;

        inputField.text = suggestions[suggestionIndex];
        MoveCursorToEnd();

        Log($"<color=yellow>Suggestions: {string.Join(", ", suggestions)}</color>");
    }

    private void MoveCursorToEnd()
    {
        inputField.caretPosition = inputField.text.Length;
    }

    // Logging

    private void Log(string message)
    {
        logLines.Add($"> {message}");

        if (logLines.Count > maxLines)
            logLines.RemoveAt(0);

        text.text = string.Join("\n", logLines);

        Canvas.ForceUpdateCanvases();
        scrollRect.verticalNormalizedPosition = 0f;
    }


    public bool IsConsoleUp() { return consoleUp; }

}