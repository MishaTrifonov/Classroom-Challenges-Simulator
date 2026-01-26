using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

/// <summary>
/// CSS-like styling system for Unity UI elements
/// Provides centralized styling similar to CSS classes
/// </summary>
[CreateAssetMenu(fileName = "UIStyle", menuName = "UI/Style Manager")]
public class UIStyleManager : ScriptableObject
{
    [System.Serializable]
    public class TextStyle
    {
        [Header("Font Settings")]
        public TMP_FontAsset fontAsset;
        public int fontSize = 14;
        public FontStyles fontStyle = FontStyles.Normal;
        public Color color = Color.white;
        public TextAlignmentOptions alignment = TextAlignmentOptions.Left;
        
        [Header("Spacing")]
        public float characterSpacing = 0f;
        public float wordSpacing = 0f;
        public float lineSpacing = 0f;
        
        [Header("Effects")]
        public bool enableOutline = false;
        public Color outlineColor = Color.black;
        public float outlineWidth = 0.2f;
        
        public bool enableShadow = false;
        public Color shadowColor = new Color(0, 0, 0, 0.5f);
        public Vector2 shadowOffset = new Vector2(1, -1);
    }
    
    [System.Serializable]
    public class ButtonStyle
    {
        [Header("Colors")]
        public Color normalColor = Color.white;
        public Color highlightedColor = new Color(0.9f, 0.9f, 0.9f);
        public Color pressedColor = new Color(0.8f, 0.8f, 0.8f);
        public Color disabledColor = new Color(0.5f, 0.5f, 0.5f);
        
        [Header("Sprites")]
        public Sprite normalSprite;
        public Sprite highlightedSprite;
        public Sprite pressedSprite;
        public Sprite disabledSprite;
        
        [Header("Text Style")]
        public TextStyle textStyle = new TextStyle();
    }
    
    [System.Serializable]
    public class PanelStyle
    {
        [Header("Background")]
        public Color backgroundColor = new Color(1, 1, 1, 0.95f);
        public Sprite backgroundSprite;
        
        [Header("Border")]
        public bool showBorder = false;
        public Color borderColor = Color.black;
        public float borderWidth = 2f;
        
        [Header("Padding")]
        public RectOffset padding = new RectOffset(10, 10, 10, 10);
    }
    
    [Header("Text Styles")]
    public TextStyle headingStyle = new TextStyle
    {
        fontSize = 24,
        fontStyle = FontStyles.Bold,
        color = Color.white
    };
    
    public TextStyle bodyStyle = new TextStyle
    {
        fontSize = 14,
        color = Color.white
    };
    
    public TextStyle captionStyle = new TextStyle
    {
        fontSize = 12,
        color = new Color(0.8f, 0.8f, 0.8f)
    };
    
    [Header("Button Styles")]
    public ButtonStyle primaryButton = new ButtonStyle
    {
        normalColor = new Color(0.2f, 0.6f, 1f),
        highlightedColor = new Color(0.3f, 0.7f, 1f),
        pressedColor = new Color(0.1f, 0.5f, 0.9f)
    };
    
    public ButtonStyle secondaryButton = new ButtonStyle
    {
        normalColor = new Color(0.5f, 0.5f, 0.5f),
        highlightedColor = new Color(0.6f, 0.6f, 0.6f),
        pressedColor = new Color(0.4f, 0.4f, 0.4f)
    };
    
    public ButtonStyle successButton = new ButtonStyle
    {
        normalColor = new Color(0.2f, 0.8f, 0.2f),
        highlightedColor = new Color(0.3f, 0.9f, 0.3f),
        pressedColor = new Color(0.1f, 0.7f, 0.1f)
    };
    
    public ButtonStyle dangerButton = new ButtonStyle
    {
        normalColor = new Color(0.9f, 0.2f, 0.2f),
        highlightedColor = new Color(1f, 0.3f, 0.3f),
        pressedColor = new Color(0.8f, 0.1f, 0.1f)
    };
    
    [Header("Panel Styles")]
    public PanelStyle defaultPanel = new PanelStyle();
    public PanelStyle cardPanel = new PanelStyle
    {
        backgroundColor = new Color(0.15f, 0.15f, 0.15f, 0.95f)
    };
    
    /// <summary>
    /// Apply text style to a TextMeshProUGUI component
    /// </summary>
    public void ApplyTextStyle(TextMeshProUGUI text, TextStyle style)
    {
        if (text == null || style == null) return;
        
        if (style.fontAsset != null)
            text.font = style.fontAsset;
        
        text.fontSize = style.fontSize;
        text.fontStyle = style.fontStyle;
        text.color = style.color;
        text.alignment = style.alignment;
        text.characterSpacing = style.characterSpacing;
        text.wordSpacing = style.wordSpacing;
        text.lineSpacing = style.lineSpacing;
        
        // Outline
        if (style.enableOutline)
        {
            text.outlineWidth = style.outlineWidth;
            text.outlineColor = style.outlineColor;
        }
        
        // Shadow
        if (style.enableShadow)
        {
            text.enableVertexGradient = false;
            // Note: TextMeshPro shadow is handled via material/shader
            // For simple shadow, you might need to use a separate shadow object
        }
    }
    
    /// <summary>
    /// Apply button style to a Button component
    /// </summary>
    public void ApplyButtonStyle(Button button, ButtonStyle style)
    {
        if (button == null || style == null) return;
        
        var colors = button.colors;
        colors.normalColor = style.normalColor;
        colors.highlightedColor = style.highlightedColor;
        colors.pressedColor = style.pressedColor;
        colors.disabledColor = style.disabledColor;
        button.colors = colors;
        
        var spriteState = button.spriteState;
        spriteState.highlightedSprite = style.highlightedSprite;
        spriteState.pressedSprite = style.pressedSprite;
        spriteState.disabledSprite = style.disabledSprite;
        button.spriteState = spriteState;
        
        // Apply text style to button text
        var buttonText = button.GetComponentInChildren<TextMeshProUGUI>();
        if (buttonText != null)
        {
            ApplyTextStyle(buttonText, style.textStyle);
        }
    }
    
    /// <summary>
    /// Apply panel style to an Image component (background)
    /// </summary>
    public void ApplyPanelStyle(Image panel, PanelStyle style)
    {
        if (panel == null || style == null) return;
        
        panel.color = style.backgroundColor;
        if (style.backgroundSprite != null)
            panel.sprite = style.backgroundSprite;
    }
}
