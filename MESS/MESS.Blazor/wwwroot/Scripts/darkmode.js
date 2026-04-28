function getSharedTextAreaStyles(isDark) {
    return `
        [part="root"],
        [part="control"],
        [part="field"],
        [role="presentation"],
        *::before,
        *::after {
            background: transparent !important;
            border: none !important;
            box-shadow: none !important;
            outline: none !important;
        }

        textarea {
            background: ${isDark ? '#2c2c2c' : '#fff'} !important;
            color: ${isDark ? '#fff' : '#000'} !important;
            caret-color: ${isDark ? '#fff' : '#000'} !important;
            border: none !important; /* removed all borders */
            border-radius: 4px !important;
            padding: 0.5rem !important;
            resize: vertical !important;
            min-height: 100px !important;
            max-height: 300px !important;
            box-shadow: none !important;
            position: relative !important;
            z-index: 1000 !important;
            font-size: 1rem !important;
            transition: background-color 0.3s ease, color 0.3s ease !important;
        }

        textarea:focus {
            outline: none !important;
            box-shadow: none !important;
        }
    `;
}

function applyDarkModeToFluentTextAreas(isDark) {
    document.querySelectorAll("fluent-text-area").forEach(el => {
        const shadow = el.shadowRoot;
        if (!shadow) return;

        const existingStyle = shadow.getElementById('theme-mode-injected-style');
        if (existingStyle) existingStyle.remove();

        const style = document.createElement('style');
        style.id = 'theme-mode-injected-style';
        style.textContent = getSharedTextAreaStyles(isDark);
        shadow.appendChild(style);

        el.style.backgroundColor = 'transparent';
        el.style.color = isDark ? '#fff' : '#000';
    });
}

/**
 * Stronger dark mode style injection into the shadow DOM of fluent-menu and fluent-menu-item,
 * to forcibly set background, color, and remove all borders (including yellow).
 */
function applyDarkModeToFluentMenus(isDark) {
    const menus = document.querySelectorAll('fluent-menu, fluent-menu-item');
    menus.forEach(el => {
        const shadow = el.shadowRoot;
        if (!shadow) return;

        // Remove previous injected style if any
        const styleId = 'dark-mode-menu-style';
        const existingStyle = shadow.getElementById(styleId);
        if (existingStyle) existingStyle.remove();

        const style = document.createElement('style');
        style.id = styleId;

        if (isDark) {
            style.textContent = `
                [part="root"],
                [part="control"],
                [part="field"],
                [role="presentation"] {
                    background-color: #1a1a1a !important;
                    color: #ffffff !important;
                    border: none !important; /* removed borders */
                    box-shadow: none !important;
                    outline: none !important;
                }
                [part="control"]:hover,
                [part="field"]:hover {
                    background-color: #333333 !important; /* subtle dark hover */
                    color: #ffffff !important;
                    border: none !important;
                    box-shadow: none !important;
                    outline: none !important;
                }
                [part="control"][aria-disabled="true"],
                [part="field"][aria-disabled="true"] {
                    opacity: 0.5 !important;
                    cursor: not-allowed !important;
                    border: none !important;
                }
            `;
        } else {
            style.textContent = `
                [part="root"],
                [part="control"],
                [part="field"],
                [role="presentation"] {
                    background-color: #ffffff !important;
                    color: #212529 !important;
                    border: none !important; /* removed borders */
                    box-shadow: none !important;
                    outline: none !important;
                }
                [part="control"]:hover,
                [part="field"]:hover {
                    background-color: #e0e0e0 !important; /* subtle light hover */
                    color: #000000 !important;
                    border: none !important;
                    box-shadow: none !important;
                    outline: none !important;
                }
                [part="control"][aria-disabled="true"],
                [part="field"][aria-disabled="true"] {
                    opacity: 0.5 !important;
                    cursor: not-allowed !important;
                    border: none !important;
                }
            `;
        }

        shadow.appendChild(style);

        // Clear border styles on host element as fallback
        el.style.border = 'none';
        el.style.backgroundColor = isDark ? '#1a1a1a' : '#fff';
        el.style.color = isDark ? '#fff' : '#212529';
    });
}

/**
 * Rich-text body content (e.g. Quill output) may embed inline "color" styles
 * authored while in light mode. Those inline colors defeat CSS inheritance and
 * leave text unreadable after toggling themes (e.g. near-black text on a dark
 * background). This helper walks such content and remaps only the colors that
 * would be unreadable on the active theme, while preserving the original color
 * (stashed in data-mess-original-color) so toggling back restores the author's
 * intent (red/green accents, etc.).
 */
const RICH_TEXT_ORIGINAL_COLOR_ATTR = 'messOriginalColor';

function parseColorToRgb(colorStr) {
    if (!colorStr) return null;
    const trimmed = colorStr.trim().toLowerCase();
    if (trimmed === '' || trimmed === 'inherit' || trimmed === 'initial' ||
        trimmed === 'unset' || trimmed === 'currentcolor' || trimmed === 'transparent') {
        return null;
    }

    const rgbMatch = trimmed.match(/^rgba?\(\s*(\d+(?:\.\d+)?)\s*,\s*(\d+(?:\.\d+)?)\s*,\s*(\d+(?:\.\d+)?)/);
    if (rgbMatch) {
        return [parseFloat(rgbMatch[1]), parseFloat(rgbMatch[2]), parseFloat(rgbMatch[3])];
    }

    const hexMatch = trimmed.match(/^#([0-9a-f]{3}|[0-9a-f]{6})$/);
    if (hexMatch) {
        let hex = hexMatch[1];
        if (hex.length === 3) hex = hex.split('').map(c => c + c).join('');
        return [
            parseInt(hex.substring(0, 2), 16),
            parseInt(hex.substring(2, 4), 16),
            parseInt(hex.substring(4, 6), 16)
        ];
    }

    const named = { black: [0, 0, 0], white: [255, 255, 255], silver: [192, 192, 192],
                    gray: [128, 128, 128], grey: [128, 128, 128] };
    return named[trimmed] || null;
}

function relativeLuminance(rgb) {
    const channels = rgb.map(v => {
        const c = Math.max(0, Math.min(255, v)) / 255;
        return c <= 0.03928 ? c / 12.92 : Math.pow((c + 0.055) / 1.055, 2.4);
    });
    return 0.2126 * channels[0] + 0.7152 * channels[1] + 0.0722 * channels[2];
}

function adjustRichTextColorsForTheme(isDark) {
    // Any container whose content comes from a rich-text editor. Add new classes
    // here if other pages render Quill/HTML content into the DOM.
    const containers = document.querySelectorAll('.rich-text-content, .ql-editor');
    if (!containers.length) return;

    containers.forEach(container => {
        const candidates = container.querySelectorAll('[style*="color"]');
        candidates.forEach(el => {
            if (!(RICH_TEXT_ORIGINAL_COLOR_ATTR in el.dataset)) {
                el.dataset[RICH_TEXT_ORIGINAL_COLOR_ATTR] = el.style.color || '';
            }

            const originalColor = el.dataset[RICH_TEXT_ORIGINAL_COLOR_ATTR];
            const rgb = parseColorToRgb(originalColor);

            if (!rgb) {
                // Nothing useful to parse (inherit/unset/etc.) — leave as-is.
                return;
            }

            const lum = relativeLuminance(rgb);
            const chroma = Math.max(...rgb) - Math.min(...rgb);
            const isNearGrayscale = chroma < 32;

            // Poor-contrast thresholds chosen to match our theme backgrounds
            // (~#1a1a1a dark, ~#ffffff light). We only rewrite near-grayscale
            // colors that would be unreadable, so semantic accents (red/green/
            // blue status text authored in rich text) survive the theme flip.
            if (isDark && isNearGrayscale && lum < 0.35) {
                el.style.color = '#ffffff';
            } else if (!isDark && isNearGrayscale && lum > 0.75) {
                el.style.color = '#000000';
            } else {
                el.style.color = originalColor;
            }
        });
    });
}

function applyTheme(isDark) {
    document.body.classList.toggle("dark-mode", isDark);
    document.body.classList.toggle("light-mode", !isDark);

    applyDarkModeToFluentTextAreas(isDark);
    applyDarkModeToFluentMenus(isDark);
    adjustRichTextColorsForTheme(isDark);

    if (isDark && typeof window.ApplyDarkModeFixToFailureTextAreas === "function") {
        window.ApplyDarkModeFixToFailureTextAreas();
    }
}

let currentDark = window.matchMedia("(prefers-color-scheme: dark)").matches;
let manualOverride = null;

function detectAndApplyTheme() {
    const stored = localStorage.getItem("manualDarkMode");
    if (stored === "dark") manualOverride = true;
    else if (stored === "light") manualOverride = false;

    const isDark = manualOverride !== null ? manualOverride : currentDark;
    applyTheme(isDark);
}

document.addEventListener("DOMContentLoaded", detectAndApplyTheme);

window.toggleDarkMode = function () {
    const isCurrentlyDark = document.body.classList.contains("dark-mode");
    manualOverride = !isCurrentlyDark;
    localStorage.setItem("manualDarkMode", manualOverride ? "dark" : "light");
    applyTheme(manualOverride);
};

window.setDarkMode = function (isDark) {
    manualOverride = isDark;
    localStorage.setItem("manualDarkMode", isDark ? "dark" : "light");
    applyTheme(isDark);
};

window.matchMedia("(prefers-color-scheme: dark)").addEventListener("change", e => {
    currentDark = e.matches;
    if (manualOverride === null) {
        detectAndApplyTheme(); // only auto-toggle if user hasn't manually overridden
    }
});

// Only re-apply styling on DOM changes (not full theme switching)
const observer = new MutationObserver(() => {
    const isDark = document.body.classList.contains("dark-mode");
    applyDarkModeToFluentTextAreas(isDark);
    applyDarkModeToFluentMenus(isDark);
    // Blazor can render/re-render rich-text panels after the initial theme pass
    // (e.g. navigating between work instruction steps); keep their colors in sync.
    adjustRichTextColorsForTheme(isDark);
});
observer.observe(document.body, { childList: true, subtree: true });

window.FixDarkModeColors = function () {
    if (document.body.classList.contains("dark-mode")) {
        document.querySelectorAll(".show-details-text *").forEach(el => {
            const inlineColor = el.style.color;
            if (inlineColor && ["black", "#000000", "#000", "rgb(0, 0, 0)"].includes(inlineColor.toLowerCase())) {
                el.style.color = "white";
            }
        });
    }
};

window.ApplyDarkModeFixToFailureTextAreas = function () {
    document.querySelectorAll("fluent-text-area.failure-textarea").forEach(el => {
        const shadow = el.shadowRoot;
        if (!shadow) return;

        const textarea = shadow.querySelector("textarea");
        if (textarea) {
            textarea.style.backgroundColor = "#2c2c2c";
            textarea.style.color = "white";
            textarea.style.border = "none";
            textarea.style.padding = "0.5rem";
            textarea.style.borderRadius = "4px";
            textarea.style.resize = "vertical";
            textarea.style.minHeight = "100px";
            textarea.style.maxHeight = "300px";
            textarea.style.fontSize = "1rem";
            textarea.style.caretColor = "white";
        }
    });
};
