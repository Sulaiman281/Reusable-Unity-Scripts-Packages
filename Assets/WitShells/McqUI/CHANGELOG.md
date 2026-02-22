# Changelog

All notable changes to this package will be documented in this file.

The format is based on [Keep a Changelog](http://keepachangelog.com/en/1.0.0/)
and this project adheres to [Semantic Versioning](http://semver.org/spec/v2.0.0.html).

## [1.0.0] - 2026-01-28

### Added
- Initial release of WitShells MCQ UI package
- `McqData` class for storing question, correct answer, and wrong options
- `McqOptionItem` component for individual option display with animations
- `McqPage` component for managing complete MCQ display and interaction
- `McqDataSet` ScriptableObject for easy question set creation in editor
- `McqUtilities` static class with helper methods for MCQ management
- `McqExample` script demonstrating how to use the system
- Custom editor scripts for improved Unity Editor experience
- Support for timed questions with countdown display
- Visual feedback system with correct/incorrect color coding
- Option shuffling and randomization features
- Typewriter text effects for questions and options
- Complete event system for answer selection and completion
- Auto-reveal functionality for correct answers