// Theme Manager for ThrottleX
window.themeManager = {
    getTheme: function () {
        return localStorage.getItem('throttlex-theme') || 'auto';
    },
    setTheme: function (theme) {
        localStorage.setItem('throttlex-theme', theme);
        let effectiveTheme = theme;
        if (theme === 'auto') {
            effectiveTheme = window.matchMedia('(prefers-color-scheme: dark)').matches ? 'dark' : 'light';
        }
        document.documentElement.setAttribute('data-bs-theme', effectiveTheme);
    },
    initTheme: function () {
        const saved = window.themeManager.getTheme();
        this.setTheme(saved);
        
        window.matchMedia('(prefers-color-scheme: dark)').addEventListener('change', function () {
            const current = window.themeManager.getTheme();
            if (current === 'auto') {
                window.themeManager.setTheme('auto');
            }
        });
    }
};

window.themeManager.initTheme();
