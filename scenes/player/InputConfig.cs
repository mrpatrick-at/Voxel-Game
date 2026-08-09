using Godot;
using Godot.Collections;
using System;
// Enum
public enum GameAction {
	
}
[GlobalClass]
public partial class InputConfig : Resource {
	// Public Vars
	[Export] private Dictionary<StringName, Array<InputEvent>> ActionEvents = []; // Input Map

	// Public Funcs
    public bool HasAction(StringName Action) { // Check if Action Exists
        return ActionEvents.ContainsKey(Action);
    }
    public void AddAction(StringName Action) { // Add Empty Action
        if (!ActionEvents.ContainsKey(Action)) {
            ActionEvents[Action] = [];
        }
    }
    public Array<InputEvent> GetActionEvents(StringName Action) { // Get Keys tied to Action
        if (ActionEvents.ContainsKey(Action)) {
            return ActionEvents[Action];
        }
        return [];
    }
    public void AddEvent(StringName Action, InputEvent Event) { // Add a Key to an Action
        if (!ActionEvents.ContainsKey(Action)) {
            AddAction(Action);
        }
        
        if (!ActionEvents[Action].Contains(Event)) {
            ActionEvents[Action].Add(Event);
        }
    }
    public void EraseEvents(StringName Action) { // Remove a Key from an Action
        if (ActionEvents.ContainsKey(Action)) {
            ActionEvents[Action].Clear();
        }
    }
    public void ApplyToGlobalInputMap() { // Write Input to Godot Input Map
        foreach (var pair in ActionEvents) {
            var Action = pair.Key;
            var events = pair.Value;

            if (!InputMap.HasAction(Action)) {
                InputMap.AddAction(Action);
            }

            InputMap.ActionEraseEvents(Action);

            foreach (var Event in events) {
                InputMap.ActionAddEvent(Action, Event);
            }
        }
    }
}

