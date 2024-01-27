using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using PanicBuying;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ConfigurationPanel : MonoBehaviour
{
    [SerializeField] private List<Resolution> resolutions = new List<Resolution>();
    [SerializeField, Range(0, 100)] private int volume;
    [SerializeField] private TMP_Dropdown resolutionsDropDown;
    [SerializeField] private int resolutionDropDownIndex;
    [SerializeField] private Toggle fullscreenModeToggle;


    public void InitUI()
    {
        resolutions.Clear();
        resolutionsDropDown.options.Clear();

        var res60hz = from res in Screen.resolutions
                      where (int)res.refreshRateRatio.value == 60
                      select res;

        resolutions.AddRange(res60hz.ToArray());

       

        int index = 0;
        foreach (var res in resolutions)
        {
            TMP_Dropdown.OptionData data = new TMP_Dropdown.OptionData();
            data.text = res.width + " x " + res.height;
            resolutionsDropDown.options.Add(data);
            if (res.width == Screen.width && res.height == Screen.height)
            {
                resolutionsDropDown.value = index;
            }

            index++;
        }
        resolutionsDropDown.RefreshShownValue();
    }

    public void OnResolutionDropdownChange(int x)
    {
        resolutionDropDownIndex = x;
    }

    public void OnApplyButtonClick()
    {
        ApplyAll();
        InitUI();
    }

    public void OnConfirmButtonClick()
    {
        ApplyAll();
        InitUI();
        Close();
    }

    public void ApplyAll()
    {
        var res = resolutions[resolutionDropDownIndex];
        Screen.SetResolution(res.width, res.height, fullscreenModeToggle.isOn);
    }

    public void Close()
    {
        gameObject.SetActive(false);
    }
}

