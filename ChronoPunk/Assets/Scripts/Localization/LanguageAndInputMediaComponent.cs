using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class LanguageAndInputMediaComponent : MonoBehaviour, ILanguageAndInputMediaListener
{
    [Serializable]
    public class LanguageAndInputVariant
    {
        [SerializeField] private LanguageAndInputMediaManager.Language language;
        [SerializeField] private LanguageAndInputMediaManager.InputType inputType;

        [TextArea(1, 5)] public string textValue;
        public Sprite spriteValue;
        public GameObject customObjectValue;

        public LanguageAndInputMediaManager.Language Language => language;
        public LanguageAndInputMediaManager.InputType InputType => inputType;

        public LanguageAndInputVariant(
            LanguageAndInputMediaManager.Language language,
            LanguageAndInputMediaManager.InputType inputType)
        {
            this.language = language;
            this.inputType = inputType;
            textValue = string.Empty;
            spriteValue = null;
            customObjectValue = null;
        }

        public void SetKey(
            LanguageAndInputMediaManager.Language newLanguage,
            LanguageAndInputMediaManager.InputType newInputType)
        {
            language = newLanguage;
            inputType = newInputType;
        }
    }

    [Serializable]
    public class LanguageAndInputObject
    {
        public enum ObjectType
        {
            Text = 0,
            Image = 1,
            CustomObjects = 2
        }

        [Header("General")]
        [Tooltip("Nombre opciondal para identificar este bloque en el inspector.")]
        public string debugName;
        public ObjectType objectType = ObjectType.Text;

        [Header("Text")]
        public TMP_Text textTarget;

        [Header("Image (UI and/or SpriteRenderer)")]
        public Image uiImageTarget;
        public SpriteRenderer spriteRendererTarget;

        [Header("Variants")]
        public List<LanguageAndInputVariant> variants = new List<LanguageAndInputVariant>();

        public void EnsureAllCombinations()
        {
            Array languages = Enum.GetValues(typeof(LanguageAndInputMediaManager.Language));
            Array inputTypes = Enum.GetValues(typeof(LanguageAndInputMediaManager.InputType));

            foreach (LanguageAndInputMediaManager.Language language in languages)
            {
                foreach (LanguageAndInputMediaManager.InputType inputType in inputTypes)
                {
                    EnsureVariant(language, inputType);
                }
            }

            variants.RemoveAll(v => !IsValidCombination(v.Language, v.InputType));
            variants.Sort(CompareVariantOrder);
        }

        private void EnsureVariant(
            LanguageAndInputMediaManager.Language language,
            LanguageAndInputMediaManager.InputType inputType)
        {
            LanguageAndInputVariant existing = variants.Find(
                v => v.Language == language && v.InputType == inputType);

            if (existing != null)
            {
                existing.SetKey(language, inputType);
                return;
            }

            variants.Add(new LanguageAndInputVariant(language, inputType));
        }

        private static bool IsValidCombination(
            LanguageAndInputMediaManager.Language language,
            LanguageAndInputMediaManager.InputType inputType)
        {
            bool languageDefined = Enum.IsDefined(typeof(LanguageAndInputMediaManager.Language), language);
            bool inputDefined = Enum.IsDefined(typeof(LanguageAndInputMediaManager.InputType), inputType);
            return languageDefined && inputDefined;
        }

        private static int CompareVariantOrder(LanguageAndInputVariant a, LanguageAndInputVariant b)
        {
            int languageCompare = a.Language.CompareTo(b.Language);
            if (languageCompare != 0)
            {
                return languageCompare;
            }

            return a.InputType.CompareTo(b.InputType);
        }

        public void Apply(LanguageAndInputMediaManager.MediaConfig config)
        {
            LanguageAndInputVariant selected = variants.Find(
                v => v.Language == config.language && v.InputType == config.inputType);

            if (selected == null)
            {
                return;
            }

            switch (objectType)
            {
                case ObjectType.Text:
                    if (textTarget != null)
                    {
                        textTarget.text = selected.textValue;
                    }
                    break;

                case ObjectType.Image:
                    if (uiImageTarget != null)
                    {
                        uiImageTarget.sprite = selected.spriteValue;
                    }

                    if (spriteRendererTarget != null)
                    {
                        spriteRendererTarget.sprite = selected.spriteValue;
                    }
                    break;

                case ObjectType.CustomObjects:
                    ApplyCustomObjects(selected.customObjectValue);
                    break;
            }
        }

        private void ApplyCustomObjects(GameObject selectedObject)
        {
            HashSet<GameObject> allObjects = new HashSet<GameObject>();

            for (int i = 0; i < variants.Count; i++)
            {
                GameObject option = variants[i].customObjectValue;
                if (option != null)
                {
                    allObjects.Add(option);
                }
            }

            foreach (GameObject option in allObjects)
            {
                option.SetActive(option == selectedObject);
            }
        }
    }

    [SerializeField] private List<LanguageAndInputObject> objects = new List<LanguageAndInputObject>();

    private void OnEnable()
    {
        LanguageAndInputMediaManager manager = LanguageAndInputMediaManager.Instance;
        if (manager != null)
        {
            manager.RegisterListener(this);
            ApplyConfig(manager.CurrentConfig);
        }
    }

    private void Start()
    {
        LanguageAndInputMediaManager manager = LanguageAndInputMediaManager.Instance;
        if (manager != null)
        {
            ApplyConfig(manager.CurrentConfig);
        }
    }

    private void OnDisable()
    {
        LanguageAndInputMediaManager manager = LanguageAndInputMediaManager.Instance;
        if (manager != null)
        {
            manager.UnregisterListener(this);
        }
    }

    private void OnValidate()
    {
        SyncAllObjectsWithEnums();
    }

    public void SyncAllObjectsWithEnums()
    {
        for (int i = 0; i < objects.Count; i++)
        {
            if (objects[i] != null)
            {
                objects[i].EnsureAllCombinations();
            }
        }
    }

    public void OnLanguageAndInputConfigChanged(LanguageAndInputMediaManager.MediaConfig newConfig)
    {
        ApplyConfig(newConfig);
    }

    private void ApplyConfig(LanguageAndInputMediaManager.MediaConfig config)
    {
        for (int i = 0; i < objects.Count; i++)
        {
            LanguageAndInputObject targetObject = objects[i];
            if (targetObject != null)
            {
                targetObject.Apply(config);
            }
        }
    }
}
