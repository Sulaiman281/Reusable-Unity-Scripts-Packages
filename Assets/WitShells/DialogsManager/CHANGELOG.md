# Changelog

All notable changes to the WitShells Dialogs Manager package will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [1.0.0] - 2025-12-31

### Added
- Initial release of WitShells Dialogs Manager
- **DialogManager**: Singleton manager for handling conversations and dialog playback
  - C# events: `ConversationStarted`, `ConversationEnded`, `DialogStarted`, `DialogFinished`
  - UnityEvents for Inspector-based event handling
  - Auto-play functionality with configurable delay
  - Audio playback support
  - Pause/Resume functionality
  - Progress tracking utilities

- **Conversation**: ScriptableObject for organizing dialog sequences
  - Loop support for repeating conversations
  - Progress tracking
  - Navigation methods (next, previous, jump to index)
  - Total audio duration calculation

- **DialogObject**: ScriptableObject for individual dialog entries
  - Title, content, and audio clip support
  - Portrait sprite for speaker visualization
  - Emotion enum for dynamic UI responses
  - Tag system for categorization
  - Typing speed configuration
  - Display duration settings

- **DialogTrigger**: Component for triggering conversations
  - Multiple trigger types (Manual, OnStart, OnEnable, Collision, Trigger, Interact)
  - Tag filtering for collision events
  - One-shot trigger option
  - Configurable delay

- **DialogUIController**: Base UI controller component
  - TextMeshPro support
  - Typewriter effect with sound
  - Progress slider and text
  - Portrait display
  - Keyboard and mouse input handling

- **DialogExtensions**: Utility extension methods
  - Conversation filtering by tag and emotion
  - Dialog validation
  - Optimal duration calculation
  - Manager convenience methods

### Dependencies
- WitShells Design Patterns 1.2.1
- TextMeshPro 3.0.6
- Unity 2021.3+
