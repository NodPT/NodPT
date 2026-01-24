# technology:
- riotjs #fetch https://riot.js.org/documentation/
- component extension: .riot
- use riot observable for passing data between components #fetch https://github.com/riot/observable/tree/main/doc
- use riot router for routing #fetch https://github.com/riot/route?tab=readme-ov-file#documentation
- user litegraph.js for visual programming #fetch https://github.com/jagenjo/litegraph.js
- use bootstrap 5 for css framework
- use both dark and light mode
- use firebase for authentication
- use axios for http requests


# important coding guidelines for frontend.riot
- style files should be under a `styles` folder, do not keep style tag in the component file
- avoid keeping large component templates in the component file, use separate riot component files for better maintainability.
- in riotjs, there is no <template> tag, all html goes directly under the root of the .riot file
- use this.api to call backend api endpoints, this is the plugin that was already install in the riot.install
- use this.bus to access the global event bus for inter-component communication. bus.on(event, callback) to listen to event, bus.trigger(event, data) to emit event.
- 