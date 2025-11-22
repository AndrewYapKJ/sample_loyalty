window.changeTheme = function(theme){
  try {
    console.log('[Theme] Requested theme:', theme);
    const el = document.getElementById('radzen-theme');
    if(!el){
      console.warn('[Theme] radzen-theme link element not found');
      return;
    }
    const href = `_content/Radzen.Blazor/css/${theme}.css`;
    el.setAttribute('href', href);
    console.log('[Theme] Applied href:', href);
  } catch(e){
    console.error('[Theme] Error applying theme', e);
  }
};