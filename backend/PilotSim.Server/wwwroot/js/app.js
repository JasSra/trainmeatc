// Modal management functions
window.showModal = (modalId) => {
    const modal = new bootstrap.Modal(document.getElementById(modalId));
    modal.show();
};

window.hideModal = (modalId) => {
    const modal = bootstrap.Modal.getInstance(document.getElementById(modalId));
    if (modal) {
        modal.hide();
    }
};

// Confirmation dialog
window.confirm = (message) => {
    return confirm(message);
};

// Audio playback for TTS
window.playAudio = (elementId, audioPath) => {
    const audio = document.getElementById(elementId);
    if (audio && audioPath) {
        audio.src = audioPath;
        audio.play().catch(error => {
            console.warn('Audio playback failed:', error);
        });
    }
};

// Theme management helpers to keep dark mode consistent across the site
(function () {
    const THEME_KEY = 'theme';
    const DARK_CLASS = 'dark-theme';
    let initialized = false;

    function normalizeTheme(theme) {
        return theme === 'dark' ? 'dark' : 'light';
    }

    function getStoredTheme() {
        try {
            return localStorage.getItem(THEME_KEY);
        } catch (error) {
            return null;
        }
    }

    function setStoredTheme(theme) {
        try {
            localStorage.setItem(THEME_KEY, normalizeTheme(theme));
        } catch (error) {
            /* ignore storage failures */
        }
    }

    function clearStoredTheme() {
        try {
            localStorage.removeItem(THEME_KEY);
        } catch (error) {
            /* ignore storage failures */
        }
    }

    function applyTheme(theme) {
        const normalized = normalizeTheme(theme);
        const isDark = normalized === 'dark';
        const root = document.documentElement;
        const body = document.body;

        if (root) {
            root.classList.toggle(DARK_CLASS, isDark);
            root.setAttribute('data-theme', normalized);
        }

        if (body) {
            body.classList.toggle(DARK_CLASS, isDark);
            body.setAttribute('data-theme', normalized);
        }

        document.querySelectorAll('.page').forEach(element => {
            element.classList.toggle(DARK_CLASS, isDark);
            element.classList.toggle('light-theme', !isDark);
        });

        if (window.plotlyBranding) {
            window.plotlyBranding.refreshTheme();
        }

        return normalized;
    }

    function currentTheme() {
        return document.documentElement.classList.contains(DARK_CLASS) ? 'dark' : 'light';
    }

    function preferredTheme() {
        const stored = getStoredTheme();
        if (stored) {
            return normalizeTheme(stored);
        }

        if (window.matchMedia && window.matchMedia('(prefers-color-scheme: dark)').matches) {
            return 'dark';
        }

        return 'light';
    }

    function bindSystemThemeListener() {
        if (!(window.matchMedia && window.matchMedia('(prefers-color-scheme: dark)'))) {
            return;
        }

        const mediaQuery = window.matchMedia('(prefers-color-scheme: dark)');
        mediaQuery.addEventListener('change', event => {
            const stored = getStoredTheme();
            if (stored) {
                return; // user has explicit preference
            }

            applyTheme(event.matches ? 'dark' : 'light');
        });
    }

    function initialize() {
        if (!initialized) {
            applyTheme(preferredTheme());
            bindSystemThemeListener();
            initialized = true;
        }

        return currentTheme();
    }

    function setTheme(theme, persist = true) {
        const normalized = applyTheme(theme);
        if (persist) {
            setStoredTheme(normalized);
        }
        return normalized;
    }

    function toggleTheme() {
        const next = currentTheme() === 'dark' ? 'light' : 'dark';
        return setTheme(next, true);
    }

    window.themeManager = {
        initialize,
        setTheme: theme => setTheme(theme, true),
        toggleTheme,
        currentTheme,
        clearStoredTheme,
        refresh: () => applyTheme(currentTheme())
    };

    if (document.readyState === 'loading') {
        document.addEventListener('DOMContentLoaded', initialize, { once: true });
    } else {
        initialize();
    }
})();

// Plotly branding utilities for consistent styling
(function () {
    if (typeof window === 'undefined') {
        return;
    }

    function getVar(name, fallback) {
        const styles = getComputedStyle(document.documentElement);
        const value = styles.getPropertyValue(name);
        return value ? value.trim() : fallback;
    }

    function ensureElement(elementOrId) {
        if (!elementOrId) {
            return null;
        }
        if (elementOrId instanceof HTMLElement) {
            return elementOrId;
        }
        return document.getElementById(elementOrId);
    }

    function buildBaseLayout() {
        return {
            paper_bgcolor: getVar('--bg-secondary', '#f8fafc'),
            plot_bgcolor: getVar('--bg-primary', '#ffffff'),
            font: {
                family: 'Inter, -apple-system, BlinkMacSystemFont, "Segoe UI", sans-serif',
                color: getVar('--text-primary', '#1e293b'),
                size: 13
            },
            margin: { t: 40, r: 30, b: 40, l: 50 },
            legend: {
                orientation: 'h',
                x: 0,
                y: 1.1,
                font: {
                    family: 'Inter, -apple-system, BlinkMacSystemFont, "Segoe UI", sans-serif',
                    color: getVar('--text-secondary', '#64748b')
                }
            },
            title: {
                font: {
                    family: 'Inter, -apple-system, BlinkMacSystemFont, "Segoe UI", sans-serif',
                    size: 18,
                    color: getVar('--text-primary', '#1e293b')
                }
            },
            colorway: [
                getVar('--aviation-primary', '#1e3a8a'),
                getVar('--aviation-accent', '#0ea5e9'),
                getVar('--aviation-success', '#10b981'),
                getVar('--aviation-warning', '#f59e0b'),
                getVar('--aviation-info', '#06b6d4'),
                getVar('--aviation-danger', '#ef4444'),
                getVar('--aviation-secondary', '#334155')
            ],
            hoverlabel: {
                font: {
                    family: 'Inter, -apple-system, BlinkMacSystemFont, "Segoe UI", sans-serif',
                    color: getVar('--bg-primary', '#ffffff')
                },
                bgcolor: getVar('--aviation-primary', '#1e3a8a')
            }
        };
    }

    function mergeLayout(base, overrides) {
        if (!overrides) {
            return { ...base };
        }

        return {
            ...base,
            ...overrides,
            font: { ...base.font, ...(overrides.font || {}) },
            legend: { ...base.legend, ...(overrides.legend || {}) },
            margin: { ...base.margin, ...(overrides.margin || {}) },
            title: {
                ...base.title,
                ...(overrides.title || {}),
                font: {
                    ...(base.title ? base.title.font : {}),
                    ...((overrides.title || {}).font || {})
                }
            },
            hoverlabel: { ...base.hoverlabel, ...(overrides.hoverlabel || {}) }
        };
    }

    function mergeConfig(overrides) {
        const baseConfig = {
            responsive: true,
            displaylogo: false,
            modeBarButtonsToRemove: ['lasso2d', 'select2d']
        };

        if (!overrides) {
            return baseConfig;
        }

        return {
            ...baseConfig,
            ...overrides,
            modeBarButtonsToRemove: overrides.modeBarButtonsToRemove || baseConfig.modeBarButtonsToRemove
        };
    }

    function apply(elementOrId, data, layoutOverrides, configOverrides) {
        if (!window.Plotly) {
            console.warn('Plotly is not loaded.');
            return;
        }

        const target = ensureElement(elementOrId);
        if (!target) {
            console.warn('Plotly target element not found.');
            return;
        }

        const layout = mergeLayout(buildBaseLayout(), layoutOverrides);
        const config = mergeConfig(configOverrides);

        target.dataset.plotlyBrand = 'true';
        target.__plotlyBranding = {
            data,
            layoutOverrides,
            configOverrides
        };

        return window.Plotly.react(target, data, layout, config);
    }

    function refreshTheme() {
        if (!window.Plotly) {
            return;
        }

        document.querySelectorAll('[data-plotly-brand="true"]').forEach(target => {
            const snapshot = target.__plotlyBranding;
            if (!snapshot) {
                return;
            }
            apply(target, snapshot.data, snapshot.layoutOverrides, snapshot.configOverrides);
        });
    }

    window.plotlyBranding = {
        apply,
        buildLayout: overrides => mergeLayout(buildBaseLayout(), overrides),
        refreshTheme
    };
})();
