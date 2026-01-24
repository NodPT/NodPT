import { expect, describe, it, beforeEach } from "vitest";
import { install } from "riot";
import eventBusPlugin, { eventBus } from "./eventBusPlugin.js";

describe("Event Bus Plugin Unit Test", () => {
  beforeEach(() => {
    // Clean up any existing listeners before each test
    eventBus.off("*");
  });

  it("Event bus is created and can trigger/listen to events", () => {
    let receivedData = null;
    
    // Listen to an event
    eventBus.on("test-event", (data) => {
      receivedData = data;
    });
    
    // Trigger the event
    eventBus.trigger("test-event", { message: "Hello World" });
    
    // Check if the event was received
    expect(receivedData).toBeDefined();
    expect(receivedData.message).toBe("Hello World");
  });

  it("Event bus can listen once with one()", () => {
    let callCount = 0;
    
    // Listen once to an event
    eventBus.one("once-event", () => {
      callCount++;
    });
    
    // Trigger the event multiple times
    eventBus.trigger("once-event");
    eventBus.trigger("once-event");
    eventBus.trigger("once-event");
    
    // Should only be called once
    expect(callCount).toBe(1);
  });

  it("Event bus can remove listeners with off()", () => {
    let callCount = 0;
    
    const handler = () => {
      callCount++;
    };
    
    // Add listener
    eventBus.on("remove-event", handler);
    
    // Trigger once
    eventBus.trigger("remove-event");
    expect(callCount).toBe(1);
    
    // Remove listener
    eventBus.off("remove-event", handler);
    
    // Trigger again - should not increase count
    eventBus.trigger("remove-event");
    expect(callCount).toBe(1);
  });

  it("Plugin adds $eventBus to component prototype", () => {
    // Create a mock component class
    const MockComponent = function() {};
    MockComponent.prototype = {};
    
    // Install the plugin
    eventBusPlugin(MockComponent);
    
    // Check if $eventBus is added to the prototype
    expect(MockComponent.prototype.$eventBus).toBeDefined();
    expect(MockComponent.prototype.$eventBus).toBe(eventBus);
  });

  it("Multiple components can communicate through event bus", () => {
    let component1Received = null;
    let component2Received = null;
    
    // Simulate two components listening to the same event
    eventBus.on("shared-event", (data) => {
      component1Received = data;
    });
    
    eventBus.on("shared-event", (data) => {
      component2Received = data;
    });
    
    // Trigger the event
    const testData = { value: 42 };
    eventBus.trigger("shared-event", testData);
    
    // Both components should receive the data
    expect(component1Received).toEqual(testData);
    expect(component2Received).toEqual(testData);
  });
});
