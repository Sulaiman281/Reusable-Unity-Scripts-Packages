# CustomAssets

Welcome to this collection of reusable assets and tools for Unity game development.

In this repository, you will find multiple Unity packages designed to accelerate your workflow and improve code quality, including design patterns, API integration, character control systems, and actor frameworks.

---

## 📦 Available Packages

### **Design Patterns**  
A robust, production-ready set of classic and modern design patterns, tailored for Unity projects.
- Singleton, Factory, Observer, Command, State Machine patterns
- Unity-optimized implementations
- Comprehensive documentation and examples

### **API Integration**  
A powerful package that makes it easy to integrate any RESTful API into your Unity project with minimal code.
- Quickly define endpoints and headers
- Send and test API requests directly inside the Unity Editor
- Supports custom headers, request bodies, and UnityEvents for success/failure callbacks
- Great for prototyping, debugging, and production use

### **Third Person Control**  
A complete third-person character controller system with advanced features.
- Smooth locomotion with walk, run, crouch, and sprint
- Jump mechanics with proper ground detection
- Camera-relative movement controls
- Cinemachine integration for professional camera handling
- Customizable movement parameters
- Animation controller included

### **WitActor**  
An advanced actor framework for complex character behaviors and state management.
- Sophisticated locomotion state machines
- Modular character component system
- Advanced animation blending
- Extensible architecture for custom behaviors
- Integrated with Third Person Control system

More packages will be added over time!

---

## 📥 Package Installation Guide

All packages can be installed using Unity's Package Manager with Git URLs. Choose the packages you need:

### **API Integration Package**

```
https://github.com/Sulaiman281/Reusable-Unity-Scripts-Packages.git?path=Assets/WitShells/ApiIntegration
```

### **Design Patterns Package**

```
https://github.com/Sulaiman281/Reusable-Unity-Scripts-Packages.git?path=Assets/WitShells/DesignPatterns
```

### **Third Person Control Package**

```
https://github.com/Sulaiman281/Reusable-Unity-Scripts-Packages.git?path=Assets/WitShells/ThirdPersonControl
```

### **WitActor Package**

```
https://github.com/Sulaiman281/Reusable-Unity-Scripts-Packages.git?path=Assets/WitShells/WitActor
```


### **Threading Job**

```
https://github.com/Sulaiman281/Reusable-Unity-Scripts-Packages.git?path=Assets/WitShells/ThreadingJob
```

---

## 🚀 Installation Steps

### 1. Open Your Unity Project

Open the Unity project where you want to add the packages.

### 2. Open the Package Manager

Go to **Window → Package Manager**.

### 3. Add Package from Git URL

Click the **+** button (top left), then select **Add package from Git URL...**

### 4. Paste the Git URL

Copy and paste one of the Git URLs above for the package you want to install.

### 5. Wait for Import

Unity will download and import the package. Repeat for additional packages as needed.

---

## 📋 Package Dependencies

Some packages have dependencies on others. Install them in the following order for best results:

### **For Character Control Systems:**

1. **Design Patterns** (foundation)
2. **Third Person Control** (basic character controller)
3. **WitActor** (advanced actor system) - *depends on Third Person Control*

### **For API Integration:**

1. **API Integration** (standalone - no dependencies)

### **Cinemachine Dependency:**

The **Third Person Control** and **WitActor** packages require **Cinemachine**:

1. Open **Window → Package Manager**
2. Switch to **Unity Registry**
3. Search for "Cinemachine"
4. Click **Install**

---

## 🛠️ Quick Setup Guides

### **Third Person Control Setup**

After installing the package:

1. Select your character GameObject in the hierarchy
2. Go to **WitShells → ThirdPersonSetup → Setup ThirdPerson Character**
3. The setup will automatically:
   - Add required components (CharacterController, ThirdPersonControl)
   - Configure the animator with locomotion controller
   - Create camera target and setup Cinemachine camera
   - Add CinemachineBrain to Main Camera

### **WitActor Setup**

The WitActor package extends Third Person Control:

1. First complete the Third Person Control setup
2. Add WitActor components as needed for advanced behaviors
3. Configure the locomotion state machine for complex character actions

### **API Integration Setup**

1. Go to **WitShells → API Integration → API Test Window**
2. Define your API endpoints and test them directly in the editor
3. Use the generated code in your runtime scripts

---

## 📖 Documentation & Examples

Each package includes comprehensive documentation:

### **Design Patterns**
- `DesignPatternGuide.md` - Complete guide with code examples
- Example scenes demonstrating each pattern
- Best practices and Unity-specific implementations

### **API Integration**
- Built-in editor documentation
- Real-time API testing tools
- Example integration scripts

### **Third Person Control**
- Component reference guide
- Movement customization examples
- Camera setup tutorials

### **WitActor**
- Advanced state machine documentation
- Character behavior scripting guides
- Animation integration examples

---

## 🎯 Use Cases

### **Game Development**
- Rapid prototyping with design patterns
- Professional character controllers
- API integration for online features
- Modular actor systems for complex NPCs

### **Educational Projects**
- Learn design patterns in practice
- Understand character controller mechanics
- Explore state machine architectures
- Practice API integration techniques

### **Production Ready**
- All packages are optimized for production use
- Comprehensive error handling
- Performance-focused implementations
- Extensible architectures

---

## ⚡ Features Comparison

| Feature | Design Patterns | API Integration | Third Person Control | WitActor |
|---------|----------------|-----------------|---------------------|----------|
| **Complexity** | Beginner-Advanced | Beginner-Intermediate | Intermediate | Advanced |
| **Dependencies** | None | None | Cinemachine | Third Person Control |
| **Editor Tools** | Examples | Test Window | Setup Wizard | State Debugger |
| **Runtime Performance** | Excellent | Good | Excellent | Excellent |
| **Customization** | High | Medium | High | Very High |

---

## 🔧 System Requirements

- **Unity Version:** 2021.3 LTS or newer
- **Target Platforms:** All Unity-supported platforms
- **.NET Standard:** 2.1 compatibility
- **Dependencies:** Package-specific (see above)

---

## 🐛 Troubleshooting

### **Common Issues:**

**Package Import Fails:**
- Ensure stable internet connection
- Check Unity Package Manager cache
- Try importing packages individually

**Missing Dependencies:**
- Install Cinemachine for character control packages
- Follow the dependency installation order

**Compilation Errors:**
- Ensure all dependencies are installed
- Check Unity version compatibility
- Restart Unity if needed

### **Getting Help:**

1. Check package documentation first
2. Review example scenes and scripts
3. Open GitHub issues for bugs
4. Contribute improvements via pull requests

---

## 💡 More Assets Coming Soon

Stay tuned for additional reusable Unity packages in this repository:

- **Inventory System** - Comprehensive item management
- **Dialogue System** - Branching conversation trees
- **Save System** - Flexible data persistence
- **Audio Manager** - Advanced sound management
- **UI Framework** - Modular interface system

---

## 📣 Feedback & Contributions

We welcome community involvement!

### **How to Contribute:**
- Report bugs via GitHub Issues
- Suggest features and improvements
- Submit pull requests with enhancements
- Share your use cases and examples

### **Contact:**
- **Author:** Syed Suleman
- **Email:** sayedsulaiman607@gmail.com
- **GitHub:** [Sulaiman281](https://github.com/Sulaiman281)

### **License:**
All packages are provided under MIT License - see individual package documentation for details.

---

**Happy Coding! 🚀**
