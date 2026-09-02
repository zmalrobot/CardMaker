// CardMaker Theme Management (System / Light / Dark)
// Synchronizes data-bs-theme, localStorage, and cookie for SSR

(function () {
    const THEME_KEY = 'cm-theme';
    const COOKIE_NAME = 'cm-theme';

    function getStoredTheme() {
        return localStorage.getItem(THEME_KEY) || 'system';
    }

    function getSystemTheme() {
        return window.matchMedia('(prefers-color-scheme: dark)').matches ? 'dark' : 'light';
    }

    function applyTheme(theme) {
        const resolvedTheme = theme === 'system' ? getSystemTheme() : theme;
        document.documentElement.setAttribute('data-bs-theme', resolvedTheme);
        document.documentElement.setAttribute('data-cm-theme-setting', theme);

        // Update cookie for SSR
        document.cookie = `${COOKIE_NAME}=${theme};path=/;max-age=31536000;SameSite=Lax`;
    }

    // Expose to window for Blazor JSInterop
    window.cardMakerTheme = {
        getTheme: function () {
            return getStoredTheme();
        },
        setTheme: function (theme) {
            localStorage.setItem(THEME_KEY, theme);
            applyTheme(theme);
        },
        init: function () {
            const current = getStoredTheme();
            applyTheme(current);

            // Listen for OS scheme changes
            window.matchMedia('(prefers-color-scheme: dark)').addEventListener('change', function () {
                if (getStoredTheme() === 'system') {
                    applyTheme('system');
                }
            });
        }
    };

    // Auto-init on script load
    window.cardMakerTheme.init();

    // CardMaker App Utilities (CSP compliant, no eval)
    window.cardMaker = {
        downloadFile: function (fileName, contentType, base64Data) {
            try {
                const byteCharacters = atob(base64Data);
                const byteNumbers = new Array(byteCharacters.length);
                for (let i = 0; i < byteCharacters.length; i++) {
                    byteNumbers[i] = byteCharacters.charCodeAt(i);
                }
                const byteArray = new Uint8Array(byteNumbers);
                const blob = new Blob([byteArray], { type: contentType });
                const url = URL.createObjectURL(blob);
                const a = document.createElement('a');
                a.href = url;
                a.download = fileName;
                document.body.appendChild(a);
                a.click();
                setTimeout(() => {
                    document.body.removeChild(a);
                    URL.revokeObjectURL(url);
                }, 200);
            } catch (err) {
                console.error("CardMaker: Errore durante il download del file:", err);
            }
        },

        copyToClipboard: async function (text) {
            try {
                if (navigator.clipboard && window.isSecureContext) {
                    await navigator.clipboard.writeText(text);
                    return true;
                } else {
                    const textArea = document.createElement("textarea");
                    textArea.value = text;
                    textArea.style.position = "fixed";
                    textArea.style.left = "-999999px";
                    document.body.appendChild(textArea);
                    textArea.focus();
                    textArea.select();
                    document.execCommand('copy');
                    textArea.remove();
                    return true;
                }
            } catch (err) {
                console.error("CardMaker: Impossibile copiare negli appunti:", err);
                return false;
            }
        },

        toggleSidebar: function () {
            const sidebar = document.querySelector('.cm-sidebar');
            if (sidebar) {
                sidebar.classList.toggle('show');
            }
        }
    };
})();

