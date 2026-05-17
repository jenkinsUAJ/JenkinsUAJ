using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;


/**
 * En este fichero se encuentran varias clases para la serializacion de los eventos 
 * - Serializacion de una partida completa (array de slots)
 * - Serializacion de un slot (array de inputs)
 * - Serializacion de un input (conversion entre input del juego y la clase auxiliar para la serializacion)
 * 
 * Las clases ofrecen metodos para convertir entre los datos serializados y los originales
 */


//serializable para poder guardarlo en fichero de forma sencilla
[Serializable]
/// <summary>
/// Formato raíz del fichero JSON de grabación.
/// Agrupa los slots grabados y se serializa desde TestRecordManager para luego leerlo en tests.
/// </summary>
public class TestRecordingFileData
{
    public string sceneName;
    public string createdAtUtc;
    public List<TestRecordingSlotData> slots = new List<TestRecordingSlotData>();

    public Dictionary<int, List<RecordedInput>> ToRecordedInputsBySlot()
    {
        Dictionary<int, List<RecordedInput>> result = new Dictionary<int, List<RecordedInput>>();

        if (slots == null)
        {
            return result;
        }

        for (int i = 0; i < slots.Count; i++)
        {
            TestRecordingSlotData slotData = slots[i];
            if (slotData == null)
            {
                continue;
            }

            result[slotData.slotId] = slotData.ToRecordedInputs();
        }

        return result;
    }
}

//serializable para poder guardarlo en fichero de forma sencilla
[Serializable]
/// <summary>
/// Datos serializables de un slot concreto dentro de una grabación.
/// Se usa como capa intermedia entre RecordManager y el fichero final de test.
/// </summary>
public class TestRecordingSlotData
{
    public int slotId;
    public List<TestRecordedInputData> inputs = new List<TestRecordedInputData>();

    public List<RecordedInput> ToRecordedInputs()
    {
        List<RecordedInput> result = new List<RecordedInput>();

        if (inputs == null)
        {
            return result;
        }

        for (int i = 0; i < inputs.Count; i++)
        {
            TestRecordedInputData inputData = inputs[i];
            if (inputData == null)
            {
                continue;
            }

            RecordedInput recordedInput = inputData.ToRecordedInput();
            if (recordedInput != null)
            {
                result.Add(recordedInput);
            }
        }

        return result;
    }
}



//serializable para poder guardarlo en fichero de forma sencilla
[Serializable]
/// <summary>
/// Representación serializable de un input grabado.
/// Convierte entre RecordedInput en runtime y el JSON guardado para reproducción posterior.
/// </summary>
public class TestRecordedInputData
{
    public InputActionType actionType;
    public int fixedFrameStamp;
    public Vector2 direction;
    public bool isPressed;

    public static TestRecordedInputData FromRecordedInput(RecordedInput input)
    {
        if (input == null)
        {
            return null;
        }

        TestRecordedInputData data = new TestRecordedInputData
        {
            actionType = input.actionType,
            fixedFrameStamp = input.fixedFrameStamp
        };

        if (input is MoveInput moveInput)
        {
            data.actionType = InputActionType.Move;
            data.direction = moveInput.direction;
        }
        else if (input is JumpInput jumpInput)
        {
            data.actionType = InputActionType.Jump;
            data.isPressed = jumpInput.isPressed;
        }
        else if (input is ShootInput)
        {
            data.actionType = InputActionType.Shoot;
        }
        else if (input is StopRecordingInput)
        {
            data.actionType = InputActionType.StopRecording;
        }

        return data;
    }

    public RecordedInput ToRecordedInput()
    {
        switch (actionType)
        {
            case InputActionType.Move:
                return new MoveInput
                {
                    actionType = InputActionType.Move,
                    fixedFrameStamp = fixedFrameStamp,
                    direction = direction
                };
            case InputActionType.Jump:
                return new JumpInput
                {
                    actionType = InputActionType.Jump,
                    fixedFrameStamp = fixedFrameStamp,
                    isPressed = isPressed
                };
            case InputActionType.Shoot:
                return new ShootInput
                {
                    actionType = InputActionType.Shoot,
                    fixedFrameStamp = fixedFrameStamp
                };
            case InputActionType.StopRecording:
                return new StopRecordingInput
                {
                    actionType = InputActionType.StopRecording,
                    fixedFrameStamp = fixedFrameStamp
                };
            default:
                return null;
        }
    }
}