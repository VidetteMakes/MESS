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

// ...rest of your code unchanged...

function applyTheme(isDark) {
    document.body.classList.toggle("dark-mode", isDark);
    document.body.classList.toggle("light-mode", !isDark);

    applyDarkModeToFluentTextAreas(isDark);
    applyDarkModeToFluentMenus(isDark);

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
    if (document.body.classList.contains("dark-mode")) {
        applyDarkModeToFluentTextAreas(true);
        applyDarkModeToFluentMenus(true);
    } else {
        applyDarkModeToFluentTextAreas(false);
        applyDarkModeToFluentMenus(false);
    }
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
