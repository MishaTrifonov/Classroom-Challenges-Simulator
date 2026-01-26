# Raise Hand Animation Setup Guide

This guide will help you connect the raise hand animation to your code in Unity.

## Current Code Status

The code is already set up to trigger the raise hand animation:
- `StudentAgent.RaiseHand()` - Triggers the animation
- `StudentReactionAnimator.RaiseHand()` - Also triggers the animation
- `StudentQuestionResponder` - Automatically calls raise hand when students want to answer

## Step-by-Step Unity Setup

### Step 1: Open Your Animator Controller

1. In Unity, navigate to your student prefab (e.g., `studentfemale.prefab` or `studentagent.prefab`)
2. Find the **Animator** component on the student GameObject
3. Click on the **Controller** field to open the Animator Controller
4. The Animator window should open showing the state machine

### Step 2: Add the "RaiseHand" Trigger Parameter

1. In the **Animator** window, look at the **Parameters** tab (usually at the top-left)
2. Click the **+** button to add a new parameter
3. Select **Trigger** from the dropdown
4. Name it exactly: **`RaiseHand`** (case-sensitive, no spaces)
5. The parameter should now appear in your Parameters list

### Step 3: Add the Raise Hand Animation State

1. In the Animator window, right-click in an empty area
2. Select **Create State** → **Empty**
3. Name the state: **"RaiseHand"** or **"Raise Hand"**
4. Select the new state
5. In the **Inspector** panel, find the **Motion** field
6. Drag your `student_raise_hand.anim` animation file into the Motion field
   - The animation file should be at: `Assets/Prefabs/student_raise_hand.anim`

### Step 4: Create Transitions

You need to create transitions FROM your base states TO the RaiseHand state:

#### Option A: From Any State (Recommended for Quick Reactions)

1. Right-click on **Any State** (the orange state at the top)
2. Select **Make Transition**
3. Click on your **RaiseHand** state
4. Select the transition arrow
5. In the **Inspector**, uncheck **Has Exit Time**
6. In **Conditions**, click **+** and select **RaiseHand** trigger
7. Set **Transition Duration** to a low value (0.1-0.2 seconds)

#### Option B: From Specific States (More Control)

Create transitions from each state that should allow raising hand:

1. Right-click on **IsListening** state (or your default idle state)
2. Select **Make Transition** → Click **RaiseHand** state
3. Configure the transition:
   - Uncheck **Has Exit Time**
   - Add condition: **RaiseHand** trigger
   - Set **Transition Duration**: 0.1-0.2 seconds

4. Repeat for other states like:
   - **IsEngaged** → **RaiseHand**
   - **IsListening** → **RaiseHand**

### Step 5: Create Transition Back to Base State

After the raise hand animation plays, the student should return to their previous state:

1. Right-click on **RaiseHand** state
2. Select **Make Transition**
3. Click on your default/idle state (e.g., **IsListening** or **IsEngaged**)
4. Select the transition arrow
5. In the **Inspector**:
   - Check **Has Exit Time**
   - Set **Exit Time** to 0.9 (plays 90% of animation before transitioning)
   - Set **Transition Duration**: 0.2-0.3 seconds
   - Leave **Conditions** empty (or add a condition if needed)

### Step 6: Test the Animation

1. Select your student GameObject in the scene
2. In the **Inspector**, find the **StudentAgent** component
3. You can test by:
   - Clicking **Play** in Unity
   - Or manually calling `RaiseHand()` from code
   - Or asking a question (if `StudentQuestionResponder` is set up)

## Troubleshooting

### Animation Not Playing?

1. **Check Parameter Name**: The trigger must be named exactly `RaiseHand` (case-sensitive)
2. **Check Animator Reference**: Ensure the student GameObject has an Animator component with the correct controller assigned
3. **Check Animation File**: Verify `student_raise_hand.anim` exists and is assigned to the state
4. **Check Transitions**: Make sure transitions are set up correctly with the trigger condition
5. **Check Exit Time**: If using exit time, make sure it's set appropriately

### Debug in Code

Add this to see if the trigger is being called:

```csharp
// In StudentAgent.RaiseHand() or StudentReactionAnimator.RaiseHand()
Debug.Log($"[RaiseHand] Triggering animation for {studentName}");
if (animator != null)
{
    bool hasParameter = false;
    foreach (AnimatorControllerParameter param in animator.parameters)
    {
        if (param.name == "RaiseHand" && param.type == AnimatorControllerParameterType.Trigger)
        {
            hasParameter = true;
            break;
        }
    }
    
    if (hasParameter)
    {
        animator.SetTrigger("RaiseHand");
        Debug.Log($"[RaiseHand] Trigger set successfully");
    }
    else
    {
        Debug.LogError($"[RaiseHand] ERROR: 'RaiseHand' trigger parameter not found in Animator Controller!");
    }
}
else
{
    Debug.LogError($"[RaiseHand] ERROR: Animator component is null!");
}
```

## Animation File Location

The raise hand animation should be at:
- `Assets/Prefabs/student_raise_hand.anim`

If it's missing, you may need to:
1. Import/create the animation
2. Or use a different animation file
3. Or create a simple animation in Unity

## Code Integration Points

The raise hand animation is triggered in these places:

1. **Automatic (When Student Has Answer)**:
   - `StudentAgent.ExecuteStateBehavior()` - When student is Engaged and has an answer
   - `StudentQuestionResponder.DisplayEagernessCoroutine()` - When teacher asks a question

2. **Manual (From Code)**:
   - `StudentAgent.RaiseHand()` - Public method you can call
   - `StudentReactionAnimator.RaiseHand()` - Public method you can call

## Example: Trigger Raise Hand from TeacherUI

If you want to manually trigger raise hand from the teacher UI:

```csharp
// In TeacherUI.cs
public void OnCallOnStudentButtonClicked()
{
    if (selectedStudent != null)
    {
        selectedStudent.RaiseHand(); // This will trigger the animation
    }
}
```

## Next Steps

1. Follow the setup steps above
2. Test the animation in Play mode
3. Adjust transition timings if needed
4. Add more states/transitions as needed for your specific use case
