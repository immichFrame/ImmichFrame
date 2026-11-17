/**
 * Drop @layer so Chromium < 99 (webOS 23 and older) still applies utilities.
 * Source order matches layer priority (theme, base, components, utilities),
 * so unwrapping keeps the same cascade inside this stylesheet.
 */
export default function unwrapLayers() {
	return {
		postcssPlugin: 'unwrap-cascade-layers',
		Once(root) {
			let found = true;
			while (found) {
				found = false;
				root.walkAtRules('layer', (atRule) => {
					const nested = atRule.nodes?.some(
						(node) => node.type === 'atrule' && node.name === 'layer'
					);
					if (nested) return;

					found = true;
					// Copy first: replaceWith mutates the child list while moving nodes.
					if (atRule.nodes?.length) atRule.replaceWith(atRule.nodes.slice());
					else atRule.remove();
				});
			}
		}
	};
}

unwrapLayers.postcss = true;
