import './style.css'

const query = new URLSearchParams(location.search)
if (query.get('debug') === 'levels') void import('./level-preview-page')
else if (query.has('atlas')) void import('./atlas-inspector')
else void import('./main')
