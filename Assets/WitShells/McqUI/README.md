# WitShells MCQ UI

A versatile and reusable Multiple Choice Question UI system for Unity.

## Features

- **Centralized Settings**: Global settings system with singleton pattern for easy configuration
- **Customizable Option Items**: Interactive option buttons with visual feedback
- **MCQ Page Manager**: Complete page management for multiple choice questions  
- **Data-Driven**: Simple data structure for questions and answers
- **Visual Feedback**: Color-coded correct/incorrect responses with animations
- **Override System**: Global settings with per-component override options
- **Reusable**: Easy to integrate into any Unity project
- **Flexible**: Support for different question types and configurations

## Quick Start

1. **Configure global settings** (optional):
```csharp
// Access global settings anywhere
var settings = McqSettings.Instance;
Debug.Log($"Fill Duration: {settings.FillDuration}");
```

2. Create an `McqData` with your question and options:
```csharp
var mcqData = new McqData(
    "What is the capital of France?",
    "Paris", 
    new string[] { "London", "Berlin", "Madrid" }
);
```

3. Use the `McqPage` component to display your question:
```csharp
mcqPage.SetupQuestion(mcqData);
mcqPage.OnAnswerSelected += HandleAnswerSelected;
```

4. Handle the answer selection:
```csharp
void HandleAnswerSelected(string selectedAnswer, bool isCorrect)
{
    Debug.Log($"Selected: {selectedAnswer}, Correct: {isCorrect}");
}
```

## Components

### McqSettings (New!)
Centralized singleton settings system that controls all MCQ behavior globally. Can be overridden per-component if needed.

### McqData
Contains question text, correct answer, and list of wrong options. Now uses global settings for defaults.

### McqOptionItem
Individual option button with selection animations and visual feedback. Uses global settings with override capability.

### McqPage
Main component that manages the entire MCQ display and interaction. Uses global settings with override capability.

## Installation

Import this package into your Unity project and start using the MCQ system immediately.

## License

MIT License - see LICENSE file for details.