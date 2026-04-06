// Blazor JS Isolation — Cockpit.razor.js
// ES Module syntax: export named functions only.
// No window registration. Lifetime managed by IJSObjectReference in Cockpit.razor.

/**
 * Initialises the PCB hover lighting effect on the hero section.
 * @param {string} sectionId - The id attribute of the hero <section> element.
 */
export function init(sectionId) {
    const section = document.getElementById(sectionId);
    if (!section) return;

    const svg = section.querySelector('#cockpit-pcb-svg');
    if (!svg) return;

    const lines = Array.from(svg.querySelectorAll('line.pcb-line'));
    if (lines.length === 0) return;

    let rafPending = false;
    let mouseX = -9999;
    let mouseY = -9999;

    /**
     * For a given <line> element, compute its midpoint in page coordinates.
     * SVG percentage-based attributes (e.g. "25%") are resolved via getBoundingClientRect.
     */
    function getMidpoint(line) {
        const svgRect = svg.getBoundingClientRect();

        const resolveX = (attr) => {
            const val = line.getAttribute(attr);
            if (!val) return 0;
            if (val.endsWith('%')) return svgRect.left + (parseFloat(val) / 100) * svgRect.width;
            if (val === '0') return svgRect.left;
            if (val === '100%') return svgRect.right;
            return svgRect.left + parseFloat(val);
        };

        const resolveY = (attr) => {
            const val = line.getAttribute(attr);
            if (!val) return 0;
            if (val.endsWith('%')) return svgRect.top + (parseFloat(val) / 100) * svgRect.height;
            if (val === '0') return svgRect.top;
            if (val === '100%') return svgRect.bottom;
            return svgRect.top + parseFloat(val);
        };

        return {
            x: (resolveX('x1') + resolveX('x2')) / 2,
            y: (resolveY('y1') + resolveY('y2')) / 2,
        };
    }

    function updateLines() {
        rafPending = false;
        const RADIUS = 120;

        for (const line of lines) {
            const mid = getMidpoint(line);
            const dx = mouseX - mid.x;
            const dy = mouseY - mid.y;
            const dist = Math.sqrt(dx * dx + dy * dy);

            if (dist < RADIUS) {
                const intensity = 1 - dist / RADIUS;
                const alpha = 0.06 + intensity * 0.69; // ramps from 0.06 → 0.75
                line.setAttribute('stroke', `rgba(234, 88, 12, ${alpha.toFixed(2)})`);
            } else {
                line.setAttribute('stroke', 'rgba(234, 88, 12, 0.06)');
            }
        }
    }

    function onMouseMove(e) {
        mouseX = e.clientX;
        mouseY = e.clientY;

        if (!rafPending) {
            rafPending = true;
            requestAnimationFrame(updateLines);
        }
    }

    function onMouseLeave() {
        mouseX = -9999;
        mouseY = -9999;
        if (!rafPending) {
            rafPending = true;
            requestAnimationFrame(updateLines);
        }
    }

    section.addEventListener('mousemove', onMouseMove);
    section.addEventListener('mouseleave', onMouseLeave);

    // Return a cleanup function — not called by Blazor directly but good practice
    // Blazor disposes the IJSObjectReference; that ends the module's lifetime.
}
