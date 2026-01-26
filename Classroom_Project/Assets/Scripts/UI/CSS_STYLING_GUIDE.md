# CSS-Like Styling System for Unity UI

## Overview

Unity doesn't use CSS, but I've created a CSS-like styling system that provides similar functionality using ScriptableObjects and helper components.

## 🎨 How It Works

### 1. **UIStyleManager** (ScriptableObject)
   - Acts like a CSS stylesheet
   - Contains predefined styles (like CSS classes)
   - Create multiple style managers for different themes

### 2. **UIStyleHelper** (Component)
   - Attach to any UI GameObject
   - Automatically applies styles on Start()
   - Works like adding a CSS class to an element

## 📋 Setup Instructions

### Step 1: Create a Style Manager Asset

1. In Unity, right-click in your `Assets` folder
2. Go to: **Create → UI → Style Manager**
3. Name it: `DefaultUIStyle` (or any name you prefer)
4. Configure your styles in the Inspector:
   - **Text Styles**: Heading, Body, Caption
   - **Button Styles**: Primary, Secondary, Success, Danger
   - **Panel Styles**: Default, Card

### Step 2: Apply Styles to TeacherUI

1. Select your `TeacherUI` GameObject in the scene
2. In the Inspector, find the **TeacherUI** component
3. Under **UI Styling**, drag your `DefaultUIStyle` asset to the **Style Manager** field
4. Styles will be applied automatically when the scene starts!

### Step 3: Apply Styles to Individual Elements (Optional)

For more control, you can add `UIStyleHelper` to individual UI elements:

1. Select any UI GameObject (Button, Text, Panel, etc.)
2. Click **Add Component**
3. Search for **UIStyle Helper**
4. Configure:
   - **Style Manager**: Drag your style asset
   - **Apply Text Style**: Check if it's a text element
   - **Text Style Class**: Choose Heading, Body, or Caption
   - **Apply Button Style**: Check if it's a button
   - **Button Style Class**: Choose Primary, Secondary, Success, or Danger

## 🎯 Usage Examples

### Example 1: Style All Buttons in TeacherUI

```csharp
// In TeacherUI.cs (already integrated)
void ApplyUIStyles()
{
    if (styleManager == null) return;
    
    // Apply success style to praise button
    styleManager.ApplyButtonStyle(praiseButton, styleManager.successButton);
    
    // Apply danger style to yell button
    styleManager.ApplyButtonStyle(yellButton, styleManager.dangerButton);
}
```

### Example 2: Style Text Elements

```csharp
// Style a heading
styleManager.ApplyTextStyle(scoreValueText, styleManager.headingStyle);

// Style body text
styleManager.ApplyTextStyle(engagementText, styleManager.bodyStyle);

// Style caption
styleManager.ApplyTextStyle(sessionTimeText, styleManager.captionStyle);
```

### Example 3: Style Panels

```csharp
// Style a panel background
styleManager.ApplyPanelStyle(endSessionPanelBackground, styleManager.cardPanel);
```

### Example 4: Create Custom Styles at Runtime

```csharp
// Create a custom text style
var customStyle = new UIStyleManager.TextStyle
{
    fontSize = 20,
    color = Color.cyan,
    fontStyle = FontStyles.Bold,
    enableOutline = true,
    outlineColor = Color.black,
    outlineWidth = 0.3f
};

// Apply it
styleManager.ApplyTextStyle(myText, customStyle);
```

## 🎨 Style Properties

### TextStyle Properties
- **Font Asset**: TMP font to use
- **Font Size**: Text size
- **Font Style**: Bold, Italic, etc.
- **Color**: Text color
- **Alignment**: Left, Center, Right, etc.
- **Character/Word/Line Spacing**: Text spacing
- **Outline**: Enable outline with color and width
- **Shadow**: Enable shadow (requires shader setup)

### ButtonStyle Properties
- **Normal Color**: Default button color
- **Highlighted Color**: Color when hovered
- **Pressed Color**: Color when clicked
- **Disabled Color**: Color when disabled
- **Sprites**: Custom sprites for each state
- **Text Style**: Style for button text

### PanelStyle Properties
- **Background Color**: Panel background color
- **Background Sprite**: Optional background image
- **Border**: Show border with color and width
- **Padding**: Internal spacing

## 🔧 Advanced: Multiple Themes

Create multiple style managers for different themes:

1. **DefaultUIStyle** - Light theme
2. **DarkUIStyle** - Dark theme
3. **ColorfulUIStyle** - Colorful theme

Then switch themes at runtime:

```csharp
public void SwitchTheme(UIStyleManager newTheme)
{
    styleManager = newTheme;
    ApplyUIStyles();
}
```

## 📝 CSS to Unity Style Comparison

| CSS | Unity Equivalent |
|-----|------------------|
| `.heading { font-size: 24px; }` | `styleManager.headingStyle.fontSize = 24` |
| `.button-primary { background-color: blue; }` | `styleManager.primaryButton.normalColor = Color.blue` |
| `.panel { padding: 10px; }` | `styleManager.defaultPanel.padding = new RectOffset(10, 10, 10, 10)` |
| `color: #FF0000` | `color = GetColorFromHex("FF0000")` |

## 🚀 Quick Start Checklist

- [ ] Create a `UIStyleManager` ScriptableObject asset
- [ ] Configure text styles (Heading, Body, Caption)
- [ ] Configure button styles (Primary, Secondary, Success, Danger)
- [ ] Configure panel styles (Default, Card)
- [ ] Assign style manager to `TeacherUI.styleManager`
- [ ] Test in Play mode to see styles applied
- [ ] (Optional) Add `UIStyleHelper` to individual elements for fine control

## 💡 Tips

1. **Create Style Prefabs**: Save styled UI elements as prefabs for reuse
2. **Use ScriptableObjects**: Create multiple style managers for different themes
3. **Runtime Switching**: Change styles dynamically based on user preferences
4. **Inherit Styles**: Create base styles and extend them for variations
5. **Color Palettes**: Define color constants and reuse them across styles

## 🎓 Best Practices

1. **Consistency**: Use the same style manager across all UI
2. **Hierarchy**: Apply styles at the highest level possible
3. **Performance**: Styles are applied once at Start(), not every frame
4. **Organization**: Group related styles together
5. **Documentation**: Document your style choices in the style manager

## 🔍 Troubleshooting

**Styles not applying?**
- Check that `styleManager` is assigned in Inspector
- Verify the UI element has the correct component (TextMeshProUGUI, Button, Image)
- Check console for warnings

**Styles look wrong?**
- Verify font assets are assigned
- Check color values are correct
- Ensure sprites are assigned if using custom button sprites

**Performance issues?**
- Styles are applied once at Start(), so performance should be fine
- If styling many elements, consider batching the ApplyStyles() calls

---

**Note**: This system provides CSS-like functionality but uses Unity's native systems. It's not actual CSS, but gives you similar control and organization!
