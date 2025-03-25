using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class SliderController : MonoBehaviour
{
    [SerializeField] private TMP_Text NumDisk = null;
    [SerializeField] private Slider slider = null;

    // This method will be called whenever the slider value changes
    public void SliderChange(float value)
    {
        // Just display the raw slider value, rounded to nearest integer
        NumDisk.text = Mathf.Round(value).ToString("0");
        // Optionally, print debug information
        Debug.Log("Slider changed to: " + value + ", displayed as: " + NumDisk.text);
    }

    // Call this from Start() if you want to initialize the text
    private void Start()
    {
        // Get the initial slider value and update text
        if (slider == null)
            slider = GetComponent<Slider>();

        if (slider != null)
        {
            SliderChange(slider.value);
        }
    }

    // New method to disable the slider
    public void DisableSlider()
    {
        if (slider != null)
        {
            // Disable interactivity
            slider.interactable = false;

        }
    }
}