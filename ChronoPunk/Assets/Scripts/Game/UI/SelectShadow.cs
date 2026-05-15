using System.Collections.Generic;
using GameFlow;
using CameraSystem;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;
using UnityEngine.InputSystem.Layouts;


public class SelectShadow : PausableMonoBehaviour
{

    public GameObject iconContainer;

    public ShadowsColor shadowsColorScriptableObject;


    public Sprite _iconSprite;
    public Sprite _blokedSprite;

    public int _maxRecords;


    public int _currentSelected;

    public GameObject textoEligeTuRama;

    public bool optionsMenuOpen;

    [Header("Shadow Menu Camera")]
    [SerializeField, Min(0f)] private float shadowMenuExitBlendSeconds = 0.4f;
    [SerializeField] private CameraBlendStyle shadowMenuExitBlendStyle = CameraBlendStyle.EaseInOut;

    private bool waitingNavigationRelease = true;
    private bool isNavigationHeld = false;

    void Start()
    {
        if (CameraSystemManager.Instance != null)
        {
            CameraSystemManager.Instance.ActivateShadowMenuCamera(-1, CameraBlendStyle.EaseInOut);
        }

        // Pausamos el juego para lanzar el men�.
        PauseManager.Instance.SetPause(true);

        //obtener el numero maximo de sobras en este nivel
        _maxRecords = RecordingSlotManager.Instance.MaxSlots;

        _currentSelected = GetDefaultSlotSelection();

        SetupSlots();
    }

    private void SetupSlots()
    {
        int globalSlots = iconContainer.transform.childCount;

        //activar y desactivar segun los datos
        for (int i = 0; i < globalSlots; i++)
        {
            //si la sombra esta disponible, elegir de color pintar el icono
            if (i < _maxRecords)
            {

                //elegir que color poner segun si se ha grabado o no

                //si esta usado, se pone el color de la sombra correspondiente
                if (RecordingSlotManager.Instance.IsSlotUsed(i))
                {
                    iconContainer.transform.GetChild(i).GetComponent<Image>().color = shadowsColorScriptableObject.GetColor(i);
                }
                else // si no esta usado, se pone el color default (blanco)
                {

                    iconContainer.transform.GetChild(i).GetComponent<Image>().color = Color.white;
                }

                // Mostrar el icono de "Delete"
                iconContainer.transform.GetChild(i).transform.GetChild(4).gameObject.SetActive(true);
            }
            else //si la sobra no esta disponible, poner sprite bloqueado
            {
                iconContainer.transform.GetChild(i).GetComponent<Image>().sprite = _blokedSprite;

                // Esconder el icono de "Delete"
                iconContainer.transform.GetChild(i).transform.GetChild(4).gameObject.SetActive(false);
            }
        }
    }

    private int GetDefaultSlotSelection()
    {
        for (int i = 0; i < _maxRecords; i++)
        {
            if (!RecordingSlotManager.Instance.IsSlotUsed(i))
            {
                return i;
            }
        }

        int lastRecordedSlot = RecordingSlotManager.Instance.LastRecordedSlot;
        if (lastRecordedSlot >= 0 && lastRecordedSlot < _maxRecords && RecordingSlotManager.Instance.IsSlotUsed(lastRecordedSlot))
        {
            return lastRecordedSlot;
        }

        for (int i = _maxRecords - 1; i >= 0; i--)
        {
            if (RecordingSlotManager.Instance.IsSlotUsed(i))
            {
                return i;
            }
        }

        return 0;
    }

    void Update()
    {
        if (waitingNavigationRelease && !isNavigationHeld)
        {
            waitingNavigationRelease = false;
        }

        //desactivar todas las flechitas de icono seleccionado
        for (int i = 0; i < iconContainer.transform.childCount; i++)
        {
            iconContainer.transform.GetChild(i).transform.GetChild(0).gameObject.SetActive(false);
        }

        //activar solo la flecha del icono seleccionado actual
        iconContainer.transform.GetChild(_currentSelected).transform.GetChild(0).gameObject.SetActive(true);
    }

    // Esta función actualiza el índice seleccionado usando eventos del Input System
    // para teclado y mando.
    public void OnChange(InputAction.CallbackContext context)
    {
        if (!gameObject.activeSelf) return;
        if (optionsMenuOpen) return;

        bool wasNavigationHeld = isNavigationHeld;

        if (context.canceled)
        {
            isNavigationHeld = false;
            return;
        }

        if (context.started || context.performed)
        {
            if (context.control is KeyControl keyControl)
            {
                isNavigationHeld =
                    keyControl.keyCode == Key.A ||
                    keyControl.keyCode == Key.D ||
                    keyControl.keyCode == Key.LeftArrow ||
                    keyControl.keyCode == Key.RightArrow;
            }
            else
            {
                Vector2 moveState = context.ReadValue<Vector2>();
                isNavigationHeld = Mathf.Abs(moveState.x) >= 0.5f;
            }
        }

        if (waitingNavigationRelease) return;

        if (context.started)
        {
            if (context.control is KeyControl keyControl)
            {
                if (keyControl.keyCode == Key.A || keyControl.keyCode == Key.LeftArrow)
                {
                    iconContainer.transform.GetChild(_currentSelected).transform.GetChild(4).GetComponent<LoadingBarController>().SetPressed(false);
                    _currentSelected = (_currentSelected + _maxRecords - 1) % _maxRecords;
                }
                else if (keyControl.keyCode == Key.D || keyControl.keyCode == Key.RightArrow)
                {
                    iconContainer.transform.GetChild(_currentSelected).transform.GetChild(4).GetComponent<LoadingBarController>().SetPressed(false);
                    _currentSelected = (_currentSelected + 1) % _maxRecords;
                }
            }
        }

        if ((context.started || context.performed) && context.control.device is Gamepad)
        {
            Vector2 moveInput = context.ReadValue<Vector2>();
            bool movedIntoNavigationZone = !wasNavigationHeld && Mathf.Abs(moveInput.x) >= 0.5f;

            if (movedIntoNavigationZone)
            {
                iconContainer.transform.GetChild(_currentSelected).transform.GetChild(4).GetComponent<LoadingBarController>().SetPressed(false);

                if (moveInput.x < 0f)
                {
                    _currentSelected = (_currentSelected + _maxRecords - 1) % _maxRecords;
                }
                else if (moveInput.x > 0f)
                {
                    _currentSelected = (_currentSelected + 1) % _maxRecords;
                }
            }
        }
    }

    public void OnSelect(InputAction.CallbackContext context)
    {
        if (!gameObject.activeSelf) return;
        if(optionsMenuOpen) return;

        if (context.started)
        {
            //desactivar el objeto
            gameObject.SetActive(false);


            //iniciar la grabacion en el indice adecuado
            RecordingSlotManager.Instance.SelectAndStartRecording(_currentSelected);

            //iniciar el replay del resto de sombras
            ReplayManager.Instance.StartFullReplay();

            //desactivar texto
            textoEligeTuRama.SetActive(false);

            // Reanudamos el juego.
            PauseManager.Instance.SetPause(false);

            if (CameraSystemManager.Instance != null)
            {
                CameraSystemManager.Instance.DeactivateShadowMenuCamera(shadowMenuExitBlendSeconds, shadowMenuExitBlendStyle);
            }
        }
    }

    // Captura del input de pulsar o soltar el boton de borrar sombra
    public void OnDeleteShadowInput(InputAction.CallbackContext context)
    {
        if (context.started && RecordingSlotManager.Instance.IsSlotUsed(_currentSelected))
            iconContainer.transform.GetChild(_currentSelected).transform.GetChild(4).GetComponent<LoadingBarController>().SetPressed(true);
        else if(context.canceled)
            iconContainer.transform.GetChild(_currentSelected).transform.GetChild(4).GetComponent<LoadingBarController>().SetPressed(false);
    }

    // Borra la sombra grabada
    public void DeleteCurrentShadow()
    {
        RecordingSlotManager.Instance.DeleteSlotRecording(_currentSelected);
        SetupSlots();
    }
}