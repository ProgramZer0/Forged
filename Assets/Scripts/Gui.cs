using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;

public enum CursorState
{
    defaultCursor,
    handCursor,
    blankCursor,
    closedHandCursor
}

public class Gui : MonoBehaviour
{
    [Header("Cursor Settings")]
    [SerializeField] private Texture2D defaultCursor;
    [SerializeField] private Texture2D handCursor;
    [SerializeField] private Texture2D closedHandCursor;
    [SerializeField] private Texture2D BLANK;
    [SerializeField] private Vector2 defaultHotspot = Vector2.zero;
    [SerializeField] private Vector2 handHotspot = Vector2.zero;

    [Header("Pause Menu")]
    [SerializeField] private GameObject pauseMenu;
    [SerializeField] private Button exitPauseButton;
    [SerializeField] private Button saveGameButton;
    [SerializeField] private Button loadPauseButton;
    [SerializeField] private Button backToMainMenuButton;

    [Header("Main Menu")]
    [SerializeField] private GameObject mainMenu;
    [SerializeField] private Button loadGameButton;
    [SerializeField] private Button newGameButton;
    [SerializeField] private Button exitGameButton;

    [Header("Cameras")]
    [SerializeField] private Camera menuCam;
    [SerializeField] private GameObject cinemaCam;

    [Header("HUD")]
    [SerializeField] private GameObject hpAndStatsUI;
    [SerializeField] private Text infoStatText;

    [SerializeField] private Controls controls;
    [SerializeField] private WorkstationScript WS;
    private bool menuIsUp;
    public CursorState currentState = CursorState.defaultCursor;

    private void Start()
    {
        pauseMenu.SetActive(false);
        mainMenu.SetActive(true);
        hpAndStatsUI.SetActive(false);

        controls.GetMainCamera().enabled = false;
        menuCam.enabled = true;
        cinemaCam.SetActive(false);

        Cursor.lockState = CursorLockMode.None;

        exitPauseButton.onClick.AddListener(ClosePauseMenu);
        saveGameButton.onClick.AddListener(SaveState);
        loadPauseButton.onClick.AddListener(Load);
        backToMainMenuButton.onClick.AddListener(GoToMainMenu);

        newGameButton.onClick.AddListener(StartNewGame);
        loadGameButton.onClick.AddListener(Load);
        exitGameButton.onClick.AddListener(() => Application.Quit());
    }

    private void FixedUpdate()
    {
        UpdateHUD();
        HandlePauseMenuEsc();
        HandleOpenPauseMenu();
    }

    private void UpdateHUD()
    {
        bool inGame = !controls.MovementLocked;
        hpAndStatsUI.SetActive(inGame);
        infoStatText.gameObject.SetActive(inGame);
    }

    private void HandlePauseMenuEsc()
    {
        // Close pause menu with Escape if it's open
        if (menuIsUp && controls.EscWasPressed())
        {
            ClosePauseMenu();
            controls.ConsumeEscPress();
        }
    }

    private void HandleOpenPauseMenu()
    {
        // Open pause only if movement is free, esc was pressed, and pause isn't already open
        if (!controls.EscWasPressed() || controls.MovementLocked || menuIsUp) return;

        OpenPauseMenu();
        controls.ConsumeEscPress();
    }

    private void OpenPauseMenu()
    {
        controls.SetMovementLocked(true);
        pauseMenu.SetActive(true);
        cinemaCam.SetActive(false);
        Cursor.lockState = CursorLockMode.None;
        menuIsUp = true;
    }

    public void ClosePauseMenu()
    {
        controls.SetMovementLocked(false);
        pauseMenu.SetActive(false);
        cinemaCam.SetActive(true);
        Cursor.lockState = CursorLockMode.Locked;
        menuIsUp = false;
        controls.ConsumeEscPress();
    }

    private void StartNewGame()
    {
        mainMenu.SetActive(false);
        controls.GetMainCamera().enabled = true;
        menuCam.enabled = false;
        cinemaCam.SetActive(true);
        Cursor.lockState = CursorLockMode.Locked;
        controls.SetMovementLocked(false);
    }

    private void GoToMainMenu()
    {
        controls.GetMainCamera().enabled = false;
        menuCam.enabled = true;
        pauseMenu.SetActive(false);
        mainMenu.SetActive(true);
        cinemaCam.SetActive(false);
        Cursor.lockState = CursorLockMode.None;
        controls.SetMovementLocked(true);
    }

    private void Load()
    {
        // WIP
    }

    private void SaveState()
    {
        // WIP
    }
    public void SetDefaultCursor()
    {
        currentState = CursorState.defaultCursor;
        Cursor.SetCursor(defaultCursor, defaultHotspot, CursorMode.Auto);
    }
    public void SetDefaultCursor(Vector2 pos)
    {
        currentState = CursorState.defaultCursor;
        Cursor.SetCursor(defaultCursor, pos, CursorMode.Auto);
    }

    public void SetHandCursor()
    {
        currentState = CursorState.handCursor;
        Cursor.SetCursor(handCursor, handHotspot, CursorMode.Auto);
    }
    public void SetHandCursor(Vector2 pos)
    {
        currentState = CursorState.handCursor;
        Cursor.SetCursor(handCursor, pos, CursorMode.Auto);
    }

    public void SetBankCursor()
    {
        currentState = CursorState.blankCursor;
        Cursor.SetCursor(BLANK, handHotspot, CursorMode.Auto);
    }
    public void SetBankCursor(Vector2 pos)
    {
        currentState = CursorState.blankCursor;
        Cursor.SetCursor(BLANK, pos, CursorMode.Auto);
    }

    public void SetClosedHCursor()
    {
        currentState = CursorState.closedHandCursor;
        Cursor.SetCursor(closedHandCursor, handHotspot, CursorMode.Auto);
    }
    public void SetClosedHCursor(Vector2 pos)
    {
        currentState = CursorState.closedHandCursor;
        Cursor.SetCursor(closedHandCursor, pos, CursorMode.Auto);
    }
}