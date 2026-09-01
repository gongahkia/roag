import './style.css'

const query = new URLSearchParams(location.search)
if (query.has('atlas')) void import('./atlas-inspector')
else if (query.has('prototype')) void import('./main')
else void import('./medieval/main')
