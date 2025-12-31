# WitShells Dialogs Manager

A comprehensive dialog and conversation management system for Unity. Perfect for creating NPC dialogues, cutscenes, tutorials, and narrative-driven experiences.

## Features

- **Conversation System**: Organize dialogs into reusable conversation assets
- **Event-Driven Architecture**: UnityEvents for maximum flexibility and Inspector support
- **Audio Support**: Built-in audio playback for voiced dialogs
- **Typewriter Effect**: Animated text reveal with customizable speed
- **Dialog Triggers**: Start conversations based on collisions and triggers (requires Collider)
- **UI Controller**: Ready-to-use UI system with progress tracking
- **New Input System**: Full support for Unity's new Input System
- **Emotion System**: Tag dialogs with emotions for dynamic UI/animation responses
- **Loop Support**: Create conversations that loop continuously
- **Progress Tracking**: Track conversation progress with built-in utilities

## Installation

### Via Package Manager
1. Open Window > Package Manager
2. Click the + button and select "Add package from git URL"
3. Enter the package URL or add from disk

### Dependencies
- **WitShells Design Patterns** (`com.witshells.designpatterns`)
- **TextMeshPro** (`com.unity.textmeshpro`)
- **Input System** (`com.unity.inputsystem`)

## Quick Start

### 1. Create a Dialog Object
Right-click in Project window > Create > WitShells > Dialogs Manager > Dialog Object

Configure:
- **Title**: Speaker name or dialog title
- **Content**: The dialog text
- **Audio**: Optional voice audio clip
- **Portrait**: Optional speaker image
- **Emotion**: Character emotion state

### 2. Create a Conversation
Right-click in Project window > Create > WitShells > Dialogs Manager > Conversation

Configure:
- **Conversation Name**: Identifier for the conversation
- **Dialogs**: Array of DialogObjects in sequence
- **Loop**: Whether to restart after the last dialog

### 3. Setup DialogManager
Add `DialogManager` component to a GameObject in your scene. It's a singleton, so only one instance is needed.

Configure:
- **Audio Source**: For playing dialog audio
- **Auto Play Next**: Automatically advance dialogs
- **Auto Play Delay**: Delay between auto-advancing dialogs

### 4. Start a Conversation

```csharp
using WitShells.DialogsManager;

public class MyScript : MonoBehaviour
{
    public Conversation myConversation;

    void Start()
    {
        // Start the conversation
        DialogManager.Instance.StartConversation(myConversation);
        
        // Play the first dialog
        DialogManager.Instance.PlayNextDialog();
    }
}
```

## Events (UnityEvents)

All events are UnityEvents that can be configured in the Inspector or via code:

```csharp
// Subscribe via code
DialogManager.Instance.OnConversationStarted.AddListener((args) => {
    Debug.Log($"Started: {args.Conversation.ConversationName}");
});

DialogManager.Instance.OnDialogStarted.AddListener((args) => {
    Debug.Log($"Dialog: {args.Dialog.Title}");
});

DialogManager.Instance.OnDialogFinished.AddListener((args) => {
    // Dialog completed
});

DialogManager.Instance.OnConversationEnded.AddListener((args) => {
    Debug.Log($"Ended. Completed: {args.WasCompleted}");
});
```

### Available Events
- **OnConversationStarted**: Fired when a conversation begins
- **OnConversationEnded**: Fired when a conversation ends (includes WasCompleted flag)
- **OnDialogStarted**: Fired when a dialog starts playing
- **OnDialogFinished**: Fired when a dialog finishes

## Components

### DialogManager
The core singleton manager handling conversation flow.

### DialogTrigger
Attach to GameObjects with Colliders to trigger conversations via:
- Manual calls
- OnStart / OnEnable
- OnTriggerEnter / OnTriggerExit
- OnCollisionEnter / OnCollisionExit

**Note**: Requires a Collider component (added automatically via `[RequireComponent]`).

### DialogUIController
Base UI controller with:
- Title and content text display
- Portrait image
- Progress slider and text
- Typewriter effect
- New Input System support via InputActionReference

**Input Setup**: Assign InputActionReference assets for:
- **Advance Action**: Progress to next dialog
- **Skip Action**: Skip typewriter effect
- **Click Action**: Click/tap to advance (optional)

## API Reference

### DialogManager
| Method | Description |
|--------|-------------|
| `StartConversation(conversation)` | Starts a new conversation |
| `EndConversation(wasCompleted)` | Ends the current conversation |
| `PlayNextDialog()` | Plays the next dialog in sequence |
| `PlayDialog(dialog)` | Plays a specific dialog |
| `SkipCurrentDialog(playNext)` | Skips current dialog |
| `PauseDialog()` | Pauses audio playback |
| `ResumeDialog()` | Resumes audio playback |
| `GetConversationProgress()` | Returns progress (0-1) |
| `HasMoreDialogs()` | Checks if more dialogs exist |

### Conversation
| Method | Description |
|--------|-------------|
| `GetNextDialog()` | Gets and advances to next dialog |
| `GetPreviousDialog()` | Gets and moves to previous dialog |
| `GetDialogAt(index)` | Gets dialog at specific index |
| `SetDialogIndex(index)` | Sets current position |
| `ResetConversation()` | Resets to beginning |
| `GetProgress()` | Returns progress (0-1) |
| `GetTotalAudioDuration()` | Total audio length |

## Extension Methods

```csharp
using WitShells.DialogsManager;

// Conversation extensions
conversation.GetDialogsWithTag("important");
conversation.GetDialogsByEmotion(DialogEmotion.Happy);
conversation.FindDialogById("intro_01");
conversation.GetTotalWordCount();

// DialogManager extensions
DialogManager.Instance.StartAndPlay(conversation);
DialogManager.Instance.PlayAllDialogs(args => Debug.Log("Done!"));

// Dialog extensions
dialog.GetFormattedContent("{0}: {1}");
dialog.IsValid();
dialog.GetOptimalDuration();
```

## License

Copyright © WitShells. All rights reserved.
