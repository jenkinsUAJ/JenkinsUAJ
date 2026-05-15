using System.Collections;
using System.Threading;
using TMPro;
using Unity.VisualScripting;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

public class TextSpawn : MonoBehaviour
{
    [Header("Ajustes escribir texto")]
    public TMP_Text textTMPRO;
    public string text;
    public float time;
    [Header("Ajustes efecto glitch")]
    public float timeToGlitch;
    public float glitchRadius;
    public float glitchVelocity;
    public int minGlitchAmmount;
    public int maxGlitchAmmount;
    public Color color1;
    public Color color2;
    public Color color3;

    private Vector2 startPos;

    private int colorIt = 0;
    private bool canGlitch = true;
    private bool canWrite = true;

    private float timer = 0;

    public bool textoTalCual = false;
    IEnumerator appearText(float totalTime)
    {
        int index = 1;
        float time = 0;
        float timeToType = 0;
        while (time < totalTime)
        {
            if (time >= timeToType)
            {
                textTMPRO.text = text.Substring(0, index++);
                if (!textoTalCual)
                    textTMPRO.text = textTMPRO.text.Replace('#', '\n');

                timeToType = time + (totalTime / text.Length); 
            }
            
            time += Time.deltaTime;

            yield return null;
        }

        textTMPRO.text = text.Replace('#', '\n');
    }

    IEnumerator Glitch()
    {
        canGlitch = false;

        int numTimes = Random.Range(minGlitchAmmount, maxGlitchAmmount);
        for (int i = 0; i < numTimes; i++)
        {
            Vector2 randomDir = new Vector2(Random.Range(-1.0f, 1.0f), Random.Range(-1.0f, 1.0f));

            float newX = startPos.x + glitchRadius * Mathf.Cos(randomDir.x);
            float newY = startPos.y + glitchRadius * Mathf.Sin(randomDir.y);

            textTMPRO.rectTransform.position = new Vector2(newX, newY);

            textTMPRO.color = colorIt == 0 ? color1 : colorIt == 1 ? color2 : color3;

            colorIt = (colorIt + 1) % 3;

            yield return new WaitForSeconds(glitchVelocity);
        }

        timer = timeToGlitch;
        canGlitch = true;
    }

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        startPos = textTMPRO.rectTransform.position;
    }

    // Update is called once per frame
    void Update()
    {
        timer -= Time.deltaTime;
        if (timer < 0 && canGlitch)
            StartCoroutine(Glitch());
    }



    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (LayerMask.LayerToName(collision.gameObject.layer) == "Player" && canWrite)
        {
            canWrite = false;
            StartCoroutine(appearText(time));
        }
    }
}
