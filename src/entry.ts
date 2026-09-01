const query = new URLSearchParams(location.search)
if (query.has('atlas')) void Promise.all([import('./style.css'), import('./atlas-inspector')])
else if (query.has('prototype')) void Promise.all([import('./style.css'), import('./main')])
else void Promise.all([import('./medieval/style.css'), import('./medieval/main')])
