# Quick Setup: Raise Hand Animation

## TL;DR - 3 Steps

1. **Open Animator Controller** (from student prefab's Animator component)

2. **Add Trigger Parameter**:
   - Click **+** in Parameters tab
   - Type: **Trigger**
   - Name: **`RaiseHand`** (exact, case-sensitive)

3. **Add Animation State & Transitions**:
   - Create new state named "RaiseHand"
   - Assign `student_raise_hand.anim` to the state
   - Create transition from **Any State** → **RaiseHand** state
     - Condition: **RaiseHand** trigger
     - Uncheck "Has Exit Time"
   - Create transition from **RaiseHand** → **IsListening** (or your default state)
     - Check "Has Exit Time" (0.9)
     - No conditions needed

## Test It

The animation will automatically trigger when:
- Teacher asks a question (if `StudentQuestionResponder` is set up)
- Student is Engaged and has an answer
- You call `studentAgent.RaiseHand()` from code

## Troubleshooting

**Animation not playing?**
- Check Console for warnings about missing "RaiseHand" parameter
- Verify Animator Controller is assigned to student's Animator component
- Make sure animation file `student_raise_hand.anim` exists

**See full guide**: `RAISE_HAND_ANIMATION_SETUP.md`
