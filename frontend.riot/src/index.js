import "@riotjs/hot-reload";
import { component, install } from "riot";
import App from "./app.riot";
import registerGlobalComponents from "./register-global-components.js";
import eventBusPlugin from "./services/eventBusPlugin.js";

// Install event bus plugin to make $eventBus available in all components
install(eventBusPlugin);

// register
registerGlobalComponents();

// mount the root tag
component(App)(document.getElementById("root"));
