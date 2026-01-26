using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Helper component to apply CSS-like styles to UI elements
/// Attach this to any UI GameObject to apply styles
/// </summary>
public class UIStyleHelper : MonoBehaviour
{
    [Header("Style References")]
    [Tooltip("Reference to the UIStyleManager ScriptableObject")]
    public UIStyleManager styleManager;
    
    [Header("Text Style")]
    [Tooltip("Apply a text style class")]
    public bool applyTextStyle = false;
    public enum TextStyleClass { Heading, Body, Caption, Custom }
    public TextStyleClass textStyleClass = TextStyleClass.Body;
    
    [Header("Button Style")]
    [Tooltip("Apply a button style class")]
    public bool applyButtonStyle = false;
    public enum ButtonStyleClass { Primary, Secondary, Success, Danger, Custom }
    public ButtonStyleClass buttonStyleClass = ButtonStyleClass.Primary;
    
    [Header("Panel Style")]
    [Tooltip("Apply a panel style class")]
    public bool applyPanelStyle = false;
    public enum PanelStyleClass { Default, Card, Custom }
    public PanelStyleClass panelStyleClass = PanelStyleClass.Default;
    
    [Header("Custom Styles (if Custom is selected)")]
    public UIStyleManager.TextStyle customTextStyle;
    public UIStyleManager.ButtonStyle customButtonStyle;
    public UIStyleManager.PanelStyle customPanelStyle;
    
    void Start()
    {
        ApplyStyles();
    }
    
    /// <summary>
    /// Apply all selected styles to this GameObject
    /// </summary>
    public void ApplyStyles()
    {
        if (styleManager == null)
        {
            Debug.LogWarning($"UIStyleHelper on {gameObject.name}: StyleManager is not assigned!");
            return;
        }
        
        // Apply text style
        if (applyTextStyle)
        {
            var text = GetComponent<TextMeshProUGUI>();
            if (text != null)
            {
                UIStyleManager.TextStyle style = GetTextStyle();
                if (style != null)
                    styleManager.ApplyTextStyle(text, style);
            }
        }
        
        // Apply button style
        if (applyButtonStyle)
        {
            var button = GetComponent<Button>();
            if (button != null)
            {
                UIStyleManager.ButtonStyle style = GetButtonStyle();
                if (style != null)
                    styleManager.ApplyButtonStyle(button, style);
            }
        }
        
        // Apply panel style
        if (applyPanelStyle)
        {
            var image = GetComponent<Image>();
            if (image != null)
            {
                UIStyleManager.PanelStyle style = GetPanelStyle();
                if (style != null)
                    styleManager.ApplyPanelStyle(image, style);
            }
        }
    }
    
    UIStyleManager.TextStyle GetTextStyle()
    {
        switch (textStyleClass)
        {
            case TextStyleClass.Heading:
                return styleManager.headingStyle;
            case TextStyleClass.Body:
                return styleManager.bodyStyle;
            case TextStyleClass.Caption:
                return styleManager.captionStyle;
            case TextStyleClass.Custom:
                return customTextStyle;
            default:
                return styleManager.bodyStyle;
        }
    }
    
    UIStyleManager.ButtonStyle GetButtonStyle()
    {
        switch (buttonStyleClass)
        {
            case ButtonStyleClass.Primary:
                return styleManager.primaryButton;
            case ButtonStyleClass.Secondary:
                return styleManager.secondaryButton;
            case ButtonStyleClass.Success:
                return styleManager.successButton;
            case ButtonStyleClass.Danger:
                return styleManager.dangerButton;
            case ButtonStyleClass.Custom:
                return customButtonStyle;
            default:
                return styleManager.primaryButton;
        }
    }
    
    UIStyleManager.PanelStyle GetPanelStyle()
    {
        switch (panelStyleClass)
        {
            case PanelStyleClass.Default:
                return styleManager.defaultPanel;
            case PanelStyleClass.Card:
                return styleManager.cardPanel;
            case PanelStyleClass.Custom:
                return customPanelStyle;
            default:
                return styleManager.defaultPanel;
        }
    }
    
    /// <summary>
    /// Apply styles at runtime (useful for dynamic UI)
    /// </summary>
    [ContextMenu("Apply Styles Now")]
    public void ApplyStylesNow()
    {
        ApplyStyles();
    }
}
