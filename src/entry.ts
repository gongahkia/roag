import './style.css'

const query = new URLSearchParams(location.search)
if (query.has('atlas')) void import('./atlas-inspector')
else void import('./main')
