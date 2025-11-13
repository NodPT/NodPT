import { triggerEvent, EVENT_TYPES } from './eventBus.js';
import { NODE_TYPES } from './SimpleNode.js';
/**
 * Creates demo nodes with 4-level hierarchy for testing the Rete editor
 * Hierarchy: Director -> Managers -> Inspectors -> Agents
 * @param {Object} editor - The Rete editor instance
 * @param {Object} area - The Rete area instance
 * @param {null} _ - Unused parameter (kept for backward compatibility)
 * @param {Function} emit - Vue emit function to notify parent component
 */
export async function createDemoNodes(editorManager) {
	try {

                // Create hierarchy: 1 Director -> 5 Managers -> 11 Inspectors -> 26 Agents
                // Animated sequence: Director first, then wait 1s for managers, 2s for inspectors, 3s for agents

                // Phase 1: Director (0s)
                const directorNode = await editorManager.addNode(NODE_TYPES.DIRECTOR, 'Director'); // 0 inputs, 2 outputs
                editorManager.arrangeNodes();

		// Phase 2: Managers (after 1s)
		setTimeout(async () => {
			// Create all 5 managers
                        const mgr1 = await directorNode.addChild(NODE_TYPES.MANAGER, 'Employee');
                        const mgr2 = await directorNode.addChild(NODE_TYPES.MANAGER, 'Salary');
                        const mgr3 = await directorNode.addChild(NODE_TYPES.MANAGER, 'Assessment');
                        const mgr4 = await directorNode.addChild(NODE_TYPES.MANAGER, 'Promotion');
                        const mgr5 = await directorNode.addChild(NODE_TYPES.MANAGER, 'SIR');
			
			editorManager.arrangeNodes();

			// Phase 3: Inspectors (after 2 more seconds, 3s total)
			setTimeout(async () => {
				// Create inspectors for each manager
				const inspector1Node = await mgr1.addChild(NODE_TYPES.INSPECTOR, 'Frontend');
				const inspector2Node = await mgr1.addChild(NODE_TYPES.INSPECTOR, 'Backend');
				const inspector3Node = await mgr1.addChild(NODE_TYPES.INSPECTOR, 'DB');

				const inspector4Node = await mgr2.addChild(NODE_TYPES.INSPECTOR, 'Frontend');
				const inspector5Node = await mgr2.addChild(NODE_TYPES.INSPECTOR, 'Backend');

				const inspector6Node = await mgr3.addChild(NODE_TYPES.INSPECTOR, 'Frontend');
				const inspector7Node = await mgr3.addChild(NODE_TYPES.INSPECTOR, 'Backend');

				const inspector8Node = await mgr4.addChild(NODE_TYPES.INSPECTOR, 'Frontend');
				const inspector9Node = await mgr4.addChild(NODE_TYPES.INSPECTOR, 'Backend');

				const inspector10Node = await mgr5.addChild(NODE_TYPES.INSPECTOR, 'Frontend');
				const inspector11Node = await mgr5.addChild(NODE_TYPES.INSPECTOR, 'Backend');

				editorManager.arrangeNodes();

				// Phase 4: Agents (after 3 more seconds, 6s total)
				setTimeout(async () => {
					// Create agents for mgr1 inspectors
					const agent1Node = await inspector1Node.addChild(NODE_TYPES.AGENT, 'Grid');
					const agent2Node = await inspector1Node.addChild(NODE_TYPES.AGENT, 'Add New Form');
					const agent3Node = await inspector1Node.addChild(NODE_TYPES.AGENT, 'Delete Form');

					const agent4Node = await inspector2Node.addChild(NODE_TYPES.AGENT, 'User Profile');
					const agent5Node = await inspector2Node.addChild(NODE_TYPES.AGENT, 'Settings');
					const agent6Node = await inspector2Node.addChild(NODE_TYPES.AGENT, 'Notifications');

					const agent7Node = await inspector3Node.addChild(NODE_TYPES.AGENT, 'Database Backup');
					const agent8Node = await inspector3Node.addChild(NODE_TYPES.AGENT, 'Query Optimization');
					const agent9Node = await inspector3Node.addChild(NODE_TYPES.AGENT, 'Data Migration');
					const agent91Node = await inspector3Node.addChild(NODE_TYPES.AGENT, 'Data Update');

					// Create agents for mgr2 inspectors
					const agent10Node = await inspector4Node.addChild(NODE_TYPES.AGENT, 'Grid');
					const agent11Node = await inspector4Node.addChild(NODE_TYPES.AGENT, 'Add New Form');
					const agent12Node = await inspector4Node.addChild(NODE_TYPES.AGENT, 'Delete Form');
					const agent121Node = await inspector4Node.addChild(NODE_TYPES.AGENT, 'Report Form');

					const agent13Node = await inspector5Node.addChild(NODE_TYPES.AGENT, 'User Profile');
					const agent14Node = await inspector5Node.addChild(NODE_TYPES.AGENT, 'Settings');

					// Create agents for mgr3 inspectors
					const agent15Node = await inspector6Node.addChild(NODE_TYPES.AGENT, 'Grid');
					const agent16Node = await inspector6Node.addChild(NODE_TYPES.AGENT, 'Add New Form');
					const agent17Node = await inspector6Node.addChild(NODE_TYPES.AGENT, 'Delete Form');
					const agent181Node = await inspector6Node.addChild(NODE_TYPES.AGENT, 'User Access');
					const agent18Node = await inspector7Node.addChild(NODE_TYPES.AGENT, 'User Profile');

					// Create agents for mgr4 inspectors
					const agent19Node = await inspector8Node.addChild(NODE_TYPES.AGENT, 'Grid');
					const agent20Node = await inspector8Node.addChild(NODE_TYPES.AGENT, 'Add New Form');
					const agent21Node = await inspector8Node.addChild(NODE_TYPES.AGENT, 'Delete Form');
					const agent22Node = await inspector9Node.addChild(NODE_TYPES.AGENT, 'User Profile');

					// Create agents for mgr5 inspectors
					const agent23Node = await inspector10Node.addChild(NODE_TYPES.AGENT, 'Grid');
					const agent24Node = await inspector10Node.addChild(NODE_TYPES.AGENT, 'Add New Form');
					const agent25Node = await inspector10Node.addChild(NODE_TYPES.AGENT, 'Delete Form');
					const agent26Node = await inspector11Node.addChild(NODE_TYPES.AGENT, 'User Profile');

					editorManager.arrangeNodes();

					// Start status change animations after all nodes are created
					setTimeout(() => {
						// Demonstrate status changes
                                                directorNode.setStatus('idle');

						// Show different states over time
                                                setTimeout(() => directorNode.setStatus('thinking'), 2000);
                                                setTimeout(() => directorNode.setStatus('working'), 4000);
                                                setTimeout(() => directorNode.setStatus('completed'), 6000);

						// Show agent status changes
						setTimeout(() => agent1Node.setStatus('thinking'), 2500);
						setTimeout(() => agent1Node.setStatus('working'), 3500);
						setTimeout(() => agent1Node.setStatus('completed'), 5500);

						// Show error state
						setTimeout(() => agent3Node.setStatus('error'), 3000);
					}, 1500);

				}, 3000); // Phase 4: Agents (after 3 more seconds)

			}, 2000); // Phase 3: Inspectors (after 2 more seconds)

		}, 1000); // Phase 2: Managers (after 1 second)

	} catch (error) {
		console.warn('Error adding demo nodes:', error);
		throw error;
	}
}
