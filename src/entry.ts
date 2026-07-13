import './style.css'

if (new URLSearchParams(location.search).has('atlas')) void import('./atlas-inspector')
else void import('./main')
