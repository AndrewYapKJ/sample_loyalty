window.changeTheme = function(theme) {
  try {
    console.log('[Theme] Requested theme:', theme);
        
    // Toggle dark theme via data-theme attribute on html element
    if (theme === 'dark' || theme === 'dark-base') {
      document.documentElement.setAttribute('data-theme', 'dark');
      localStorage.setItem('theme', 'dark');
      console.log('[Theme] Applied dark theme');
    } else {
      document.documentElement.removeAttribute('data-theme');
      localStorage.setItem('theme', 'light');
      console.log('[Theme] Applied light theme');
    }
  } catch(e) {
    console.error('[Theme] Error applying theme', e);
  }
};

// Initialize theme on page load
window.initializeTheme = function() {
  try {
    const savedTheme = localStorage.getItem('theme');
    if (savedTheme === 'dark') {
      document.documentElement.setAttribute('data-theme', 'dark');
      console.log('[Theme] Initialized with dark theme from localStorage');
    } else {
      console.log('[Theme] Initialized with light theme (default)');
    }
  } catch(e) {
    console.error('[Theme] Error initializing theme', e);
  }
};

// Call initialize on page load
if (document.readyState === 'loading') {
  document.addEventListener('DOMContentLoaded', window.initializeTheme);
} else {
  window.initializeTheme();
}