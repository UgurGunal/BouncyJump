using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro; // Import TextMeshPro namespace

public class PointsManager : MonoBehaviour
{
    public static PointsManager Instance { get; private set; }

    public TMP_Text angularVelocityText;
    public TMP_Text heightText;
    public Transform character;

    private Rigidbody2D rb;

    void Awake()
    {
        if (Instance == null)
            Instance = this;
        else
            Destroy(gameObject); // Optional safeguard
    }

    void Start()
    {
        if (character != null)
        {
            rb = character.GetComponent<Rigidbody2D>();
        }

        UpdateUI();
    }

    public void UpdateUI()
    {
        int angularVelocity = Mathf.RoundToInt(rb.angularVelocity);
        int height = Mathf.Max(Mathf.RoundToInt(character.position.y), 0);

        angularVelocityText.SetText("{0}", angularVelocity);
        heightText.SetText("{0}", height);
    }

}
