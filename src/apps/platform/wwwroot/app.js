window.tabflowSettings = {
  load() {
    const raw = window.localStorage.getItem("tabflow.platform.settings");
    if (!raw) {
      return null;
    }

    try {
      return JSON.parse(raw);
    } catch {
      return null;
    }
  },

  save(settings) {
    window.localStorage.setItem("tabflow.platform.settings", JSON.stringify(settings));
    const culture = settings?.language || "en-GB";
    const value = `c=${culture}|uic=${culture}`;
    document.cookie = `.AspNetCore.Culture=${encodeURIComponent(value)}; path=/; max-age=31536000; SameSite=Lax`;
  },

  apply(settings) {
    const root = document.documentElement;
    root.dataset.uiDensity = settings?.density || "compact";
    root.lang = settings?.language || "en-GB";
  }
};

(() => {
  const settings = window.tabflowSettings.load();
  if (settings) {
    window.tabflowSettings.apply(settings);
  }
})();
