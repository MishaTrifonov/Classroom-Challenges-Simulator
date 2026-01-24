# StudentReactionAnimator - Usage Guide

## Overview
`StudentReactionAnimator` is a comprehensive model for managing all student reaction animations in the classroom simulator. It acts as a centralized bridge between emotional states, behavioral states, and the Unity Animator component.

## Features

### 1. Behavioral State Animations
Automatically synchronizes with `StudentAgent.currentState` and updates animator accordingly:
- Listening
- Engaged
- Distracted
- SideTalk (Talking)
- Arguing
- Withdrawn

### 2. Emotional Reactions
Automatic reactions based on emotion thresholds (configurable):
- **Happy** - Triggered when Happiness >= threshold
- **Sad** - Triggered when Sadness >= threshold
- **Angry** - Triggered when Anger >= threshold
- **Frustrated** - Triggered when Frustration >= threshold
- **Bored** - Triggered when Boredom >= threshold

Additional reactions:
- **Celebrating** - For success and praise
- **Shocked** - For surprises
- **Confused** - For misunderstandings

### 3. Specific Actions
- **Crying** - Automatically triggered when Sadness = 10
- **Raise Hand** - Manual trigger for participation
- **Walking** - Movement animation with speed control

### 4. Teacher Action Responses
Automatically reacts to teacher actions with appropriate animations based on student personality.

---

## Setup Instructions

### 1. Add Component to Student Prefab
```
1. Select your student prefab/GameObject
2. Add Component -> StudentReactionAnimator
3. Assign the StudentAgent reference (auto-detects if on same GameObject)
```

### 2. Animator Configuration
The animator expects these parameters:

#### Boolean Parameters:
- `IsListening`
- `IsEngaged`
- `IsDistracted`
- `IsTalking`
- `IsArguing`
- `IsWithdrawn`
- `IsCrying`
- `IsWalking`

#### Trigger Parameters:
- **`RaiseHand`** — Name must be exactly `RaiseHand` (case-sensitive). Hand rise runs without camera shift when `CameraController.autoFocusOnEagerStudents` is disabled.
- `TriggerHappy` (optional)
- `TriggerSad` (optional)
- `TriggerAngry` (optional)
- `TriggerFrustrated` (optional)
- `TriggerBored` (optional)
- `TriggerCelebrate` (optional)
- `TriggerShocked` (optional)
- `TriggerConfused` (optional)

#### Float Parameters:
- `WalkSpeed` (optional)

**Note:** The system gracefully handles missing parameters - if an optional trigger doesn't exist in your animator, it simply won't try to set it.

---

## Usage Examples

### Basic Usage (Automatic)
Once attached, the system works automatically:
```csharp
// The component automatically:
// 1. Monitors StudentAgent.currentState and updates animations
// 2. Watches emotion levels and triggers reactions
// 3. Handles crying when sadness = 10
```

### Manual Trigger - Raise Hand
```csharp
StudentReactionAnimator animator = student.GetComponent<StudentReactionAnimator>();
animator.RaiseHand();
```

### Manual Trigger - Specific Emotion
```csharp
animator.TriggerHappyReaction();
animator.TriggerAngryReaction();
animator.TriggerCelebrationReaction();
```

### React to Teacher Action
```csharp
animator.ReactToTeacherAction(ActionType.Praise);
animator.ReactToTeacherAction(ActionType.Yell);
```

### React to Emotional Trigger
```csharp
animator.ReactToEmotionalTrigger(EmotionalTrigger.WrongAnswerPublic);
animator.ReactToEmotionalTrigger(EmotionalTrigger.SuccessfulContribution);
```

### Update Walking Animation
```csharp
animator.UpdateWalkingAnimation(true, 1.5f); // Walking at 1.5x speed
animator.UpdateWalkingAnimation(false);      // Stop walking
```

### Get Animation State Info (Debugging)
```csharp
string info = animator.GetAnimationStateInfo();
Debug.Log(info);
// Output: "State: Engaged, Reaction: Happy, Crying: False, RaisingHand: False, Walking: False"
```

---

## Configuration Options

### Inspector Settings

#### Reaction Configuration
- **Quick Reaction Duration** (default: 2s) - How long quick reactions last
- **Emotional Reaction Cooldown** (default: 5s) - Cooldown between automatic emotion reactions
- **Enable Automatic Emotional Reactions** (default: true) - Toggle automatic reactions

#### Thresholds
- **Emotion Threshold** (default: 7) - Emotion level (1-10) required to trigger automatic reactions

---

## Integration with Existing Code

### Option 1: Replace StudentAgent Animation Code
You can optionally remove the animation code from `StudentAgent.cs` and let `StudentReactionAnimator` handle it all:

```csharp
// In StudentAgent.cs - REMOVE or comment out:
// - OnStateEnter() animator code
// - TriggerCryingAnimation()
// - StopCryingAnimation()
// - RaiseHand() animator code
// - UpdateMovementAnimations() animator code

// The StudentReactionAnimator will handle all of this automatically
```

### Option 2: Keep Both (Hybrid)
Keep existing code but add calls to `StudentReactionAnimator` for enhanced reactions:

```csharp
// In StudentAgent.cs ReceiveTeacherAction():
public void ReceiveTeacherAction(TeacherAction action)
{
    // ... existing emotion code ...

    // Add reaction animation
    StudentReactionAnimator reactionAnimator = GetComponent<StudentReactionAnimator>();
    if (reactionAnimator != null)
    {
        reactionAnimator.ReactToTeacherAction(action.Type);
    }
}
```

---

## Advanced Features

### Custom Reaction Sequences
Create complex reaction sequences by combining methods:

```csharp
IEnumerator ComplexReaction()
{
    animator.TriggerShockedReaction();
    yield return new WaitForSeconds(2f);
    animator.TriggerSadReaction();
    yield return new WaitForSeconds(2f);
    animator.TriggerAngryReaction();
}
```

### Personality-Based Reactions
The system already considers personality traits:
- High rebelliousness → angry reaction to yelling
- High sensitivity → shocked reaction to being called to board

### Debugging
- Enable Gizmos in Scene view to see animation state labels above students
- Check console logs for reaction triggers
- Use `GetAnimationStateInfo()` for runtime debugging

---

## Performance Notes

- Efficient parameter checking prevents errors with missing animator parameters
- Cooldown system prevents spam of automatic reactions
- Only checks emotional reactions at configurable intervals

---

## Extensibility

### Adding New Reactions

1. **Add trigger name to constants:**
```csharp
private static readonly string TRIGGER_NEW_REACTION = "TriggerNewReaction";
```

2. **Add ReactionType enum value:**
```csharp
public enum ReactionType
{
    // ... existing ...
    NewReaction
}
```

3. **Create trigger method:**
```csharp
public void TriggerNewReaction()
{
    if (!HasAnimatorParameter(TRIGGER_NEW_REACTION)) return;
    animator.SetTrigger(TRIGGER_NEW_REACTION);
    currentReaction = ReactionType.NewReaction;
    Debug.Log($"[StudentReactionAnimator] {studentAgent.studentName} triggered new reaction");
}
```

4. **Add to animator controller** in Unity

---

## Troubleshooting

### Reactions Not Triggering
- Check that animator parameters exist and match spelling
- Verify `studentAgent` reference is assigned
- Check emotion threshold settings
- Ensure `enableAutomaticEmotionalReactions` is true

### Animation Stuck in One State
- Check for conflicting scripts setting animator parameters
- Use `ResetAllAnimationStates()` to reset
- Verify state transitions in animator controller

### Performance Issues
- Increase `emotionalReactionCooldown` value
- Disable `enableAutomaticEmotionalReactions` if not needed
- Check for multiple scripts controlling same animator

---

## API Reference

### Public Methods

| Method | Description |
|--------|-------------|
| `TransitionToBehavioralState(StudentState)` | Manually transition to behavioral state |
| `TriggerHappyReaction()` | Trigger happy emotion reaction |
| `TriggerSadReaction()` | Trigger sad emotion reaction |
| `TriggerAngryReaction()` | Trigger angry emotion reaction |
| `TriggerFrustratedReaction()` | Trigger frustrated emotion reaction |
| `TriggerBoredReaction()` | Trigger bored emotion reaction |
| `TriggerCelebrationReaction()` | Trigger celebration reaction |
| `TriggerShockedReaction()` | Trigger shocked/surprised reaction |
| `TriggerConfusedReaction()` | Trigger confused reaction |
| `RaiseHand()` | Trigger raise hand animation |
| `UpdateWalkingAnimation(bool, float)` | Update walking state and speed |
| `ReactToTeacherAction(ActionType)` | React to teacher action |
| `ReactToEmotionalTrigger(EmotionalTrigger)` | React to emotional trigger |
| `ResetAllAnimationStates()` | Reset all animations to default |
| `GetAnimationStateInfo()` | Get debug info string |

---

## Example Integration Workflow

1. **Attach to all student prefabs**
2. **Configure animator controller** with required parameters
3. **Test automatic reactions** by modifying student emotions in runtime
4. **Fine-tune thresholds** for desired behavior
5. **Optional:** Integrate manual reaction triggers in gameplay events
6. **Optional:** Replace StudentAgent animation code to centralize all animation logic

---

For questions or issues, check the console logs with the `[StudentReactionAnimator]` prefix.
