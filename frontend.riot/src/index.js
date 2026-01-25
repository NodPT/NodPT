import "@riotjs/hot-reload";
import "bootstrap/dist/css/bootstrap.min.css";
import "bootstrap-icons/font/bootstrap-icons.css";
import "./styles/landing-page.css";
import "./styles/landing-page-dark.css";
import "./styles/landing-page-light-scoped.css";
import { component, install } from "riot";
import App from "./app.riot";
import eventBusPlugin from "./plugins/bus.js";
import apiPlugin from "./plugins/api-plugin.js";

// Install event bus plugin to make bus available in all components
install(eventBusPlugin);
// Install api plugin to make $api available in all components
install(apiPlugin);

// mount the root tag
component(App)(document.getElementById("root"));
