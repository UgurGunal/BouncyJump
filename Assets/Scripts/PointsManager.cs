using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro; // Import TextMeshPro namespace

public class PointsManager : MonoBehaviour
{
    public TMP_Text angularVelocityText; // Use TMP_Text instead of Text
    public TMP_Text heightText;
    public Transform character; // Reference to the character object

    private Rigidbody2D rb;

    void Start()
    {
        if (character != null)
        {
            rb = character.GetComponent<Rigidbody2D>();
        }

        UpdateUI();
    }

    void Update()
    {
        if (rb != null)
        {
            UpdateUI();
        }
    }

    void UpdateUI()
    {
        int angularVelocity = Mathf.RoundToInt(rb.angularVelocity); // Get angular velocity
        int height = Mathf.Max(Mathf.RoundToInt(character.position.y),0); // Get height (Y position)

        angularVelocityText.text = angularVelocity.ToString();
        heightText.text = height.ToString();
    }
}
