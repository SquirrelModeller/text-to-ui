# Text to UI
Dynamically generate user interface elements at runtime based on a text configuration file. This system is built for VRChat using UdonSharp. The code parses the configuration file, instantiates UI components from specified prefabs, and organizes them into a hierarchical layout.

## Hierarchical Organization
The structure of the UI is defined using indentation in the configuration file. Each line represents a UI element, and the level of indentation (2 spaces) indicates its depth in the hierarchy.

## Configuration File Syntax
```ElementName (Element Type, ActionType)```

### Element Types:
**nav:** Navigation elements that contain other elements

**btn:** Buttons that can execute custom actions

### Action Types:
Actions are custom behaviors that can be assigned to buttons. Examples include:

**writeToScreen:** Updates text on a specified screen

**customAction:** Custom action Template

### Example Configurations:
```
MainMenu (nav)
  Messages (nav)
    Hello World (btn, writeToScreen)
    Welcome (btn, writeToScreen)
  Settings (nav)
    Volume Up (btn, adjustVolume)
    Volume Down (btn, adjustVolume)
```

## Setup Instructions

### Quick Setup
1. Drag the UIGenerator prefab into your scene
1. (Optional) Create an empty
    - Assign an action script (for example write to screen, and target a TextPro asset in the scene)
1. In the UIGenerator settings (UIGenerator -> Canvas -> Inspector) add the new empty to the list 


### Manual

1. Add the UIGenerator script to a GameObject
1. Assign your prefabs to the UIGenerator
1. Set up your action pairs:
    - Action Identifiers (strings matching your config file)
    - Action Managers (references to your action scripts)
1. Create your configuration file and assign it to the UIGenerator

*Note: There are currently strict structure requirements for prefabs added to the UIGenerator script*

## Dependencies
- VRChat Worlds SDK
- UdonSharp
- TextMeshPro

## Contribution
Follow the git conventional commits standard.